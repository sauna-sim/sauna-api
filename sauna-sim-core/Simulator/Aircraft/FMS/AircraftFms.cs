using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AviationCalcUtilNet.Aviation;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Magnetic;
using AviationCalcUtilNet.MathTools;
using AviationCalcUtilNet.Units;
using FsdConnectorNet;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fixes;
using FsdConnectorNet.Args;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.Autopilot;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;
using AviationCalcUtilNet.Atmos;
using System.Numerics;
using NavData_Interface.Objects.LegCollections.Procedures;
using NavData_Interface.Objects.LegCollections.Legs;

namespace SaunaSim.Core.Simulator.Aircraft.FMS
{
    public enum FmsPhaseType
    {
        CLIMB,
        CRUISE,
        DESCENT,
        APPROACH,
        GO_AROUND
    }
    public class AircraftFms
    {
        private SimAircraft _parentAircraft;
        // private int _cruiseAlt;
        private IRouteLeg _activeLeg;
        private List<IRouteLeg> _routeLegs;
        private object _routeLegsLock;
        private bool _suspended;
        private static Velocity MIN_GS_DIFF = Velocity.FromKnots(10);
        private Velocity _lastGs = (Velocity) 0;
        private bool _wpEvtTriggered = false;
        private MagneticTileManager _magTileMgr;

        // Fms Values
        private Length _xTk;
        private Length _aTk;
        private Bearing _requiredTrueCourse;
        private Length _turnRadius;
        private Length _vTk;
        private Angle _requiredFpa;
        private McpSpeedUnitsType _spdUnits;
        private int _selSpd;

        private int _routeStartIndex = 0;

        public EventHandler<WaypointPassedEventArgs> WaypointPassed;

        public AircraftFms(SimAircraft parentAircraft, MagneticTileManager mgr)
        {
            _parentAircraft = parentAircraft;
            _routeLegsLock = new object();
            _xTk = Length.FromMeters(-1);
            _aTk = Length.FromMeters(-1);
            _requiredTrueCourse = null;
            _vTk = Length.FromMeters(-1);
            _requiredFpa = Angle.FromRadians(0);
            _turnRadius = Length.FromMeters(-1);
            _magTileMgr = mgr;

            lock (_routeLegsLock)
            {
                _routeLegs = new List<IRouteLeg>();
            }

            _suspended = false;

            PhaseType = FmsPhaseType.CRUISE;
        }

        public Length AlongTrackDistance => _aTk;

        public Length CrossTrackDistance => _xTk;

        public Bearing RequiredTrueCourse => _requiredTrueCourse;

        public Length TurnRadius => _turnRadius;

        public Length VerticalTrackDistance => _vTk;

        public Angle RequiredFpa => _requiredFpa;

        public FmsPhaseType PhaseType { get; set; }
        public McpSpeedUnitsType FmsSpeedUnits => _spdUnits;
        public int FmsSpeedValue => _selSpd;


        public bool Suspended
        {
            get => _suspended;
            set => _suspended = value;
        }

        public int CruiseAltitude
        {
            get => (int)(_parentAircraft.FlightPlan != null ? _parentAircraft.FlightPlan.Value.cruiseLevel:0);
            // set => _cruiseAlt = value;
        }

        private string _depIcao;
        private Fix _depArpt;
        public Fix DepartureAirport
        {
            get
            {
                string curDepIcao = _parentAircraft.FlightPlan?.origin.ToUpper();
                if (curDepIcao != _depIcao)
                {
                    _depArpt = DataHandler.GetAirportByIdentifier(curDepIcao);
                    _depIcao = curDepIcao;
                }
                return _depArpt;
            }
            set
            {
                _depArpt = value;
                _depIcao = value.Identifier.ToString().ToUpper();
                if (_parentAircraft.FlightPlan.HasValue)
                {
                    FlightPlan fp = _parentAircraft.FlightPlan.Value;
                    fp.origin = _depIcao;
                    _parentAircraft.FlightPlan = fp;
                }
            }

        }

