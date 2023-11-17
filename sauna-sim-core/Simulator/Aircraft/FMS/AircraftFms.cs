using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AviationCalcUtilNet.GeoTools;
using NavData_Interface.Objects.Fix;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;

namespace SaunaSim.Core.Simulator.Aircraft.FMS
{
    public class AircraftFms
    {
        private SimAircraft _parentAircraft;
        private Fix _depApt;
        private Fix _arrApt;
        private int _cruiseAlt;
        private IRouteLeg _activeLeg;
        private List<IRouteLeg> _routeLegs;
        private object _routeLegsLock;
        private bool _suspended;

        // Fms Values
        private double _xTk_m;
        private double _aTk_m;
        private double _requiredTrueCourse;
        private double _turnRadius_m;

        public EventHandler<WaypointPassedEventArgs> WaypointPassed;

        public AircraftFms(SimAircraft parentAircraft)
        {
            _parentAircraft = parentAircraft;
            _routeLegsLock = new object();
            _xTk_m = -1;
            _aTk_m = -1;
            _requiredTrueCourse = -1;
            _turnRadius_m = 0;

            lock (_routeLegsLock)
            {
                _routeLegs = new List<IRouteLeg>();
            }

            _suspended = false;
        }

        public double AlongTrackDistance_m => _aTk_m;

        public double CrossTrackDistance_m => _xTk_m;

        public double RequiredTrueCourse => _requiredTrueCourse;

        public double TurnRadius_m => _turnRadius_m;

        public bool Suspended
        {
            get => _suspended;
            set => _suspended = value;
        }

        public int CruiseAltitude
        {
            get => _cruiseAlt;
            set => _cruiseAlt = value;
        }

        public Fix DepartureAirport
        {
            get => _depApt;
            set => _depApt = value;
        }

        public Fix ArrivalAirport
        {
            get => _arrApt;
            set => _arrApt = value;
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

        public void AddRouteLeg(IRouteLeg routeLeg)
        {
            lock (_routeLegsLock)
            {
                _routeLegs.Add(routeLeg);
            }
        }

        public IRouteLeg ActivateNextLeg()
        {
            lock (_routeLegsLock)
            {
                if (_routeLegs.Count > 0)
                {
                    _activeLeg = _routeLegs[0];
                    _routeLegs.RemoveAt(0);
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
                } else if (_activeLeg != null && _activeLeg.EndPoint != null && _activeLeg.EndPoint.Point.Equals(routePoint))
                {
                    point = _activeLeg.EndPoint;
                    index = 0;
                } else
                {
                    foreach (IRouteLeg leg in _routeLegs)
                    {
                        if (leg.StartPoint != null && leg.StartPoint.Point.Equals(routePoint))
                        {
                            index = _routeLegs.IndexOf(leg);
                            point = leg.StartPoint;
                            break;
                        } else if (leg.EndPoint != null && leg.EndPoint.Point.Equals(routePoint))
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

                if (index >= 0)
                {
                    // Remove everything before index
                    _routeLegs.RemoveRange(0, index);
                } else
                {
                    _routeLegs.Insert(0, new DiscoLeg(dtoLeg.FinalTrueCourse));
                }
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
                } else
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
                    WaypointPassed?.Invoke(this, new WaypointPassedEventArgs(ActiveLeg.EndPoint.Point));
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
                } else if (ShouldStartTurnToNextLeg(intervalMs))
                {
                    // Begin turn to next leg, but do not activate
                    nextLeg.ProcessLeg(_parentAircraft, intervalMs);
                    (_requiredTrueCourse, _xTk_m, _aTk_m, _turnRadius_m) = nextLeg.GetCourseInterceptInfo(_parentAircraft);
                    return;
                }
            }

            // Calculate course values
            (_requiredTrueCourse, _xTk_m, _aTk_m, _turnRadius_m) = ActiveLeg.GetCourseInterceptInfo(_parentAircraft);
        }
    }
}
