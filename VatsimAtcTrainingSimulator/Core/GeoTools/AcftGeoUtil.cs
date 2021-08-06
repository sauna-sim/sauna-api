using CoordinateSharp;
using Grib.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.GeoTools
{
    public class AcftPosition
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
    }

    public static class AcftGeoUtil
    {
        public static AcftPosition CalculateNextLatLon(AcftPosition pos, double trueBearingDeg, double gsKts, double vsFtMin, double timeMs)
        {
            Coordinate start = new Coordinate(pos.Latitude, pos.Longitude);
            double distanceNMi = (timeMs / 1000.0) * (gsKts / 60.0 / 60.0);
            double distanceM = distanceNMi * 1852;
            start.Move(distanceM, trueBearingDeg, Shape.Ellipsoid);
            pos.Latitude = start.Latitude.ToDouble();
            pos.Longitude = start.Longitude.ToDouble();
            pos.Altitude = pos.Altitude + ((timeMs / 1000.0) * (vsFtMin / 60.0));

            return pos;
        }
    }
}