        private string _arrIcao;
        private Fix _arrArpt;
        public Fix ArrivalAirport
        {
            get
            {
                string curArrIcao = _parentAircraft.FlightPlan?.destination.ToUpper();
                if (curArrIcao != _arrIcao)
                {
                    _arrArpt = DataHandler.GetAirportByIdentifier(curArrIcao);
                    _arrIcao = curArrIcao;
                }
                return _arrArpt;
            }
            set
            {
                _arrArpt = value;
                _arrIcao = value.Identifier.ToString().ToUpper();
                if (_parentAircraft.FlightPlan.HasValue)
                {
                    FlightPlan fp = _parentAircraft.FlightPlan.Value;
                    fp.destination = _arrIcao;
                    _parentAircraft.FlightPlan = fp;
                }
            }
        }

        public IRouteLeg ActiveLeg
        {
            get => _activeLeg;
        }

        public List<IRouteLeg> GetRouteLegs()
        {
            lock (_routeLegsLock)
            {
                return _routeLegs.ToList();
            }
        }

        public void InsertAtIndex(IRouteLeg routeLeg, int index)
        {
            lock (_routeLegsLock)
            {
                try
                {
                    // If inserting as the first item, add discontinuity
                    _routeLegs.Insert(index, routeLeg);
                    if (index == 0)
                    {
                        _routeLegs.Insert(1, new DiscoLeg(Bearing.FromDegrees(0)));
                    }
                    RecalculateVnavPath();
                }
                catch (ArgumentOutOfRangeException ex) { }
            }
        }

        public void AddRouteLeg(IRouteLeg routeLeg)
        {
            lock (_routeLegsLock)
            {
                _routeLegs.Add(routeLeg);
                RecalculateVnavPath();
            }
        }

        public void AddAllLegs(IList<IRouteLeg> routeLegs)
        {
            lock (_routeLegsLock)
            {
                _routeLegs.AddRange(routeLegs);
                RecalculateVnavPath();
            }
        }

        public IRouteLeg ActivateNextLeg()
        {
            lock (_routeLegsLock)
            {
                if (_routeLegs.Count > 0)
                {
                    _activeLeg = _routeLegs[0];
                    _wpEvtTriggered = false;
                    _routeLegs.RemoveAt(0);
                    RecalculateVnavPath();
                }
            }

            return _activeLeg;
        }

        public IRouteLeg GetLegToPoint(IRoutePoint routePoint)
        {
            lock (_routeLegsLock)
            {
                if (_activeLeg != null && _activeLeg.EndPoint.Point.Equals(routePoint))
                {
                    return _activeLeg;
                }

                foreach (IRouteLeg leg in _routeLegs)
                {
                    if (leg.EndPoint.Point.Equals(routePoint))
                    {
                        return leg;
                    }
                }
            }

            return null;
        }

