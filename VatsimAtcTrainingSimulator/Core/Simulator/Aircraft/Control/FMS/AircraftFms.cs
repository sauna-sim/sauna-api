using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.GeoTools;
using VatsimAtcTrainingSimulator.Core.GeoTools.Helpers;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS.Legs;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS
{
    public class AircraftFms
    {
        private Waypoint _depApt;
        private Waypoint _arrApt;
        private int _cruiseAlt;
        private IRouteLeg _activeLeg;
        private List<IRouteLeg> _routeLegs;
        private object _routeLegsLock;
        private bool _suspended;

        public EventHandler<WaypointPassedEventArgs> WaypointPassed;

        public AircraftFms()
        {
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

        public Waypoint DepartureAirport
        {
            get => _depApt;
            set => _depApt = value;
        }

        public Waypoint ArrivalAirport
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
                foreach (IRouteLeg leg in _routeLegs)
                {
                    if (leg.EndPoint != null && leg.EndPoint.Point.Equals(routePoint))
                    {
                        index = _routeLegs.IndexOf(leg) + 1;
                        point = leg.EndPoint;
                        break;
                    }
                }

                if (point == null)
                {
                    point = new FmsPoint(routePoint, RoutePointTypeEnum.FLY_BY);
                }

                // Create direct leg
                IRouteLeg dtoLeg = new DirectToFixLeg(point);

                // Add leg
                _routeLegs.Insert(index, dtoLeg);

                // Remove everything before index
                _routeLegs.RemoveRange(0, index);
            }
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
    }
}
