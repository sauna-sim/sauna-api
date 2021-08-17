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
    public class RoutePointPbd : IRoutePoint
    {
        private string _waypointName;
        private GeoPoint _pointPosition;
        private double _bearing;
        private double _distance;

        public RoutePointPbd(Waypoint waypoint, double bearing, double distance)
        {
            _waypointName = waypoint.Identifier;
            _bearing = AcftGeoUtil.NormalizeHeading(bearing);
            _distance = distance;
            _pointPosition = new GeoPoint(waypoint.Location);
            _pointPosition.MoveByNMi(_bearing, _distance);
        }

        public GeoPoint PointPosition => _pointPosition;

        public string PointName => $"{_waypointName}{_bearing:000}{_distance:000}";

        // override object.Equals
        public override bool Equals(object obj)
        {
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            IRoutePoint wpt = (IRoutePoint)obj;
            return wpt.PointPosition == PointPosition && PointName == wpt.PointName;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return _pointPosition.GetHashCode();
        }

        public override string ToString()
        {
            return PointName;
        }
    }
}