        public void ActivateDirectTo(IRoutePoint routePoint, Bearing course = null)
        {
            lock (_routeLegsLock)
            {
                int index = -1;
                FmsPoint point = null;

                if (_activeLeg != null && _activeLeg.StartPoint != null && _activeLeg.StartPoint.Point.Equals(routePoint))
                {
                    point = _activeLeg.StartPoint;
                    _routeLegs.Insert(0, _activeLeg);
                    index = 0;
                }
                else if (_activeLeg != null && _activeLeg.EndPoint != null && _activeLeg.EndPoint.Point.Equals(routePoint))
                {
                    point = _activeLeg.EndPoint;
                    index = 0;
                }
                else
                {
                    foreach (IRouteLeg leg in _routeLegs)
                    {
                        if (leg.StartPoint != null && leg.StartPoint.Point.Equals(routePoint))
                        {
                            index = _routeLegs.IndexOf(leg);
                            point = leg.StartPoint;
                            break;
                        }
                        else if (leg.EndPoint != null && leg.EndPoint.Point.Equals(routePoint))
                        {
                            index = _routeLegs.IndexOf(leg) + 1;
                            point = leg.EndPoint;
                            break;
                        }
                    }
                }

                if (point == null)
                {
                    point = new FmsPoint(routePoint, RoutePointTypeEnum.FLY_BY);
                }

                // Create direct leg
                IRouteLeg dtoLeg = new DirectToFixLeg(point);

                if (course != null)
                {
                    dtoLeg = new CourseToFixLeg(point, BearingTypeEnum.MAGNETIC, course, _magTileMgr);
                }

                _activeLeg = dtoLeg;
                _wpEvtTriggered = false;

                if (index >= 0)
                {
                    // Remove everything before index
                    _routeLegs.RemoveRange(0, index);
                }
                else
                {
                    _routeLegs.Insert(0, new DiscoLeg(dtoLeg.FinalTrueCourse));
                }

                RecalculateVnavPath();
            }
        }

        /// <summary>
        /// Adds a whole SID to the FMS
        /// </summary>
        /// <param name="sid">The SID to add. Relevant transition and runway transition MUST be selected beforehand.</param>
        /// <returns></returns>
        public bool AddSid(Sid sid)
        {
            lock (_routeLegsLock)
            {
                if (_routeStartIndex != 0)
                {
                    RemoveSid();
                }

                try
                {
                    foreach (var leg in LegFactory.RouteLegsFromNavDataLegs(sid.GetEnumerator(), _magTileMgr))
                    {
                        InsertAtIndex(leg, _routeStartIndex);
                        _routeStartIndex++;
                    }

                } catch (Exception ex)
                {
                    return false;
                }
                
            }

            return true;
        }

        public void RemoveSid()
        {
            while (_routeStartIndex > 0)
            {
                RemoveFirstLeg();
                _routeStartIndex--;
            }
        }

        public bool AddHold(IRoutePoint rp, Bearing magCourse, HoldTurnDirectionEnum turnDir, HoldLegLengthTypeEnum legLengthType, double legLength)
        {
            lock (_routeLegsLock)
            {
                int index = -1;
                FmsPoint point = null;

                if (_activeLeg != null && _activeLeg.EndPoint != null && _activeLeg.EndPoint.Point.Equals(rp))
                {
                    index = 0;
                    point = _activeLeg.EndPoint;
                }
                else
                {
                    foreach (IRouteLeg leg in _routeLegs)
                    {
                        if (leg.EndPoint != null && leg.EndPoint.Point.Equals(rp))
                        {
                            index = _routeLegs.IndexOf(leg) + 1;
                            point = leg.EndPoint;
                            break;
                        }
                    }
                }

                if (index >= 0)
                {

                    point.PointType = RoutePointTypeEnum.FLY_OVER;

                    // Create hold leg
                    IRouteLeg holdLeg = new HoldToManualLeg(point, BearingTypeEnum.MAGNETIC, magCourse, turnDir, legLengthType, legLength, _magTileMgr);

                    // Add leg
                    _routeLegs.Insert(index, holdLeg);
                    RecalculateVnavPath();
                    return true;
                }
            }
            return false;
        }

        public IRouteLeg GetFirstLeg()
        {
            lock (_routeLegsLock)
            {
                if (_routeLegs.Count < 1)
                {
                    return null;
                }

                return _routeLegs[0];
            }
        }

        public void RemoveFirstLeg()
        {
            lock (_routeLegsLock)
            {
                if (_routeLegs.Count >= 1)
                {
                    _routeLegs.RemoveAt(0);

                    RecalculateVnavPath();
                }
            }
        }

        public bool ShouldActivateLnav(int intervalMs)
        {
            if (ActiveLeg != null)
            {
                return ActiveLeg.ShouldActivateLeg(_parentAircraft, intervalMs);
            }

            IRouteLeg leg = GetFirstLeg();

            return leg?.ShouldActivateLeg(_parentAircraft, intervalMs) ?? false;
        }

