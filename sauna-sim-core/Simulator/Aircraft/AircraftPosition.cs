using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.GeoTools.GribTools;
using AviationCalcUtilNet.GeoTools.MagneticTools;
using AviationCalcUtilNet.MathTools;
using System;
using System.Collections.Generic;
using SaunaSim.Core.Data;
using FsdConnectorNet;
using SaunaSim.Core.Simulator.Aircraft.Performance;

namespace SaunaSim.Core.Simulator.Aircraft
{
    public class AircraftPosition
    {
        private SimAircraft _parentAircraft;
        private double _lat;
        private double _lon;
        private double _altInd;
        private double _altPres;
        private double _altDens;
        private double _altTrue;
        private double _magneticHdg;
        private double _trueHdg;
        private double _trueTrack;
        private double _altSetting_hPa = AtmosUtil.ISA_STD_PRES_hPa;
        private double _sfcPress_hPa = AtmosUtil.ISA_STD_PRES_hPa;
        private double _ias;
        private double _fwdAccel;
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

        public AircraftPosition(SimAircraft parentAircraft, double lat, double lon, double indAlt)
        {
            _parentAircraft = parentAircraft;
            _lat = lat;
            _lon = lon;
            IndicatedAltitude = indAlt;
        }

        // Position
        public double Latitude
        {
            get => _lat;
            set => _lat = value;
        }

        public double Longitude
        {
            get => _lon;
            set => _lon = value;
        }

