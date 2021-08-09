using CoordinateSharp;
using CoordinateSharp.Magnetic;
using System;

namespace VatsimAtcTrainingSimulator.Core.GeoTools
{
    public class AcftData
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double IndicatedAltitude { get; private set; }
        public double StaticAirTemperature { get; private set; }
        public double WindDirection { get; private set; }
        public double WindSpeed { get; private set; }
        public double PressureAltitude => AcftGeoUtil.CalculatePressureAlt(IndicatedAltitude, AltimeterSetting_hPa);
        public double DensityAltitude => AcftGeoUtil.CalculateDensityAlt(PressureAltitude, StaticAirTemperature);
        public double AbsoluteAltitude => AcftGeoUtil.CalculateAbsoluteAlt(IndicatedAltitude, AltimeterSetting_hPa, SurfacePressure_hPa);
        public double Heading_Mag { get; set; }
        public double Bank { get; set; }
        public double Pitch { get; set; }
        public double AltimeterSetting_hPa { get; set; } = AcftGeoUtil.STD_PRES_HPA;
        public double SurfacePressure_hPa { get; set; } = AcftGeoUtil.STD_PRES_HPA;

        public double Heading_True
        {
            get
            {
                Coordinate coord = new Coordinate(Latitude, Longitude, DateTime.UtcNow);
                Magnetic m = new Magnetic(coord, IndicatedAltitude / 3.28084, DataModel.WMM2020);
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

        public double TrueAirSpeed => AcftGeoUtil.CalculateTAS(IndicatedAirSpeed, AltimeterSetting_hPa, IndicatedAltitude, StaticAirTemperature);

        public double GroundSpeed => TrueAirSpeed - WindHComp;

        public void UpdatePosition(double lat, double lon, double alt)
        {
            Latitude = lat;
            Longitude = lon;
            IndicatedAltitude = alt;

            GribDataPoint point = GribUtil.GetClosestGribPoint(this);
            if (point == null)
            {
                WindDirection = 0;
                WindSpeed = 0;
                SurfacePressure_hPa = AcftGeoUtil.STD_PRES_HPA;
                StaticAirTemperature = AcftGeoUtil.CalculateIsaTemp(alt);
            }
            else
            {
                WindDirection = point.WDir_deg;
                WindSpeed = point.WSpeed_kts;
                if (point.SfcPress_hPa != 0)
                {
                    SurfacePressure_hPa = point.SfcPress_hPa;
                } else
                {
                    SurfacePressure_hPa = AcftGeoUtil.STD_PRES_HPA;
                }
                StaticAirTemperature = point.Temp_C;
            }
        }
    }
}
