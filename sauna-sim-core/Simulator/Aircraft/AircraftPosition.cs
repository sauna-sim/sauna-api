using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.GeoTools.GribTools;
using AviationCalcUtilNet.GeoTools.MagneticTools;
using AviationCalcUtilNet.MathTools;
using System;
using System.Collections.Generic;
using SaunaSim.Core.Data;
using FsdConnectorNet;

namespace SaunaSim.Core.Simulator.Aircraft
{
    public class AircraftPosition
    {
        private GeoPoint _position;
        private double _altInd;
        private double _altPres;
        private double _altDens;
        private double _magneticHdg;
        private double _trueHdg;
        private double _trueTrack;
        private double _altSetting_hPa = AtmosUtil.ISA_STD_PRES_hPa;
        private double _sfcPress_hPa = AtmosUtil.ISA_STD_PRES_hPa;
        private double _ias;
        private double _tas;
        private double _gs;
        private double _mach;
        private GribDataPoint _gribPoint;
        private double _bank;
        private double _pitch;
        private double _bankRate;
        private double _pitchRate;
        private double _yawRate;
        private double _verticalSpeed;
        private double _windDirection;
        private double _windSpeed;
        private bool _onGround;

        public AircraftPosition(double lat, double lon, double indAlt)
        {
            _position = new GeoPoint(lat, lon);
            IndicatedAltitude = indAlt;
        }

        // Position
        public double Latitude
        {
            get => _position.Lat;
            set => _position.Lat = value;
        }

        public double Longitude
        {
            get => _position.Lon;
            set => _position.Lon = value;
        }