        public (Bearing requiredTrueCourse, Length crossTrackError, Length alongTrackDistance, Length turnRadius) CourseInterceptInfo => (_requiredTrueCourse, _xTk, _aTk, _turnRadius);

        private bool ShouldStartTurnToNextLeg(int intervalMs)
        {
            if (ActiveLeg == null || ActiveLeg.HasLegTerminated(_parentAircraft))
            {
                return true;
            }

            // If this and next leg connect, and a turn is involved
            IRouteLeg nextLeg = GetFirstLeg();

            return nextLeg != null &&
                ActiveLeg.EndPoint != null &&
                ActiveLeg.EndPoint.PointType == RoutePointTypeEnum.FLY_BY &&
                nextLeg.StartPoint != null &&
                ActiveLeg.EndPoint.Point.Equals(nextLeg.StartPoint.Point) &&
                ActiveLeg.FinalTrueCourse != null && nextLeg.InitialTrueCourse != null &&
                Math.Abs((nextLeg.InitialTrueCourse - ActiveLeg.FinalTrueCourse).Degrees) > 0.5 &&
                nextLeg.ShouldActivateLeg(_parentAircraft, intervalMs) && !Suspended;
        }

        private bool ShouldSequenceNextLeg(int intervalMs)
        {
            if (ActiveLeg == null || ActiveLeg.HasLegTerminated(_parentAircraft))
            {
                return true;
            }

            // If this and next leg connect, and a turn is involved
            IRouteLeg nextLeg = GetFirstLeg();

            if (ShouldStartTurnToNextLeg(intervalMs))
            {
                Bearing startBearing = ActiveLeg.FinalTrueCourse;
                Bearing endBearing = nextLeg.InitialTrueCourse;
                Angle turnAmt = endBearing - startBearing;

                // Find half turn and see if we've crossed abeam the point
                Bearing bisectorRadial = startBearing + (turnAmt / 2);

                (_, Length bisectorAtk, _) = AviationUtil.CalculateLinearCourseIntercept(_parentAircraft.Position.PositionGeoPoint, ActiveLeg.EndPoint.Point.PointPosition, bisectorRadial);

                return bisectorAtk.Meters <= 0;
            }

            return false;
        }

        public void OnPositionUpdate(int intervalMs)
        {
            // Method to set FMS phase
            SetPhase();

            // Set FMS Speed
            CalculateFmsSpeed();

            var position = _parentAircraft.Position;

            // Activate next leg if there's no active leg
            if (ActiveLeg == null)
            {
                if (GetFirstLeg() == null)
                {
                    return;
                }

                ActivateNextLeg();
            }

            // Process Leg
            ActiveLeg.ProcessLeg(_parentAircraft, intervalMs);

            // Check if leg has terminated
            bool hasLegTerminated = ShouldSequenceNextLeg(intervalMs);

            // Trigger Waypoint Passed if it has
            if (_aTk.Meters > 0 && ActiveLeg.EndPoint != null && ActiveLeg.FinalTrueCourse != null)
            {
                (_, Length act_atk_m, _) = AviationUtil.CalculateLinearCourseIntercept(_parentAircraft.Position.PositionGeoPoint, ActiveLeg.EndPoint.Point.PointPosition, ActiveLeg.FinalTrueCourse);
                if (act_atk_m.Meters <= 0 || hasLegTerminated)
                {
                    if (!_wpEvtTriggered)
                    {
                        WaypointPassed?.Invoke(this, new WaypointPassedEventArgs(ActiveLeg.EndPoint.Point));
                        _wpEvtTriggered = true;
                    }
                }
            }

            // Check if we should start turning towards the next leg
            IRouteLeg nextLeg = GetFirstLeg();

            // Only sequence if next leg exists and fms is not suspended
            if (nextLeg != null && !Suspended)
            {
                if (hasLegTerminated)
                {
                    // Activate next leg on termination
                    ActivateNextLeg();
                }
                else if (ShouldStartTurnToNextLeg(intervalMs))
                {
                    // Begin turn to next leg, but do not activate
                    nextLeg.ProcessLeg(_parentAircraft, intervalMs);
                    (_requiredTrueCourse, _xTk, _aTk, _turnRadius) = nextLeg.GetCourseInterceptInfo(_parentAircraft);
                    return;
                }
            }

            // Calculate course values
            (_requiredTrueCourse, _xTk, _aTk, _turnRadius) = ActiveLeg.GetCourseInterceptInfo(_parentAircraft);

            // Check if we need to recalculate remaining legs and VNAV crossing altitudes
            if (Math.Abs((_lastGs - position.GroundSpeed).MetersPerSecond) > MIN_GS_DIFF.MetersPerSecond)
            {
                RecalculateVnavPath();
                _lastGs = position.GroundSpeed;
            }

            // Calculate VNAV values
            (_requiredFpa, _vTk) = GetPitchInterceptInfoForCurrentLeg();
        }

