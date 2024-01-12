using AviationCalcUtilNet.GeoTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaunaSim.Core.Simulator.Aircraft
{
    public static class AircraftPositionUtil
    {
        public static void SetNextLatLon(ref AircraftPosition position, double distanceNMi)
        {
            //GeoPoint point = new GeoPoint(position.PositionGeoPoint);
            //point.MoveByNMi(position.Track_True, distanceNMi);
            //position.Latitude = point.Lat;
            //position.Longitude = point.Lon;
        }
    }
}
