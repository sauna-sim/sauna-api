using System;
using System.Collections.Generic;
using SaunaSim.Core.Data;
using FsdConnectorNet;
using SaunaSim.Core.Simulator.Aircraft.Performance;
using AviationCalcUtilNet.Atmos;
using AviationCalcUtilNet.Atmos.Grib;
using AviationCalcUtilNet.Units;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.Magnetic;
using AviationCalcUtilNet.Aviation;
using AviationCalcUtilNet.GeoTools;

namespace SaunaSim.Core.Simulator.Aircraft
{
    public class AircraftPosition
    {
        private SimAircraft _parentAircraft;
        private Latitude _lat;
        private Longitude _lon;
        private Length _altInd;
        private Length _altPres;
        private Length _altDens;
        private Length _altTrue;
        private Bearing _magneticHdg;
        private Bearing _trueHdg;
        private Bearing _trueTrack;
        private Pressure _altSetting;
        private Pressure _sfcPress;
        private Velocity _ias;
        private Acceleration _fwdAccel;
        private Velocity _tas;
        private Velocity _gs;
        private double _mach;
        private GribDataPoint _gribPoint;
        private Angle _bank;
        private Angle _pitch;
        private AngularVelocity _bankRate;
        private AngularVelocity _pitchRate;
        private AngularVelocity _yawRate;
        private Velocity _verticalSpeed;
        private Bearing _windDirection;
        private Velocity _windSpeed;
        private bool _onGround;
        private MagneticTileManager _magTileMgr;

        public AircraftPosition(Latitude lat, Longitude lon, Length indAlt, SimAircraft parentAircraft, MagneticTileManager magneticTileManager)
        {
            _parentAircraft = parentAircraft;

            _magneticHdg = (Bearing)0;
            _trueHdg = (Bearing)0;
            _trueTrack = (Bearing)0;
            _altSetting = AtmosUtil.ISA_STD_PRES;
            _sfcPress = AtmosUtil.ISA_STD_PRES;
            _ias = (Velocity)0;
            _fwdAccel = (Acceleration)0;
            _tas = (Velocity)0;
            _gs = (Velocity)0;
            _mach = 0;
            _gribPoint = null;
            _bank = (Angle)0;
            _pitch = (Angle)0;
            _bankRate = (AngularVelocity)0;
            _pitchRate = (AngularVelocity)0;
            _yawRate = (AngularVelocity)0;
            _verticalSpeed = (Velocity)0;
            _windDirection = (Bearing)0;
            _windSpeed = (Velocity)0;
            _onGround = false;

            _magTileMgr = magneticTileManager;

            _lat = lat;
            _lon = lon;
            IndicatedAltitude = indAlt;            
        }

        // Position
        public Latitude Latitude
        {
            get => _lat;
            set => _lat = value;
        }

        public Longitude Longitude
        {
            get => _lon;
            set => _lon = value;
        }

        public Length IndicatedAltitude
        {
            get => _altInd;
            set
            {
                _altInd = value;
                _altPres = AtmosUtil.ConvertIndicatedToPressureAlt(_altInd, _altSetting);
                _altTrue = AtmosUtil.ConvertIndicatedToAbsoluteAlt(_altInd, _altSetting, SurfacePressure);
                if (_gribPoint != null)
                {
                    Temperature T = AtmosUtil.CalculateTempAtAlt(_altTrue, _gribPoint.GeoPotentialHeight, _gribPoint.Temp);
                    Pressure p = AtmosUtil.CalculatePressureAtAlt(_altTrue, _gribPoint.GeoPotentialHeight, _gribPoint.LevelPressure, T);
                    _altDens = AtmosUtil.CalculateDensityAltitude(p, T);
                }
                else
                {
                    Temperature T = AtmosUtil.CalculateTempAtAlt(_altTrue, Length.FromMeters(0), AtmosUtil.ISA_STD_TEMP);
                    Pressure p = AtmosUtil.CalculatePressureAtAlt(_altTrue, Length.FromMeters(0), AtmosUtil.ISA_STD_PRES, T);
                    _altDens = AtmosUtil.CalculateDensityAltitude(p, T);
                }
            }
        }

        public Length TrueAltitude
        {
            get => _altTrue;
            set
            {
                _altTrue = value;
                _altInd = AtmosUtil.ConvertAbsoluteToIndicatedAlt(_altTrue, _altSetting, _sfcPress);
                _altPres = AtmosUtil.ConvertIndicatedToPressureAlt(_altInd, _altSetting);
                if (_gribPoint != null)
                {
                    var T = AtmosUtil.CalculateTempAtAlt(_altTrue, _gribPoint.GeoPotentialHeight, _gribPoint.Temp);
                    var p = AtmosUtil.CalculatePressureAtAlt(_altTrue, _gribPoint.GeoPotentialHeight, _gribPoint.LevelPressure, T);
                    _altDens = AtmosUtil.CalculateDensityAltitude(p, T);
                }
                else
                {
                    var T = AtmosUtil.CalculateTempAtAlt(_altTrue, (Length) 0, AtmosUtil.ISA_STD_TEMP);
                    var p = AtmosUtil.CalculatePressureAtAlt(_altTrue, (Length) 0, AtmosUtil.ISA_STD_PRES, T);
                    _altDens = AtmosUtil.CalculateDensityAltitude(p, T);
                }
            }
        }

