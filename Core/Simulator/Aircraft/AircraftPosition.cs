using AviationSimulation.GeoTools;
using AviationSimulation.GeoTools.GribTools;
using System;
using System.Collections.Generic;
using VatsimAtcTrainingSimulator.Core.Data;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft
{
    public class AircraftPosition
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double IndicatedAltitude { get; set; }
        public double StaticAirTemperature { get; private set; }
        public double WindDirection { get; private set; }
        public double WindSpeed { get; private set; }
        public double PressureAltitude => GeoUtil.CalculatePressureAlt(IndicatedAltitude, AltimeterSetting_hPa);
        public double DensityAltitude => GeoUtil.CalculateDensityAlt(PressureAltitude, StaticAirTemperature);
        public double AbsoluteAltitude => GeoUtil.CalculateAbsoluteAlt(IndicatedAltitude, AltimeterSetting_hPa, SurfacePressure_hPa);

        private double _magneticHdg;
        public double Heading_Mag
        {
            get => _magneticHdg;

            set
            {
                _magneticHdg = value;

                // Calculate True Heading
                _trueHdg = GeoUtil.MagneticToTrue(_magneticHdg, PositionGeoPoint);

                // Calculate True Track
                double wca = TrueAirSpeed == 0 ? 0 : Math.Acos(WindXComp / TrueAirSpeed);
                _trueTrack = GeoUtil.NormalizeHeading(_trueHdg + wca);
            }
        }
        public double Bank { get; set; }
        public double Pitch { get; set; }
        public double VerticalSpeed { get; set; }
        private double _altSetting_hPa = GeoUtil.STD_PRES_HPA;
        public double AltimeterSetting_hPa
        {
            get => _altSetting_hPa; set
            {
                _altSetting_hPa = value;

                // Backwards compute new Indicated Alt
                IndicatedAltitude = GeoUtil.CalculateIndicatedAlt(AbsoluteAltitude, value, SurfacePressure_hPa);
            }
        }
        public double SurfacePressure_hPa { get; set; } = GeoUtil.STD_PRES_HPA;
        public int PresAltDiff => (int)((GeoUtil.STD_PRES_HPA - (SurfacePressure_hPa == 0 ? GeoUtil.STD_PRES_HPA : SurfacePressure_hPa)) * GeoUtil.STD_PRES_DROP);

        private double _trueHdg;
        public double Heading_True
        {
            get => _trueHdg;

            set
            {
                _trueHdg = value;

                // Set Magnetic Heading
                _magneticHdg = GeoUtil.TrueToMagnetic(_trueHdg, PositionGeoPoint);

                // Calculate True Track
                double wca = Math.Acos(WindXComp / TrueAirSpeed);
                _trueTrack = GeoUtil.NormalizeHeading(_trueHdg + wca);
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
                _trueHdg = GeoUtil.NormalizeHeading(_trueTrack - wca);

                // Set Magnetic Heading
                _magneticHdg = GeoUtil.TrueToMagnetic(_trueHdg, PositionGeoPoint);
            }
        }

        public double IndicatedAirSpeed { get; set; }

        public double TrueAirSpeed => GeoUtil.CalculateTAS(IndicatedAirSpeed, AltimeterSetting_hPa, IndicatedAltitude, StaticAirTemperature);

        public double GroundSpeed => TrueAirSpeed - WindHComp;

        private GribDataPoint _gribPoint;
        public GribDataPoint GribPoint
        {
            get => _gribPoint;
            set
            {
                if (value == null)
                {
                    _gribPoint = value;

                    WindDirection = 0;
                    WindSpeed = 0;
                    SurfacePressure_hPa = GeoUtil.STD_PRES_HPA;
                    StaticAirTemperature = GeoUtil.CalculateIsaTemp(AbsoluteAltitude);

                    // Calculate True Track
                    double wca = TrueAirSpeed == 0 ? 0 : Math.Acos(WindXComp / TrueAirSpeed);
                    _trueTrack = GeoUtil.NormalizeHeading(_trueHdg + wca);
                } else if (_gribPoint != value)
                {
                    _gribPoint = value;

                    if (WindDirection != _gribPoint.WDir_deg || WindSpeed != _gribPoint.WSpeed_kts)
                    {
                        WindDirection = _gribPoint.WDir_deg;
                        WindSpeed = _gribPoint.WSpeed_kts;

                        // Calculate True Track
                        double wca = TrueAirSpeed == 0 ? 0 : Math.Acos(WindXComp / TrueAirSpeed);
                        _trueTrack = GeoUtil.NormalizeHeading(_trueHdg + wca);
                    }
                    if (_gribPoint.SfcPress_hPa != 0)
                    {
                        SurfacePressure_hPa = _gribPoint.SfcPress_hPa;
                    }
                    else
                    {
                        SurfacePressure_hPa = GeoUtil.STD_PRES_HPA;
                    }
                    StaticAirTemperature = _gribPoint.Temp_C;
                }
            }
        }

        public GeoPoint PositionGeoPoint => new GeoPoint(Latitude, Longitude, AbsoluteAltitude);

        public void UpdatePosition()
        {
            GribPoint = GribUtil.GetClosestGribPoint(PositionGeoPoint);
        }
    }
}