        private void SetPhase()
        {
            if (PhaseType == FmsPhaseType.CLIMB && Math.Abs(_parentAircraft.Position.IndicatedAltitude.Feet - CruiseAltitude) < 50)
            {
                PhaseType = FmsPhaseType.CRUISE;
            }
            else if (PhaseType == FmsPhaseType.CRUISE && _parentAircraft.Autopilot.SelectedAltitude < CruiseAltitude - 100 &&
                (_parentAircraft.Autopilot.CurrentVerticalMode != VerticalModeType.ALT || _parentAircraft.Autopilot.CurrentVerticalMode != VerticalModeType.VALT))
            {
                PhaseType = FmsPhaseType.DESCENT;
            }
            // TODO: Need to implement Airport Distance
            else if (PhaseType == FmsPhaseType.DESCENT && _parentAircraft.Position.IndicatedAltitude < _parentAircraft.RelaventAirport.Elevation + Length.FromFeet(5000))
            {
                PhaseType = FmsPhaseType.APPROACH;
            }
            else if (PhaseType == FmsPhaseType.GO_AROUND && _parentAircraft.Position.IndicatedAltitude > _parentAircraft.RelaventAirport.Elevation + Length.FromFeet(1300))
            {
                PhaseType = FmsPhaseType.APPROACH;
            }
        }

