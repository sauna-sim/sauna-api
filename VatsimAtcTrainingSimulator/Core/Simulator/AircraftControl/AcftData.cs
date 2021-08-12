using CoordinateSharp;
using CoordinateSharp.Magnetic;
using System;
using VatsimAtcTrainingSimulator.Core.GeoTools;

namespace VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl
{
    public class AcftData
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double IndicatedAltitude { get; set; }
        public double StaticAirTemperature { get; private set; }
        public double WindDirection { get; private set; }
        public double WindSpeed { get; private set; }
        public double PressureAltitude => AcftGeoUtil.CalculatePressureAlt(IndicatedAltitude, AltimeterSetting_hPa);
        public double DensityAltitude => AcftGeoUtil.CalculateDensityAlt(PressureAltitude, StaticAirTemperature);
        public double AbsoluteAltitude => AcftGeoUtil.CalculateAbsoluteAlt(IndicatedAltitude, AltimeterSetting_hPa, SurfacePressure_hPa);

        private double _magneticHdg;
        public double Heading_Mag
        {
            get => _magneticHdg;

            set
            {
                _magneticHdg = value;

                // Calculate True Heading
                Coordinate coord = new Coordinate(Latitude, Longitude, DateTime.UtcNow);
                Magnetic m = new Magnetic(coord, IndicatedAltitude / 3.28084, DataModel.WMM2015);
                double declin = Math.Round(m.MagneticFieldElements.Declination, 1);
                _trueHdg = AcftGeoUtil.NormalizeHeading(_magneticHdg + declin);

                // Calculate True Track
                double wca = TrueAirSpeed == 0 ? 0 : Math.Acos(WindXComp / TrueAirSpeed);
                _trueTrack = AcftGeoUtil.NormalizeHeading(_trueHdg + wca);
            }
        }
        public double Bank { get; set; }
        public double Pitch { get; set; }
        public double VerticalSpeed { get; set; }
        private double _altSetting_hPa = AcftGeoUtil.STD_PRES_HPA;
        public double AltimeterSetting_hPa
        {
            get => _altSetting_hPa; set
            {
                _altSetting_hPa = value;

                // Backwards compute new Indicated Alt
                IndicatedAltitude = AcftGeoUtil.CalculateIndicatedAlt(AbsoluteAltitude, value, SurfacePressure_hPa);
            }
        }
        public double SurfacePressure_hPa { get; set; } = AcftGeoUtil.STD_PRES_HPA;
        public int PresAltDiff => (int)((AcftGeoUtil.STD_PRES_HPA - (SurfacePressure_hPa == 0 ? AcftGeoUtil.STD_PRES_HPA : SurfacePressure_hPa)) * AcftGeoUtil.STD_PRES_DROP);

        private double _trueHdg;
        public double Heading_True
        {
            get => _trueHdg;

            set
            {
                _trueHdg = value;

                // Set Magnetic Heading
                Coordinate coord = new Coordinate(Latitude, Longitude, DateTime.UtcNow);
                Magnetic m = new Magnetic(coord, IndicatedAltitude / 3.28084, DataModel.WMM2015);
                double declin = Math.Round(m.MagneticFieldElements.Declination, 1);
                _magneticHdg = AcftGeoUtil.NormalizeHeading(_trueHdg - declin);

                // Calculate True Track
                double wca = Math.Acos(WindXComp / TrueAirSpeed);
                _trueTrack = AcftGeoUtil.NormalizeHeading(_trueHdg + wca);
            }
        }

        public double WindXComp => WindSpeed * Math.Sin((Heading_True - WindDirection) * Math.PI / 180.0);

        public double WindHComp => WindSpeed * Math.Cos((Heading_True - WindDirection) * Math.PI / 180.0);

        private double _trueTrack;
        public double Track_True
        {
            get => _trueTrack;

            set
            {
                _trueTrack = value;

                // Calculate True Heading
                double wca = Math.Acos(WindXComp / TrueAirSpeed);
                _trueHdg = AcftGeoUtil.NormalizeHeading(_trueTrack - wca);

                // Set Magnetic Heading
                Coordinate coord = new Coordinate(Latitude, Longitude, DateTime.UtcNow);
                Magnetic m = new Magnetic(coord, IndicatedAltitude / 3.28084, DataModel.WMM2015);
                double declin = Math.Round(m.MagneticFieldElements.Declination, 1);
                _magneticHdg = AcftGeoUtil.NormalizeHeading(_trueHdg - declin);
            }
        }

        public double IndicatedAirSpeed { get; set; }

        public double TrueAirSpeed => AcftGeoUtil.CalculateTAS(IndicatedAirSpeed, AltimeterSetting_hPa, IndicatedAltitude, StaticAirTemperature);

        public double GroundSpeed => TrueAirSpeed - WindHComp;

        public void UpdatePosition()
        {
            GribDataPoint point = GribUtil.GetClosestGribPoint(this);
            if (point == null)
            {
                WindDirection = 0;
                WindSpeed = 0;
                SurfacePressure_hPa = AcftGeoUtil.STD_PRES_HPA;
                StaticAirTemperature = AcftGeoUtil.CalculateIsaTemp(AbsoluteAltitude);

                // Calculate True Track
                double wca = TrueAirSpeed == 0 ? 0 : Math.Acos(WindXComp / TrueAirSpeed);
                _trueTrack = AcftGeoUtil.NormalizeHeading(_trueHdg + wca);
            }
            else
            {
                if (WindDirection != point.WDir_deg || WindSpeed != point.WSpeed_kts)
                {
                    WindDirection = point.WDir_deg;
                    WindSpeed = point.WSpeed_kts;

                    // Calculate True Track
                    double wca = TrueAirSpeed == 0 ? 0 : Math.Acos(WindXComp / TrueAirSpeed);
                    _trueTrack = AcftGeoUtil.NormalizeHeading(_trueHdg + wca);
                }
                if (point.SfcPress_hPa != 0)
                {
                    SurfacePressure_hPa = point.SfcPress_hPa;
                }
                else
                {
                    SurfacePressure_hPa = AcftGeoUtil.STD_PRES_HPA;
                }
                StaticAirTemperature = point.Temp_C;
            }
        }
    }
}
