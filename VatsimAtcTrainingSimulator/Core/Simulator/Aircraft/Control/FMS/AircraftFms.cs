using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.GeoTools;
using VatsimAtcTrainingSimulator.Core.GeoTools.Helpers;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS
{
    public class AircraftFms
    {
        private Waypoint _depApt;
        private Waypoint _arrApt;
        private int _cruiseAlt;
        private List<IRouteLeg> _routeLegs;
        private object _routeLegsLock;

        public AircraftFms()
        {
            _routeLegsLock = new object();

            lock (_routeLegsLock)
            {
                _routeLegs = new List<IRouteLeg>();
            }
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

        public IRouteLeg GetLegToPoint(IRoutePoint routePoint)
        {
            lock (_routeLegsLock)
            {
                foreach (IRouteLeg leg in _routeLegs)
                {
                    if (leg.EndPoint == routePoint)
                    {
                        return leg;
                    }
                }
            }

            return null;
        }

        public void ActivateDirectTo(IRoutePoint routePoint, AircraftPosition pos)
        {
            lock (_routeLegsLock)
            {
                int index = 0;
                FmsPoint point = null;
                foreach (IRouteLeg leg in _routeLegs)
                {
                    if (leg.StartPoint != null && leg.StartPoint.Point == routePoint)
                    {
                        index = _routeLegs.IndexOf(leg);
                        point = leg.StartPoint;
                        break;
                    }
                }

                if (point == null)
                {
                    point = new FmsPoint(routePoint, RoutePointTypeEnum.FLY_BY);
                }

                double dctCourse = AcftGeoUtil.CalculateDirectBearingAfterTurn(
                        new GeoPoint(pos.Latitude, pos.Longitude, pos.AbsoluteAltitude),
                        routePoint.PointPosition,
                        AcftGeoUtil.CalculateRadiusOfTurn(AcftGeoUtil.CalculateBankAngle(pos.GroundSpeed, 25, 3), pos.GroundSpeed),
                        pos.Track_True);

                if (dctCourse >= 0)
                {
                    // Create direct leg
                    IRouteLeg dtoLeg = new CourseToPointLeg(
                        point,
                        InterceptTypeEnum.TRUE_TRACK,
                        dctCourse);

                    // Add leg
                    _routeLegs.Insert(index, dtoLeg);

                    // Remove everything before index
                    _routeLegs.RemoveRange(0, index);
                }
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
