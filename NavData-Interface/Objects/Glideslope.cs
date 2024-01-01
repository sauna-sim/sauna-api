using AviationCalcUtilNet.GeoTools;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects
{
    public class Glideslope
    {
        public GeoPoint Gs_location { get; }

        public double Gs_angle { get; }

        public int Gs_elevation { get; }

        public Glideslope(
            GeoPoint gs_location, 
            double gs_angle,
            int gs_elevation
            )
        {
            Gs_location = gs_location;
            Gs_angle = gs_angle;
            Gs_elevation = gs_elevation;
        }

        public override string ToString()
        {
            return $"Glideslope: Located at ({Gs_location.Lat}, {Gs_location.Lon})\n" +
                $"Angle: {Gs_angle} Elevation: {Gs_elevation}";
        }
    }
}