        private void CalculateFmsSpeed()
        {
            if (PhaseType == FmsPhaseType.CLIMB)
            {
                if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.RelaventAirport.Elevation + Length.FromFeet(1000))
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 180;
                }
                else if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.RelaventAirport.Elevation + Length.FromFeet(3000))
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 210;
                }                
                else if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.RelaventAirport.Elevation + Length.FromFeet(10000))
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 250;                    
                }
                else
                {
                    // spd 270/.76 
                    var climbIas = _parentAircraft.PerformanceData.Climb_KIAS;
                    var climbMach = _parentAircraft.PerformanceData.Climb_Mach;

                    SetConversionSpeed(Velocity.FromKnots(climbIas), climbMach);
                }
            }
            else if (PhaseType == FmsPhaseType.CRUISE)
            {
                if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.RelaventAirport.Elevation + Length.FromFeet(10000))
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 250;
                }
                else
                {
                    // spd 270/.76 
                    var cruiseIas = _parentAircraft.PerformanceData.Cruise_KIAS;
                    var cruiseMach = _parentAircraft.PerformanceData.Cruise_Mach;

                    SetConversionSpeed(Velocity.FromKnots(cruiseIas), cruiseMach);
                }
            }
            else if (PhaseType == FmsPhaseType.DESCENT)
            {
                if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.RelaventAirport.Elevation + Length.FromFeet(10000))
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 250;
                }
                else
                {
                    var descentIas = _parentAircraft.PerformanceData.Descent_KIAS;
                    var descentMach = _parentAircraft.PerformanceData.Descent_Mach;

                    SetConversionSpeed(Velocity.FromKnots(descentIas), descentMach);
                }
            }
            else if (PhaseType == FmsPhaseType.APPROACH)
            {
                if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.RelaventAirport.Elevation + Length.FromFeet(1300))
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 135;
                }
                else if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.RelaventAirport.Elevation + Length.FromFeet(2000))
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 160;
                }
                else if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.RelaventAirport.Elevation + Length.FromFeet(3000))
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 180;
                }
                else
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 210;
                }
            }
            else if(PhaseType == FmsPhaseType.GO_AROUND)
            {
                _spdUnits = McpSpeedUnitsType.KNOTS;
                _selSpd = 135;
            }
        }
        private void SetConversionSpeed(Velocity ias, double mach)
        {
            double curIasMach;
            var gribPoint = _parentAircraft.Position.GribPoint;
            if (gribPoint != null)
            {
                (_, curIasMach) = AtmosUtil.ConvertIasToTas(ias, gribPoint.LevelPressure, _parentAircraft.Position.TrueAltitude, gribPoint.GeoPotentialHeight, gribPoint.Temp);
            }
            else
            {
                (_, curIasMach) = AtmosUtil.ConvertIasToTas(ias, AtmosUtil.ISA_STD_PRES, _parentAircraft.Position.TrueAltitude, (Length) 0, AtmosUtil.ISA_STD_TEMP);
            }

            if (curIasMach >= mach)
            {
                _spdUnits = McpSpeedUnitsType.MACH;
                _selSpd = (int)(mach * 100);
            }
            else
            {
                _spdUnits = McpSpeedUnitsType.KNOTS;
                _selSpd = (int) ias.Knots;
            }
        }
        private void RecalculateVnavPath()
        {
            lock (_routeLegsLock)
            {
                if (_routeLegs != null)
                {
                    // Go through route legs in reverse
                    for (int i = _routeLegs.Count - 1; i >= 0; i--)
                    {
                        var leg = _routeLegs[i];

                        // Update leg dimensions
                        leg.InitializeLeg(_parentAircraft);

                        // Update VNAV info
                        if (leg.EndPoint != null)
                        {
                            // TODO: Change this to actually calculate VNAV paths
                            leg.EndPoint.VnavTargetAltitude = leg.EndPoint.LowerAltitudeConstraint;
                        }
                    }
                }
                if (_activeLeg != null && _activeLeg.EndPoint != null)
                {
                    // TODO: Change this to actually calculate VNAV paths
                    _activeLeg.EndPoint.VnavTargetAltitude = _activeLeg.EndPoint.LowerAltitudeConstraint;
                }
            }
        }

        private (Angle requiredFpa, Length vTk_m) GetPitchInterceptInfoForCurrentLeg()
        {
            if (_activeLeg.EndPoint == null || _activeLeg.EndPoint.VnavTargetAltitude < 0 || _activeLeg.EndPoint.AngleConstraint < 0)
            {
                return ((Angle)0, (Length)0);
            }

            // TODO: Change this to actually figure out the angle
            Angle requiredFpa = Angle.FromDegrees(_activeLeg.EndPoint.AngleConstraint);

            // Calculate how much altitude we still need to climb/descend from here to the EndPoint
            Length deltaAlt_m = Length.FromMeters(Math.Tan(requiredFpa.Radians) * _aTk.Meters);

            // Add to Vnav target alt
            Length altTarget_m = deltaAlt_m + Length.FromFeet(_activeLeg.EndPoint.VnavTargetAltitude);

            Length vTk_m = _parentAircraft.Position.TrueAltitude - altTarget_m;

            return (requiredFpa, vTk_m);
        }
    }
}
