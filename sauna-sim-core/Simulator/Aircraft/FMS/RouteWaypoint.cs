using AviationCalcUtilNet.GeoTools;
using NavData_Interface.Objects.Fixes;
using SaunaSim.Core.Data;

namespace SaunaSim.Core.Simulator.Aircraft.FMS
{
    public class RouteWaypoint : IRoutePoint
    {
        private string _waypointName;
        private GeoPoint _pointPosition;

        public RouteWaypoint(Fix wp)
        {
            _waypointName = wp.Identifier;
            _pointPosition = (GeoPoint) wp.Location.Clone();
        }

        internal RouteWaypoint(GeoPoint pointPosition)
        {
            _waypointName = "";
            _pointPosition = (GeoPoint)pointPosition.Clone();
        }

        internal RouteWaypoint(string name, GeoPoint pointPosition)
        {
            _waypointName = name;
            _pointPosition = (GeoPoint)pointPosition.Clone();
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