        public double IndicatedAltitude
        {
            get => _altInd;
            set
            {
                _altInd = value;
                _altPres = AtmosUtil.ConvertIndicatedToPressureAlt(_altInd, _altSetting_hPa);
                _position.Alt = AtmosUtil.ConvertIndicatedToAbsoluteAlt(_altInd, _altSetting_hPa, SurfacePressure_hPa);
                if (_gribPoint != null)
                {
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), _gribPoint.GeoPotentialHeight_M, _gribPoint.Temp_K);
                    double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), _gribPoint.GeoPotentialHeight_M, _gribPoint.Level_hPa * 100, T);
                    _altDens = MathUtil.ConvertMetersToFeet(AtmosUtil.CalculateDensityAltitude(p, T));
                } else
                {
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), 0, AtmosUtil.ISA_STD_TEMP_K);
                    double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), 0, AtmosUtil.ISA_STD_PRES_Pa, T);
                    _altDens = MathUtil.ConvertMetersToFeet(AtmosUtil.CalculateDensityAltitude(p, T));
                }
            }
        }

        public double TrueAltitude
        {
            get => _position.Alt;
            set
            {
                _position.Alt = value;
                _altInd = AtmosUtil.ConvertAbsoluteToIndicatedAlt(_position.Alt, _altSetting_hPa, _sfcPress_hPa);
                _altPres = AtmosUtil.ConvertIndicatedToPressureAlt(_altInd, _altSetting_hPa);
                if (_gribPoint != null)
                {
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), _gribPoint.GeoPotentialHeight_M, _gribPoint.Temp_K);
                    double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), _gribPoint.GeoPotentialHeight_M, _gribPoint.Level_hPa * 100, T);
                    _altDens = MathUtil.ConvertMetersToFeet(AtmosUtil.CalculateDensityAltitude(p, T));
                }
                else
                {
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), 0, AtmosUtil.ISA_STD_TEMP_K);
                    double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), 0, AtmosUtil.ISA_STD_PRES_Pa, T);
                    _altDens = MathUtil.ConvertMetersToFeet(AtmosUtil.CalculateDensityAltitude(p, T));
                }
            }
        }

        public double PressureAltitude => _altPres;
        public double DensityAltitude => _altDens;

        // Rotation
        public double Heading_Mag
        {
            get => _magneticHdg;
            set
            {
                _magneticHdg = value;

                // Calculate True Heading
                _trueHdg = MagneticUtil.ConvertMagneticToTrueTile(_magneticHdg, _position);

                // Calculate True Track
                double wca = _tas == 0 ? 0 : Math.Acos(WindXComp / _tas);
                _trueTrack = GeoUtil.NormalizeHeading(_trueHdg + wca);
            }
        }

        public double Heading_True
        {
            get => _trueHdg;

            set
            {
                _trueHdg = value;

                // Set Magnetic Heading
                _magneticHdg = MagneticUtil.ConvertTrueToMagneticTile(_trueHdg, _position);

                // Calculate True Track
                double wca = _tas == 0 ? 0 : Math.Acos(WindXComp / _tas);
                _trueTrack = GeoUtil.NormalizeHeading(_trueHdg + wca);
            }
        }

        public double Track_True
        {
            get => _trueTrack;

            set
            {
                _trueTrack = value;

                // Calculate True Heading
                double wca = _tas == 0 ? 0 : Math.Acos(WindXComp / _tas);
                _trueHdg = GeoUtil.NormalizeHeading(_trueTrack - wca);

                // Set Magnetic Heading
                _magneticHdg = MagneticUtil.ConvertTrueToMagneticTile(_trueHdg, _position);
            }
        }

        public double Bank
        {
            get => _bank;
            set => _bank = value;
        }

        public double Pitch
        {
            get => _pitch;
            set => _pitch = value;
        }

        // Linear Velocities
        public double IndicatedAirSpeed
        {
            get => _ias;
            set
            {
                _ias = value;

                if (_gribPoint != null)
                {
                    _tas = AtmosUtil.ConvertIasToTas(_ias, _gribPoint.Level_hPa, _position.Alt, _gribPoint.GeoPotentialHeight_Ft, _gribPoint.Temp_K, out _mach);
                }
                else
                {
                    _tas = AtmosUtil.ConvertIasToTas(_ias, AtmosUtil.ISA_STD_PRES_hPa, _position.Alt, 0, AtmosUtil.ISA_STD_TEMP_K, out _mach);
                }
                _gs = _tas == 0 ? 0 : (_tas - WindHComp);
            }
        }

        public double TrueAirSpeed => _tas;

        public double GroundSpeed
        {
            get => _gs;
            set
            {
                _gs = value;
                _tas = _gs + WindHComp;
                if (_gribPoint != null)
                {
                    _ias = AtmosUtil.ConvertTasToIas(_tas, _gribPoint.Level_hPa, _position.Alt, _gribPoint.GeoPotentialHeight_Ft, _gribPoint.Temp_K, out _mach);
                }
                else
                {
                    _ias = AtmosUtil.ConvertIasToTas(_tas, AtmosUtil.ISA_STD_PRES_hPa, _position.Alt, 0, AtmosUtil.ISA_STD_TEMP_K, out _mach);
                }
            }
        }

        public double MachNumber
        {
            get => _mach;
            set
            {
                _mach = value;
                if (_gribPoint != null)
                {
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), _gribPoint.GeoPotentialHeight_M, _gribPoint.Temp_K);
                    _tas = MathUtil.ConvertMpersToKts(AtmosUtil.ConvertMachToTas(_mach, T));
                    _ias = AtmosUtil.ConvertTasToIas(_tas, _gribPoint.Level_hPa, _position.Alt, _gribPoint.GeoPotentialHeight_Ft, _gribPoint.Temp_K, out _);
                }
                else
                {
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), 0, AtmosUtil.ISA_STD_TEMP_K);
                    _tas = MathUtil.ConvertMpersToKts(AtmosUtil.ConvertMachToTas(_mach, T));
                    _ias = AtmosUtil.ConvertTasToIas(_tas, AtmosUtil.ISA_STD_PRES_hPa, _position.Alt, 0, AtmosUtil.ISA_STD_TEMP_K, out _);
                }
                _gs = _tas == 0 ? 0 : (_tas - WindHComp);
            }
        }

        public double VerticalSpeed
        {
            get => _verticalSpeed;
            set => _verticalSpeed = value;
        }

        public double Velocity_X_MPerS => MathUtil.ConvertKtsToMpers(GroundSpeed) * Math.Sin(MathUtil.ConvertDegreesToRadians(Track_True));
        public double Velocity_Y_MPerS => MathUtil.ConvertFeetToMeters(VerticalSpeed) / 60;
         public double Velocity_Z_MPerS => MathUtil.ConvertKtsToMpers(GroundSpeed) * Math.Cos(MathUtil.ConvertDegreesToRadians(Track_True));

        // Rotational Velocities
        public double Heading_Velocity_RadPerS => MathUtil.ConvertDegreesToRadians(_yawRate);
        public double Bank_Velocity_RadPerS => MathUtil.ConvertDegreesToRadians(_bankRate);
        public double Pitch_Velocity_RadPerS => MathUtil.ConvertDegreesToRadians(_pitchRate);
        
        // Atmospheric Data        
        public double AltimeterSetting_hPa
        {
            get => _altSetting_hPa;
            set
            {
                _altSetting_hPa = value;

                // Backwards compute new Indicated Alt
                _altInd = AtmosUtil.ConvertAbsoluteToIndicatedAlt(_position.Alt, _altSetting_hPa, _sfcPress_hPa);
                _altPres = AtmosUtil.ConvertIndicatedToPressureAlt(_altInd, _altSetting_hPa);
            }
        }
        public double SurfacePressure_hPa
        {
            get => _sfcPress_hPa;
            set
            {
                _sfcPress_hPa = value;

                // Backwards compute new Indicated Alt
                _altInd = AtmosUtil.ConvertAbsoluteToIndicatedAlt(_position.Alt, _altSetting_hPa, _sfcPress_hPa);
                _altPres = AtmosUtil.ConvertIndicatedToPressureAlt(_altInd, _altSetting_hPa);
            }
        }

        public double WindDirection
        {
            get => _windDirection;
            private set => _windDirection = value;
        }

        public double WindSpeed
        {
            get => _windSpeed;
            private set => _windSpeed = value;
        }

        public double WindXComp => WindSpeed * Math.Sin(MathUtil.ConvertDegreesToRadians(Heading_True - WindDirection));

        public double WindHComp => GeoUtil.HeadwindComponent(WindSpeed, WindDirection, Heading_True);
        
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
                    SurfacePressure_hPa = AtmosUtil.ISA_STD_PRES_hPa;

                    // Calculate True Track
                    double wca = TrueAirSpeed == 0 ? 0 : Math.Acos(WindXComp / TrueAirSpeed);
                    _trueTrack = GeoUtil.NormalizeHeading(_trueHdg + wca);

                    // Calculate TAS
                    _tas = AtmosUtil.ConvertIasToTas(_ias, AtmosUtil.ISA_STD_PRES_hPa, _position.Alt, 0, AtmosUtil.ISA_STD_TEMP_K, out _mach);

                    // Density Alt
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), 0, AtmosUtil.ISA_STD_TEMP_K);
                    double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), 0, AtmosUtil.ISA_STD_PRES_Pa, T);
                    _altDens = MathUtil.ConvertMetersToFeet(AtmosUtil.CalculateDensityAltitude(p, T));
                }
                else if (_gribPoint != value)
                {
                    _gribPoint = value;

                    if (Math.Abs(WindDirection - _gribPoint.WDir_deg) > double.Epsilon || Math.Abs(WindSpeed - _gribPoint.WSpeed_kts) > double.Epsilon)
                    {
                        WindDirection = _gribPoint.WDir_deg;
                        WindSpeed = _gribPoint.WSpeed_kts;

                        // Calculate True Track
                        double wca = TrueAirSpeed == 0 ? 0 : Math.Acos(WindXComp / TrueAirSpeed);
                        _trueTrack = GeoUtil.NormalizeHeading(_trueHdg + wca);
                    }
                    SurfacePressure_hPa = _gribPoint.SfcPress_hPa != 0 ? _gribPoint.SfcPress_hPa : AtmosUtil.ISA_STD_PRES_hPa;

                    // Density Alt
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), _gribPoint.GeoPotentialHeight_M, _gribPoint.Temp_K);
                    double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(_position.Alt), _gribPoint.GeoPotentialHeight_M, _gribPoint.Level_hPa * 100, T);
                    _altDens = MathUtil.ConvertMetersToFeet(AtmosUtil.CalculateDensityAltitude(p, T));

                    // Calculate TAS
                    _tas = AtmosUtil.ConvertIasToTas(_ias, _gribPoint.Level_hPa, _position.Alt, _gribPoint.GeoPotentialHeight_Ft, _gribPoint.Temp_K, out _mach);
                }
                _gs = _tas == 0 ? 0 : (_tas - WindHComp);
            }
        }

        public GeoPoint Position
        {
            get => _position;
            set => _position = value;
        }

        public bool OnGround
        {
            get => _onGround;
            set => _onGround = value;
        }

        public double BankRate
        {
            get => _bankRate;
            set => _bankRate = value;
        }

        public double PitchRate
        {
            get => _pitchRate;
            set => _pitchRate = value;
        }

        public double YawRate
        {
            get => _yawRate;
            set => _yawRate = value;
        }

        public void UpdateGribPoint()
        {
            GribTile tile = GribTile.FindOrCreateGribTile(_position, DateTime.UtcNow);
            GribPoint = tile?.GetClosestPoint(_position);
        }
        
    }
}
