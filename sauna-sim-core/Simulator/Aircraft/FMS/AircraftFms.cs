using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.MathTools;
using FsdConnectorNet;
using FsdConnectorNet.Args;
using NavData_Interface.Objects.Fix;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.Autopilot;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;

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
        private static double MIN_GS_DIFF = 10;
        private double _lastGs;
        private bool _wpEvtTriggered = false;

        // Fms Values
        private double _xTk_m;
        private double _aTk_m;
        private double _requiredTrueCourse;
        private double _turnRadius_m;
        private double _vTk_m;
        private double _requiredFpa;
        private McpSpeedUnitsType _spdUnits;
        private int _selSpd;

        public EventHandler<WaypointPassedEventArgs> WaypointPassed;

        public AircraftFms(SimAircraft parentAircraft)
        {
            _parentAircraft = parentAircraft;
            _routeLegsLock = new object();
            _xTk_m = -1;
            _aTk_m = -1;
            _requiredTrueCourse = -1;
            _vTk_m = -1;
            _requiredFpa = 0;
            _turnRadius_m = 0;

            lock (_routeLegsLock)
            {
                _routeLegs = new List<IRouteLeg>();
            }

            _suspended = false;

            PhaseType = FmsPhaseType.CRUISE;
        }

        public double AlongTrackDistance_m => _aTk_m;

        public double CrossTrackDistance_m => _xTk_m;

        public double RequiredTrueCourse => _requiredTrueCourse;

        public double TurnRadius_m => _turnRadius_m;

        public double VerticalTrackDistance_m => _vTk_m;

        public double RequiredFpa => _requiredFpa;

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
                        _routeLegs.Insert(1, new DiscoLeg(0));
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

        public void ActivateDirectTo(IRoutePoint routePoint, double course = -1)
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
                IRouteLeg dtoLeg = new DirectToFixLeg(point, _parentAircraft.Position.PositionGeoPoint, _parentAircraft.Position.Track_True, _parentAircraft.Position.GroundSpeed);

                if (course >= 0)
                {
                    dtoLeg = new CourseToFixLeg(point, BearingTypeEnum.MAGNETIC, course);
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

        public bool AddHold(IRoutePoint rp, double magCourse, HoldTurnDirectionEnum turnDir, HoldLegLengthTypeEnum legLengthType, double legLength)
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
                    IRouteLeg holdLeg = new HoldToManualLeg(point, BearingTypeEnum.MAGNETIC, magCourse, turnDir, legLengthType, legLength);

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

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance, double turnRadius) CourseInterceptInfo => (_requiredTrueCourse, _xTk_m, _aTk_m, _turnRadius_m);

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
                ActiveLeg.FinalTrueCourse >= 0 && nextLeg.InitialTrueCourse >= 0 &&
                Math.Abs(GeoUtil.CalculateTurnAmount(ActiveLeg.FinalTrueCourse, nextLeg.InitialTrueCourse)) > 0.5 &&
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
                double startBearing = ActiveLeg.FinalTrueCourse;
                double endBearing = nextLeg.InitialTrueCourse;
                double turnAmt = GeoUtil.CalculateTurnAmount(startBearing, endBearing);

                // Find half turn and see if we've crossed abeam the point
                double bisectorRadial = GeoUtil.NormalizeHeading(startBearing + (turnAmt / 2));

                GeoUtil.CalculateCrossTrackErrorM(_parentAircraft.Position.PositionGeoPoint, ActiveLeg.EndPoint.Point.PointPosition, bisectorRadial, out _, out double bisectorAtk);

                return bisectorAtk <= 0;
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
            if (_aTk_m > 0 && ActiveLeg.EndPoint != null && ActiveLeg.FinalTrueCourse >= 0)
            {
                GeoUtil.CalculateCrossTrackErrorM(_parentAircraft.Position.PositionGeoPoint, ActiveLeg.EndPoint.Point.PointPosition, ActiveLeg.FinalTrueCourse, out _, out double act_atk_m);
                if (act_atk_m <= 0 || hasLegTerminated)
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
                    (_requiredTrueCourse, _xTk_m, _aTk_m, _turnRadius_m) = nextLeg.GetCourseInterceptInfo(_parentAircraft);
                    return;
                }
            }

            // Calculate course values
            (_requiredTrueCourse, _xTk_m, _aTk_m, _turnRadius_m) = ActiveLeg.GetCourseInterceptInfo(_parentAircraft);

            // Check if we need to recalculate remaining legs and VNAV crossing altitudes
            if (Math.Abs(_lastGs - position.GroundSpeed) > MIN_GS_DIFF)
            {
                RecalculateVnavPath();
                _lastGs = position.GroundSpeed;
            }

            // Calculate VNAV values
            (_requiredFpa, _vTk_m) = GetPitchInterceptInfoForCurrentLeg();
        }

        private void SetPhase()
        {
            if (PhaseType == FmsPhaseType.CLIMB && Math.Abs(_parentAircraft.Position.IndicatedAltitude - CruiseAltitude) < 50)
            {
                PhaseType = FmsPhaseType.CRUISE;
            }
            else if (PhaseType == FmsPhaseType.CRUISE && _parentAircraft.Autopilot.SelectedAltitude < CruiseAltitude - 100 &&
                (_parentAircraft.Autopilot.CurrentVerticalMode != VerticalModeType.ALT || _parentAircraft.Autopilot.CurrentVerticalMode != VerticalModeType.VALT))
            {
                PhaseType = FmsPhaseType.DESCENT;
            }
            // TODO: Need to implement Airport Distance
            else if (PhaseType == FmsPhaseType.DESCENT && _parentAircraft.Position.IndicatedAltitude < _parentAircraft.airportElev + 5000)
            {
                PhaseType = FmsPhaseType.APPROACH;
            }
            else if (PhaseType == FmsPhaseType.GO_AROUND && _parentAircraft.Position.IndicatedAltitude > _parentAircraft.airportElev + 1300)
            {
                PhaseType = FmsPhaseType.APPROACH;
            }
        }

        private void CalculateFmsSpeed()
        {
            if (PhaseType == FmsPhaseType.CLIMB)
            {
                if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.airportElev + 1000)
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 180;
                }
                else if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.airportElev + 3000)
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 210;
                }
                else if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.airportElev + 10000)
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 250;
                }
                else
                {
                    // spd 270/.76 
                    var climbIas = _parentAircraft.PerformanceData.Climb_KIAS;
                    var climbMach = _parentAircraft.PerformanceData.Climb_Mach;

                    SetConversionSpeed(climbIas, climbMach);
                }
            }
            else if (PhaseType == FmsPhaseType.CRUISE)
            {
                if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.airportElev + 10000)
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 250;
                }
                else
                {
                    // spd 270/.76 
                    var cruiseIas = _parentAircraft.PerformanceData.Cruise_KIAS;
                    var cruiseMach = _parentAircraft.PerformanceData.Cruise_Mach;

                    SetConversionSpeed(cruiseIas, cruiseMach);
                }
            }
            else if (PhaseType == FmsPhaseType.DESCENT)
            {
                if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.airportElev + 10000)
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 250;
                }
                else
                {
                    var descentIas = _parentAircraft.PerformanceData.Descent_KIAS;
                    var descentMach = _parentAircraft.PerformanceData.Descent_Mach;

                    SetConversionSpeed(descentIas, descentMach);
                }
            }
            else if (PhaseType == FmsPhaseType.APPROACH)
            {
                if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.airportElev + 1300)
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 135;
                }
                else if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.airportElev + 2000)
                {
                    _spdUnits = McpSpeedUnitsType.KNOTS;
                    _selSpd = 160;
                }
                else if (_parentAircraft.Position.IndicatedAltitude < _parentAircraft.airportElev + 3000)
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
        private void SetConversionSpeed(int ias, double mach)
        {
            double curIasMach;
            var gribPoint = _parentAircraft.Position.GribPoint;
            if (gribPoint != null)
            {
                AtmosUtil.ConvertIasToTas(ias, gribPoint.Level_hPa, _parentAircraft.Position.TrueAltitude, gribPoint.GeoPotentialHeight_Ft, gribPoint.Temp_K, out curIasMach);
            }
            else
            {
                AtmosUtil.ConvertIasToTas(ias, AtmosUtil.ISA_STD_PRES_hPa, _parentAircraft.Position.TrueAltitude, 0, AtmosUtil.ISA_STD_TEMP_K, out curIasMach);
            }

            if (curIasMach >= mach)
            {
                _spdUnits = McpSpeedUnitsType.MACH;
                _selSpd = (int)(mach * 100);
            }
            else
            {
                _spdUnits = McpSpeedUnitsType.KNOTS;
                _selSpd = ias;
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

        private (double requiredFpa, double vTk_m) GetPitchInterceptInfoForCurrentLeg()
        {
            if (_activeLeg.EndPoint == null || _activeLeg.EndPoint.VnavTargetAltitude < 0 || _activeLeg.EndPoint.AngleConstraint < 0)
            {
                return (0, 0);
            }

            // TODO: Change this to actually figure out the angle
            double requiredFpa = _activeLeg.EndPoint.AngleConstraint;

            // Calculate how much altitude we still need to climb/descend from here to the EndPoint
            double deltaAlt_m = Math.Tan(MathUtil.ConvertDegreesToRadians(requiredFpa)) * _aTk_m;

            // Add to Vnav target alt
            double altTarget_m = deltaAlt_m + MathUtil.ConvertFeetToMeters(_activeLeg.EndPoint.VnavTargetAltitude);

            double vTk_m = MathUtil.ConvertFeetToMeters(_parentAircraft.Position.TrueAltitude) - altTarget_m;

            return (requiredFpa, vTk_m);
        }
    }
}
