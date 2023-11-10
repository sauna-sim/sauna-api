using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public EventHandler<WaypointPassedEventArgs> WaypointPassed;

        public AircraftFms(SimAircraft parentAircraft)
        {
            _parentAircraft = parentAircraft;
            _routeLegsLock = new object();

            lock (_routeLegsLock)
            {
                _routeLegs = new List<IRouteLeg>();
            }

            _suspended = false;
        }

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

        public void ActivateDirectTo(IRoutePoint routePoint)
        {
            lock (_routeLegsLock)
            {
                int index = 0;
                FmsPoint point = null;

                if (_activeLeg != null && _activeLeg.EndPoint != null && _activeLeg.EndPoint.Point.Equals(routePoint))
                {
                    point = _activeLeg.EndPoint;
                    index = -1;
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

                _activeLeg = dtoLeg;

                if (index >= 0)
                {
                    // Remove everything before index
                    _routeLegs.RemoveRange(0, index);
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
            IRouteLeg leg = GetFirstLeg();

            return leg?.ShouldActivateLeg(_parentAircraft, intervalMs) ?? false;
        }

        public void OnPositionUpdate(int intervalMs)
        {
            var position = _parentAircraft.Position;

            // Activate next leg if there's no active leg
            if (ActiveLeg == null)
            {
                ActivateNextLeg();
            }

            // Only sequence if next leg exists and fms is not suspended
            if (GetFirstLeg() != null && !Suspended)
            {
                if (ActiveLeg?.HasLegTerminated(_parentAircraft) ?? true)
                {
                    // Activate next leg on termination
                    ActivateNextLeg();
                }
            }
        }
    }
}
