using AviationCalcUtilNet.GeoTools;
using SaunaSim.Core.Data;
using NavData_Interface.Objects.Fix;

namespace SaunaSim.Core.Simulator.Aircraft.FMS
{
    public class RouteWaypoint : IRoutePoint
    {
        private string _waypointName;
        private GeoPoint _pointPosition;

        public RouteWaypoint(Fix wp)
        {
            _waypointName = wp.Identifier;
            _pointPosition = new GeoPoint(wp.Location);
        }

        internal RouteWaypoint(GeoPoint pointPosition)
        {
            _waypointName = "";
            _pointPosition = new GeoPoint(pointPosition);
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