        public Length PressureAltitude => _altPres;
        public Length DensityAltitude => _altDens;

        // Rotation
        public Bearing Heading_Mag
        {
            get => _magneticHdg;
            set
            {
                _magneticHdg = value;

                // Calculate True Heading
                _trueHdg = _magTileMgr.MagneticToTrue(PositionGeoPoint, DateTime.UtcNow, _magneticHdg);

                if (!_onGround)
                {
                    // Calculate True Track
                    Angle wca = (double)_tas == 0 ? (Angle)0 : (Angle)Math.Atan2((double)WindXComp, (double)_tas);
                    _trueTrack = _trueHdg + wca;
                }
            }
        }

        public Bearing Heading_True
        {
            get => _trueHdg;

            set
            {
                _trueHdg = value;

                // Set Magnetic Heading
                _magneticHdg = _magTileMgr.TrueToMagnetic(PositionGeoPoint, DateTime.UtcNow, _trueHdg);

                if (!_onGround)
                {
                    // Calculate True Track
                    var wca = (double)_tas == 0 ? (Angle)0 : (Angle)Math.Atan2((double)WindXComp, (double)_tas);
                    _trueTrack = _trueHdg + wca;
                }
            }
        }

        public Bearing Track_True
        {
            get => _trueTrack;

            set
            {
                _trueTrack = value;

                if (!_onGround)
                {
                    // Calculate True Heading
                    var wca = (double)_tas == 0 ? (Angle)0 : (Angle)Math.Atan2((double)WindXComp, (double)_tas);
                    _trueHdg = _trueTrack - wca;

                    // Set Magnetic Heading
                    _magneticHdg = _magTileMgr.TrueToMagnetic(PositionGeoPoint, DateTime.UtcNow, _trueHdg);
                }
            }
        }

        public Bearing Track_Mag => _magTileMgr.TrueToMagnetic(PositionGeoPoint, DateTime.UtcNow, _trueTrack);

        public Angle Bank
        {
            get => _bank;
            set => _bank = value;
        }

        public Angle Pitch
        {
            get => _pitch;
            set => _pitch = value;
        }

        // Linear Velocities
        public Velocity IndicatedAirSpeed
        {
            get => _ias;
            set
            {
                _ias = value;

                if (_gribPoint != null)
                {
                    (this._tas, this._mach) = AtmosUtil.ConvertIasToTas(_ias, _gribPoint.LevelPressure, _altTrue, _gribPoint.GeoPotentialHeight, _gribPoint.Temp);
                }
                else
                {
                    (this._tas, this._mach) = AtmosUtil.ConvertIasToTas(_ias, AtmosUtil.ISA_STD_PRES, _altTrue, (Length) 0, AtmosUtil.ISA_STD_TEMP);
                }

                _gs = (double) _tas == 0 ? (Velocity) 0 : (_tas - WindHComp);
            }
        }

        public Velocity TrueAirSpeed => _tas;

        public Velocity GroundSpeed
        {
            get => _gs;
            set
            {
                _gs = value;
                _tas = _gs + WindHComp;
                if (_gribPoint != null)
                {
                    (this._ias, this._mach) = AtmosUtil.ConvertTasToIas(_tas, _gribPoint.LevelPressure, _altTrue, _gribPoint.GeoPotentialHeight, _gribPoint.Temp);
                }
                else
                {
                    (this._ias, this._mach) = AtmosUtil.ConvertTasToIas(_tas, AtmosUtil.ISA_STD_PRES, _altTrue, (Length)0, AtmosUtil.ISA_STD_TEMP);
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
                    Temperature T = AtmosUtil.CalculateTempAtAlt(_altTrue, _gribPoint.GeoPotentialHeight, _gribPoint.Temp);
                    _tas = AtmosUtil.ConvertMachToTas(_mach, T);
                    (this._ias, _) = AtmosUtil.ConvertTasToIas(_tas, _gribPoint.LevelPressure, _altTrue, _gribPoint.GeoPotentialHeight, _gribPoint.Temp);
                }
                else
                {
                    var T = AtmosUtil.CalculateTempAtAlt(_altTrue, (Length) 0, AtmosUtil.ISA_STD_TEMP);
                    _tas = AtmosUtil.ConvertMachToTas(_mach, T);
                    (this._ias, _) = AtmosUtil.ConvertTasToIas(_tas, AtmosUtil.ISA_STD_PRES, _altTrue, (Length)0, AtmosUtil.ISA_STD_TEMP);
                }

                _gs = (double)_tas == 0 ? (Velocity)0 : (_tas - WindHComp);
            }
        }

        public Velocity VerticalSpeed
        {
            get => _verticalSpeed;
            set => _verticalSpeed = value;
        }

        public Angle FlightPathAngle => (Angle)Math.Atan2((double) _verticalSpeed, (double) _gs);