        public double IndicatedAltitude
        {
            get => _altInd;
            set
            {
                _altInd = value;
                _altPres = AtmosUtil.ConvertIndicatedToPressureAlt(_altInd, _altSetting_hPa);
                _altTrue = AtmosUtil.ConvertIndicatedToAbsoluteAlt(_altInd, _altSetting_hPa, SurfacePressure_hPa);
                if (_gribPoint != null)
                {
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), _gribPoint.GeoPotentialHeight_M, _gribPoint.Temp_K);
                    double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), _gribPoint.GeoPotentialHeight_M, _gribPoint.Level_hPa * 100, T);
                    _altDens = MathUtil.ConvertMetersToFeet(AtmosUtil.CalculateDensityAltitude(p, T));
                }
                else
                {
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), 0, AtmosUtil.ISA_STD_TEMP_K);
                    double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), 0, AtmosUtil.ISA_STD_PRES_Pa, T);
                    _altDens = MathUtil.ConvertMetersToFeet(AtmosUtil.CalculateDensityAltitude(p, T));
                }
            }
        }

        public double TrueAltitude
        {
            get => _altTrue;
            set
            {
                _altTrue = value;
                _altInd = AtmosUtil.ConvertAbsoluteToIndicatedAlt(_altTrue, _altSetting_hPa, _sfcPress_hPa);
                _altPres = AtmosUtil.ConvertIndicatedToPressureAlt(_altInd, _altSetting_hPa);
                if (_gribPoint != null)
                {
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), _gribPoint.GeoPotentialHeight_M, _gribPoint.Temp_K);
                    double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), _gribPoint.GeoPotentialHeight_M, _gribPoint.Level_hPa * 100, T);
                    _altDens = MathUtil.ConvertMetersToFeet(AtmosUtil.CalculateDensityAltitude(p, T));
                }
                else
                {
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), 0, AtmosUtil.ISA_STD_TEMP_K);
                    double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), 0, AtmosUtil.ISA_STD_PRES_Pa, T);
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
                _trueHdg = MagneticUtil.ConvertMagneticToTrueTile(_magneticHdg, PositionGeoPoint);

                if(!_onGround)
                {
                    // Calculate True Track
                    double wca = _tas == 0 ? 0 : Math.Acos(WindXComp / _tas);
                    _trueTrack = GeoUtil.NormalizeHeading(_trueHdg + wca);
                }
            }
        }

        public double Heading_True
        {
            get => _trueHdg;

            set
            {
                _trueHdg = value;

                // Set Magnetic Heading
                _magneticHdg = MagneticUtil.ConvertTrueToMagneticTile(_trueHdg, PositionGeoPoint);

                if(!_onGround)
                {
                    // Calculate True Track
                    double wca = _tas == 0 ? 0 : Math.Acos(WindXComp / _tas);
                    _trueTrack = GeoUtil.NormalizeHeading(_trueHdg + wca);
                }
            }
        }

        public double Track_True
        {
            get => _trueTrack;

            set
            {
                _trueTrack = value;

                if(!_onGround)
                {
                    // Calculate True Heading
                    double wca = _tas == 0 ? 0 : Math.Acos(WindXComp / _tas);
                    _trueHdg = GeoUtil.NormalizeHeading(_trueTrack - wca);
                }

                // Set Magnetic Heading
                _magneticHdg = MagneticUtil.ConvertTrueToMagneticTile(_trueHdg, PositionGeoPoint);
            }
        }

        public double Track_Mag => MagneticUtil.ConvertTrueToMagneticTile(_trueTrack, PositionGeoPoint);

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
                    _tas = AtmosUtil.ConvertIasToTas(_ias, _gribPoint.Level_hPa, _altTrue, _gribPoint.GeoPotentialHeight_Ft, _gribPoint.Temp_K, out _mach);
                }
                else
                {
                    _tas = AtmosUtil.ConvertIasToTas(_ias, AtmosUtil.ISA_STD_PRES_hPa, _altTrue, 0, AtmosUtil.ISA_STD_TEMP_K, out _mach);
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
                    _ias = AtmosUtil.ConvertTasToIas(_tas, _gribPoint.Level_hPa, _altTrue, _gribPoint.GeoPotentialHeight_Ft, _gribPoint.Temp_K, out _mach);
                }
                else
                {
                    _ias = AtmosUtil.ConvertIasToTas(_tas, AtmosUtil.ISA_STD_PRES_hPa, _altTrue, 0, AtmosUtil.ISA_STD_TEMP_K, out _mach);
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
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), _gribPoint.GeoPotentialHeight_M, _gribPoint.Temp_K);
                    _tas = MathUtil.ConvertMpersToKts(AtmosUtil.ConvertMachToTas(_mach, T));
                    _ias = AtmosUtil.ConvertTasToIas(_tas, _gribPoint.Level_hPa, _altTrue, _gribPoint.GeoPotentialHeight_Ft, _gribPoint.Temp_K, out _);
                }
                else
                {
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), 0, AtmosUtil.ISA_STD_TEMP_K);
                    _tas = MathUtil.ConvertMpersToKts(AtmosUtil.ConvertMachToTas(_mach, T));
                    _ias = AtmosUtil.ConvertTasToIas(_tas, AtmosUtil.ISA_STD_PRES_hPa, _altTrue, 0, AtmosUtil.ISA_STD_TEMP_K, out _);
                }

                _gs = _tas == 0 ? 0 : (_tas - WindHComp);
            }
        }

        public double VerticalSpeed
        {
            get => _verticalSpeed;
            set => _verticalSpeed = value;
        }

        public double FlightPathAngle => PerfDataHandler.ConvertVsToFpa(_verticalSpeed, _gs);

        public double Velocity_X_MPerS => MathUtil.ConvertKtsToMpers(GroundSpeed) * Math.Sin(MathUtil.ConvertDegreesToRadians(Track_True));
        public double Velocity_Y_MPerS => MathUtil.ConvertFeetToMeters(VerticalSpeed) / 60;
        public double Velocity_Z_MPerS => MathUtil.ConvertKtsToMpers(GroundSpeed) * Math.Cos(MathUtil.ConvertDegreesToRadians(Track_True));

        // Rotational Velocities
        public double Heading_Velocity_RadPerS => MathUtil.ConvertDegreesToRadians(_yawRate);
        public double Bank_Velocity_RadPerS => MathUtil.ConvertDegreesToRadians(_bankRate);
        public double Pitch_Velocity_RadPerS => MathUtil.ConvertDegreesToRadians(_pitchRate);

        // Acceleration
        public double Forward_Acceleration
        {
            get => _fwdAccel;
            set => _fwdAccel = value;
        }

        // Atmospheric Data        
        public double AltimeterSetting_hPa
        {
            get => _altSetting_hPa;
            set
            {
                _altSetting_hPa = value;

                // Backwards compute new Indicated Alt
                _altInd = AtmosUtil.ConvertAbsoluteToIndicatedAlt(_altTrue, _altSetting_hPa, _sfcPress_hPa);
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
                _altInd = AtmosUtil.ConvertAbsoluteToIndicatedAlt(_altTrue, _altSetting_hPa, _sfcPress_hPa);
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
                    _tas = AtmosUtil.ConvertIasToTas(_ias, AtmosUtil.ISA_STD_PRES_hPa, _altTrue, 0, AtmosUtil.ISA_STD_TEMP_K, out _mach);

                    // Density Alt
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), 0, AtmosUtil.ISA_STD_TEMP_K);
                    double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), 0, AtmosUtil.ISA_STD_PRES_Pa, T);
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
                    double T = AtmosUtil.CalculateTempAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), _gribPoint.GeoPotentialHeight_M, _gribPoint.Temp_K);
                    double p = AtmosUtil.CalculatePressureAtAlt(MathUtil.ConvertFeetToMeters(_altTrue), _gribPoint.GeoPotentialHeight_M, _gribPoint.Level_hPa * 100, T);
                    _altDens = MathUtil.ConvertMetersToFeet(AtmosUtil.CalculateDensityAltitude(p, T));

                    // Calculate TAS
                    _tas = AtmosUtil.ConvertIasToTas(_ias, _gribPoint.Level_hPa, _altTrue, _gribPoint.GeoPotentialHeight_Ft, _gribPoint.Temp_K, out _mach);
                }

                _gs = _tas == 0 ? 0 : (_tas - WindHComp);
            }
        }

        public GeoPoint PositionGeoPoint => new GeoPoint(_lat, _lon, _altTrue);

        public bool OnGround
        {
            get => _onGround;
            set
            {
                var oldValue = _onGround;
                _onGround = value;

                // Update FSD
                if (oldValue != _onGround)
                {
                    _parentAircraft.Connection.SetOnGround(_onGround);
                }
            }
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
            GribTile tile = GribTile.FindOrCreateGribTile(PositionGeoPoint, DateTime.UtcNow);
            if (tile == null)
            {
                GribPoint = null;
            }
            else
            {
                GribPoint = tile.GetClosestPoint(PositionGeoPoint);
            }
        }
    }
}