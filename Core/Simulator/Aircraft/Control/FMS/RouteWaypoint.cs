using AviationCalcUtilNet.GeoTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS
{
    class RouteWaypoint : IRoutePoint
    {
        private string _waypointName;
        private GeoPoint _pointPosition;

        public RouteWaypoint(Waypoint wp)
        {
            _waypointName = wp.Identifier;
            _pointPosition = new GeoPoint(wp.Location);
        }

        public GeoPoint PointPosition => _pointPosition;

        public string PointName => _waypointName;

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            IRoutePoint wpt = (IRoutePoint)obj;
            return wpt.PointPosition.Equals(PointPosition) && PointName == wpt.PointName;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return _pointPosition.GetHashCode();
        }

        public override string ToString()
        {
            return _waypointName;
        }
    }
}