        public Velocity Velocity_X => _gs * Math.Sin((double) _trueTrack);
        public Velocity Velocity_Y => _verticalSpeed;
        public Velocity Velocity_Z => _gs * Math.Cos((double)_trueTrack);

        // Rotational Velocities
        public AngularVelocity Heading_Velocity => _yawRate;
        public AngularVelocity Bank_Velocity => _bankRate;
        public AngularVelocity Pitch_Velocity => _pitchRate;

        // Acceleration
        public Acceleration Forward_Acceleration
        {
            get => _fwdAccel;
            set => _fwdAccel = value;
        }

        // Atmospheric Data        
        public Pressure AltimeterSetting
        {
            get => _altSetting;
            set
            {
                _altSetting = value;

                // Backwards compute new Indicated Alt
                _altInd = AtmosUtil.ConvertAbsoluteToIndicatedAlt(_altTrue, _altSetting, _sfcPress);
                _altPres = AtmosUtil.ConvertIndicatedToPressureAlt(_altInd, _altSetting);
            }
        }

        public Pressure SurfacePressure
        {
            get => _sfcPress;
            set
            {
                _sfcPress = value;

                // Backwards compute new Indicated Alt
                _altInd = AtmosUtil.ConvertAbsoluteToIndicatedAlt(_altTrue, _altSetting, _sfcPress);
                _altPres = AtmosUtil.ConvertIndicatedToPressureAlt(_altInd, _altSetting);
            }
        }

        public Bearing WindDirection
        {
            get => _windDirection;
            private set => _windDirection = value;
        }

        public Velocity WindSpeed
        {
            get => _windSpeed;
            private set => _windSpeed = value;
        }

        public Velocity WindXComp => WindSpeed * Math.Sin((double) (Heading_True - WindDirection));

        public Velocity WindHComp => AviationUtil.GetHeadwindComponent(WindDirection, WindSpeed, Heading_True);

        public GribDataPoint GribPoint
        {
            get => _gribPoint;
            set
            {
                if (value == null)
                {
                    _gribPoint = value;

                    WindDirection = (Bearing)0;
                    WindSpeed = (Velocity)0;
                    SurfacePressure = AtmosUtil.ISA_STD_PRES;

                    // Calculate True Track
                    Angle wca = (double)_tas == 0 ? (Angle)0 : (Angle)Math.Atan2((double)WindXComp, (double)_tas);
                    _trueTrack = _trueHdg + wca;

                    // Calculate TAS
                    (this._tas, this._mach) = AtmosUtil.ConvertIasToTas(_ias, AtmosUtil.ISA_STD_PRES, _altTrue, (Length) 0, AtmosUtil.ISA_STD_TEMP);

                    // Density Alt
                    Temperature T = AtmosUtil.CalculateTempAtAlt(_altTrue, (Length)0, AtmosUtil.ISA_STD_TEMP);
                    Pressure p = AtmosUtil.CalculatePressureAtAlt(_altTrue, (Length)0, AtmosUtil.ISA_STD_PRES, T);
                    _altDens = AtmosUtil.CalculateDensityAltitude(p, T);
                }
                else if (_gribPoint != value)
                {
                    _gribPoint = value;
                    var wind = _gribPoint.Wind;

                    if (Math.Abs((double) (WindDirection - wind.windDir)) > double.Epsilon || Math.Abs((double) (WindSpeed - wind.windSpd)) > double.Epsilon)
                    {
                        WindDirection = wind.windDir;
                        WindSpeed = wind.windSpd;

                        // Calculate True Track
                        Angle wca = (double)_tas == 0 ? (Angle)0 : (Angle)Math.Atan2((double)WindXComp, (double)_tas);
                        _trueTrack = _trueHdg + wca;
                    }

                    SurfacePressure = (double)_gribPoint.SfcPress != 0 ? _gribPoint.SfcPress : AtmosUtil.ISA_STD_PRES;

                    if (_onGround)
                    {
                        AltimeterSetting = SurfacePressure;
                    }

                    // Density Alt
                    Temperature T = AtmosUtil.CalculateTempAtAlt(_altTrue, _gribPoint.GeoPotentialHeight, _gribPoint.Temp);
                    Pressure p = AtmosUtil.CalculatePressureAtAlt(_altTrue, _gribPoint.GeoPotentialHeight, _gribPoint.LevelPressure, T);
                    _altDens = AtmosUtil.CalculateDensityAltitude(p, T);

                    // Calculate TAS
                    (this._tas, this._mach) = AtmosUtil.ConvertIasToTas(_ias, _gribPoint.LevelPressure, _altTrue, _gribPoint.GeoPotentialHeight, _gribPoint.Temp);
                }

                _gs = (double)_tas == 0 ? (Velocity)0 : (_tas - WindHComp);
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

        public AngularVelocity BankRate
        {
            get => _bankRate;
            set => _bankRate = value;
        }

        public AngularVelocity PitchRate
        {
            get => _pitchRate;
            set => _pitchRate = value;
        }

        public AngularVelocity YawRate
        {
            get => _yawRate;
            set => _yawRate = value;
        }
    }
}