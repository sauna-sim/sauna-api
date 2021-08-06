using CoordinateSharp;
using CoordinateSharp.Magnetic;
using System;

namespace VatsimAtcTrainingSimulator.Core.GeoTools
{
    public class AcftData
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double Altitude { get; private set; }
        public double StaticAirTemperature { get; private set; }
        public double WindDirection { get; private set; }
        public double WindSpeed { get; private set; }
        public double PressureAltitude => AcftGeoUtil.CalculatePressureAlt(Altitude, AltimeterSetting_hPa);
        public double DensityAltitude => AcftGeoUtil.CalculateDensityAlt(PressureAltitude, StaticAirTemperature);
        public double Heading_Mag { get; private set; }
        public double AltimeterSetting_hPa { get; private set; }

        public double Heading_True
        {
            get
            {
                Coordinate coord = new Coordinate(Latitude, Longitude, DateTime.UtcNow);
                Magnetic m = new Magnetic(coord, Altitude / 3.28084, DataModel.WMM2020);
                double declin = Math.Round(m.MagneticFieldElements.Declination, 1);
                return Heading_Mag - declin;
            }
        }

        public double WindXComp => WindSpeed * Math.Sin((Heading_True - WindDirection) * Math.PI / 180.0);

        public double WindHComp => WindSpeed * Math.Cos((Heading_True - WindDirection) * Math.PI / 180.0);

        public double Track_True
        {
            get
            {
                double wca = Math.Acos(WindXComp / TrueAirSpeed);
                return Heading_True + wca;
            }
        }

        public double IndicatedAirSpeed { get; set; }

        public double TrueAirSpeed => AcftGeoUtil.CalculateTAS(IndicatedAirSpeed, AltimeterSetting_hPa, Altitude, StaticAirTemperature);

        public double GroundSpeed => TrueAirSpeed - WindHComp;

        public async void UpdatePosition(double lat, double lon, double alt, double hdg, double ias)
        {
            Latitude = lat;
            Longitude = lon;
            Altitude = alt;
            Heading_Mag = hdg;
            IndicatedAirSpeed = ias;
            AltimeterSetting_hPa = 1013;

            GribDataPoint point = await GribUtil.GetClosestGribPoint(this);
            if (point == null)
            {
                WindDirection = 0;
                WindSpeed = 0;
                StaticAirTemperature = AcftGeoUtil.CalculateIsaTemp(alt);
            }
            else
            {
                WindDirection = point.WDir_deg;
                WindSpeed = point.WSpeed_kts;
                StaticAirTemperature = point.Temp_C;
            }
        }
    }
}
