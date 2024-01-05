using AviationCalcUtilNet.GeoTools;
using SaunaSim.Core.Data;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.Units;
using NavData_Interface.Objects.Fixes;

namespace SaunaSim.Core.Simulator.Aircraft.FMS
{
    public class RoutePointPbd : IRoutePoint
    {
        private string _waypointName;
        private GeoPoint _pointPosition;
        private Bearing _bearing;
        private Length _distance;

        public RoutePointPbd(Fix waypoint, Bearing bearing, Length distance) : this(waypoint.Location, bearing, distance, waypoint.Identifier) { }

        public RoutePointPbd(GeoPoint pos, Bearing bearing, Length distance, string name)
        {
            _waypointName = name;
            _bearing = bearing;
            _distance = distance;
            _pointPosition = (GeoPoint)pos.Clone();
            _pointPosition.MoveBy(_bearing, _distance);
        }

        public GeoPoint PointPosition => _pointPosition;

        public string PointName => $"{_waypointName}{_bearing:000}{_distance:000}";

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
            return PointName;
        }
    }
}
