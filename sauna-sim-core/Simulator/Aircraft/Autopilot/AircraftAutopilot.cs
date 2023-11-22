using System;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.GeoTools.GribTools;
using AviationCalcUtilNet.MathTools;
using System.Collections.Generic;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;
using SaunaSim.Core.Simulator.Aircraft.Performance;
using System.Net;

namespace SaunaSim.Core.Simulator.Aircraft.Autopilot
{
    public class AircraftAutopilot
    {
        private readonly SimAircraft _parentAircraft;

        // Autopilot internal variables
        private double _targetThrust;
        private double _targetBank;
        private double _targetPitch;

        // Modes
        private LateralModeType _curLatMode;
        private readonly List<LateralModeType> _armedLatModes;
        private readonly object _armedLatModesLock = new object();
        private VerticalModeType _curVertMode;
        private readonly List<VerticalModeType> _armedVertModes;
        private readonly object _armedVertModesLock = new object();
        private ThrustModeType _curThrustMode;
        private readonly List<ThrustModeType> _armedThrustModes;
        private readonly object _armedThrustModesLock = new object();

        // MCP
        private McpKnobDirection _hdgKnobDir;
        private int _selHdg;
        private int _selAlt;
        private int _selVs;
        private double _selFpa;
        private McpSpeedSelectorType _spdMode;
        private McpSpeedUnitsType _spdUnits;
        private int _selSpd;

        public AircraftAutopilot(SimAircraft parentAircraft)
        {
            _parentAircraft = parentAircraft;

            // Initialize Modes
            _curLatMode = LateralModeType.BANK;
            _curThrustMode = ThrustModeType.SPEED;
            _curVertMode = VerticalModeType.FPA;
            _armedLatModes = new List<LateralModeType>();
            _armedVertModes = new List<VerticalModeType>();
            _armedThrustModes = new List<ThrustModeType>();

            // Initialize MCP
            _selHdg = 0;
            _hdgKnobDir = McpKnobDirection.SHORTEST;
            _selAlt = 0;
            _selVs = 0;
            _selFpa = 0;
            _spdMode = McpSpeedSelectorType.FMS;
            _spdUnits = McpSpeedUnitsType.KNOTS;
            _selSpd = 250;
        }

        public void OnPositionUpdate(int intervalMs)
        {
            // Run Bank Controller
            RunRollController(intervalMs);

            // Run Pitch Controller
            RunPitchController(intervalMs);

            // Run Thrust Controller
            RunThrustController(intervalMs);
        }

        private void RunThrustController(int intervalMs)
        {
            if (_curThrustMode == ThrustModeType.SPEED)
            {
                // Convert selected mach # to IAS if required
                double selSpdKts = _selSpd;
                if (_spdUnits == McpSpeedUnitsType.MACH)
                {
                    selSpdKts = ConvertMachToKts(_selSpd);
                }

                // Calculate required thrust
                double speedDelta = _parentAircraft.Position.IndicatedAirSpeed - selSpdKts;
                double zeroAccelThrust = PerfDataHandler.GetRequiredThrustForVs(_parentAircraft.PerformanceData, _parentAircraft.Position.VerticalSpeed, 0,
                    selSpdKts, _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg,
                    _parentAircraft.Data.SpeedBrakePos, _parentAircraft.Data.Config);
                _targetThrust = AutopilotUtil.CalculateDemandedThrottleForSpeed(
                    speedDelta,
                    _parentAircraft.Data.ThrustLeverPos,
                    zeroAccelThrust * 100.0,
                    (thrust) =>
                        PerfDataHandler.CalculatePerformance(_parentAircraft.PerformanceData, _parentAircraft.Position.Pitch, thrust / 100.0,
                            _parentAircraft.Position.IndicatedAirSpeed, _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos,
                            _parentAircraft.Data.Config).accelFwd,
                    intervalMs
                );
                _parentAircraft.Data.ThrustLeverVel = AutopilotUtil.CalculateThrustRate(_targetThrust, _parentAircraft.Data.ThrustLeverPos, intervalMs);
            }
            else if (_curThrustMode == ThrustModeType.THRUST)
            {
                _parentAircraft.Data.ThrustLeverVel = AutopilotUtil.CalculateThrustRate(_targetThrust, _parentAircraft.Data.ThrustLeverPos, intervalMs);
            }
        }

        private void RunPitchController(int intervalMs)
        {
            if (_curVertMode == VerticalModeType.ALT)
            {
                // Check if we're off altitude by more than 200ft
                double altDelta = _parentAircraft.Position.IndicatedAltitude - _selAlt;
                if (Math.Abs(altDelta) > 200)
                {
                    _curVertMode = VerticalModeType.FLCH;
                    PitchHandleFlch(intervalMs);
                }
                else
                {
                    PitchHandleAsel(intervalMs);
                }
            }
            else if (_curVertMode == VerticalModeType.FLCH)
            {
                if (PitchShouldAsel(intervalMs))
                {
                    _curVertMode = VerticalModeType.ASEL;
                    PitchHandleAsel(intervalMs);
                }
                else
                {
                    PitchHandleFlch(intervalMs);
                }
            }
            else if (_curVertMode == VerticalModeType.ASEL)
            {
                // Check if we're at altitude
                double altDelta = _parentAircraft.Position.IndicatedAltitude - _selAlt;
                if (Math.Abs(_parentAircraft.Position.VerticalSpeed) < 50 && Math.Abs(altDelta) < 1)
                {
                    _curVertMode = VerticalModeType.ALT;
                }

                PitchHandleAsel(intervalMs);
            }
            else if (_curVertMode == VerticalModeType.VS)
            {
                if (PitchShouldAsel(intervalMs))
                {
                    _curVertMode = VerticalModeType.ASEL;
                    PitchHandleAsel(intervalMs);
                }
                else
                {
                    // Set thrust mode to speed
                    _curThrustMode = ThrustModeType.SPEED;

                    // Calculate pitch and pitch rate
                    _targetPitch = PerfDataHandler.GetRequiredPitchForVs(_parentAircraft.PerformanceData, _selVs,
                        _parentAircraft.Position.IndicatedAirSpeed, _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg,
                        _parentAircraft.Data.SpeedBrakePos, _parentAircraft.Data.Config);
                    _parentAircraft.Position.PitchRate = AutopilotUtil.CalculatePitchRate(_targetPitch, _parentAircraft.Position.Pitch, intervalMs);
                }
            } else if (_curVertMode == VerticalModeType.FPA)
            {
                if (PitchShouldAsel(intervalMs))
                {
                    _curVertMode = VerticalModeType.ASEL;
                    PitchHandleAsel(intervalMs);
                } else
                {
                    // Set thrust mode to speed
                    _curThrustMode = ThrustModeType.SPEED;

                    // Calculate pitch and pitch rate
                    _targetPitch = PerfDataHandler.GetRequiredPitchForVs(_parentAircraft.PerformanceData, PerfDataHandler.ConvertFpaToVs(_selFpa, _parentAircraft.Position.GroundSpeed),
                        _parentAircraft.Position.IndicatedAirSpeed, _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg,
                        _parentAircraft.Data.SpeedBrakePos, _parentAircraft.Data.Config);
                    _parentAircraft.Position.PitchRate = AutopilotUtil.CalculatePitchRate(_targetPitch, _parentAircraft.Position.Pitch, intervalMs);
                }
            } else if (_curVertMode == VerticalModeType.APCH)
            {
                _target
            }
        }

        private void RunRollController(int intervalMs)
        {
            // Check armed lateral modes
            lock (_armedLatModesLock)
            {
                foreach (var mode in _armedLatModes)
                {
                    if (mode == LateralModeType.LNAV && _parentAircraft.Fms.ShouldActivateLnav(intervalMs))
                    {
                        RemoveArmedLateralMode(mode);
                        _curLatMode = LateralModeType.LNAV;
                        break;
                    }
                }
            }

            // Handle Active Mode
            if (_curLatMode == LateralModeType.BANK)
            {
                _parentAircraft.Position.BankRate = AutopilotUtil.CalculateRollRate(_targetBank, _parentAircraft.Position.Bank, intervalMs);
            }
            else if (_curLatMode == LateralModeType.HDG)
            {
                RollHdgTrackHold(_parentAircraft.Position.Heading_Mag, _selHdg, intervalMs);
            }
            else if (_curLatMode == LateralModeType.TRACK)
            {
                RollHdgTrackHold(_parentAircraft.Position.Track_Mag, _selHdg, intervalMs);
            }
            else if (_curLatMode == LateralModeType.LNAV)
            {
                RollHandleLnav(intervalMs);
            }
        }

        private void RollHandleLnav(int intervalMs)
        {
            var fms = _parentAircraft.Fms;

            if (fms.ActiveLeg == null)
            {
                _selHdg = (int) _parentAircraft.Position.Track_Mag;
                _curLatMode = LateralModeType.TRACK;
                return;
            }

            // Get True Course and crossTrackError
            (double requiredTrueCourse, double crossTrackError, _, double turnRadius) = fms.CourseInterceptInfo;
            
            // Calculate Bank Angle & Rate
            _targetBank = AutopilotUtil.CalculateDemandedRollForNav(crossTrackError, _parentAircraft.Position.Track_True, requiredTrueCourse,
                turnRadius, _parentAircraft.Position.Bank, _parentAircraft.Position.GroundSpeed, intervalMs).demandedRoll;
            
            _parentAircraft.Position.BankRate = AutopilotUtil.CalculateRollRate(_targetBank, _parentAircraft.Position.Bank, intervalMs);
        }

        private (double requiredFpa, double pitchTrackError) GetPitchInterceptInfoForCurrentLeg()
        {
            double requiredFpa = _parentAircraft.Fms.ActiveLeg.EndPoint.AngleConstraint;

            if (requiredFpa == -1)
            {
                return (-1, 0);
            }
            else
            {
                // Assume straight leg. Calculate pitchTrackError from the altitude that
                // VNAV wants to be at for the EndPoint, extrapolate for current position
                // with trigonometry and compare to our current altitude.
                (_, _, double alongTrackDistance, _) = _parentAircraft.Fms.CourseInterceptInfo;

                // Calculate how much altitude we still need to climb/descend from here to the EndPoint
                double altitudeRemaining = Math.Tan(requiredFpa) * alongTrackDistance;

                IRouteLeg currentLeg = _parentAircraft.Fms.ActiveLeg;

                // This is the altitude we should be at right now
                double expectedAltitude = currentLeg.EndPoint.Point.PointPosition.Alt + altitudeRemaining;

                // The difference between our indicated altitude and expected is the pitchTrackError
                double pitchTrackError = expectedAltitude - _parentAircraft.Position.IndicatedAltitude;

                return (requiredFpa, pitchTrackError);
            }
        }

        private void RollHdgTrackHold(double currentBearing, double targetBearing, int intervalMs)
        {
            double hdgDelta = GeoUtil.CalculateTurnAmount(currentBearing, targetBearing);

            // Go the shortest way if we're almost at the heading
            if (Math.Abs(hdgDelta) < 1)
            {
                _hdgKnobDir = McpKnobDirection.SHORTEST;
            }

            if (Math.Abs(hdgDelta) > double.Epsilon || Math.Abs(_parentAircraft.Position.Bank) > double.Epsilon)
            {
                // Figure out turn direction
                bool isRightTurn = (_hdgKnobDir == McpKnobDirection.SHORTEST) ? (hdgDelta > 0) : (_hdgKnobDir == McpKnobDirection.RIGHT);

                // Get new hdgDelta
                if (isRightTurn && hdgDelta < 0)
                {
                    hdgDelta += 360;
                }
                else if (!isRightTurn && hdgDelta > 0)
                {
                    hdgDelta -= 360;
                }

                // Desired bank angle
                _targetBank = AutopilotUtil.CalculateDemandedRollForTurn(hdgDelta, _parentAircraft.Position.Bank, _parentAircraft.Position.GroundSpeed, intervalMs);
                _parentAircraft.Position.BankRate = AutopilotUtil.CalculateRollRate(_targetBank, _parentAircraft.Position.Bank, intervalMs);
            }
            else
            {
                _parentAircraft.Position.BankRate = 0;
            }
        }

        private bool PitchShouldAsel(int intervalMs)
        {
            double altDelta = _parentAircraft.Position.IndicatedAltitude - _selAlt;

            // Calculate required ASEL pitch
            double zeroVsPitch = PerfDataHandler.GetRequiredPitchForVs(_parentAircraft.PerformanceData, 0,
                _parentAircraft.Position.IndicatedAirSpeed, _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg,
                _parentAircraft.Data.SpeedBrakePos, _parentAircraft.Data.Config);
            double idlePitch = PerfDataHandler.GetRequiredPitchForThrust(_parentAircraft.PerformanceData, 0, 0, _parentAircraft.Position.IndicatedAirSpeed,
                _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos, _parentAircraft.Data.Config);
            double maxPitch = PerfDataHandler.GetRequiredPitchForThrust(_parentAircraft.PerformanceData, 1, 0, _parentAircraft.Position.IndicatedAirSpeed,
                _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos, _parentAircraft.Data.Config);
            double pitchTarget = AutopilotUtil.CalculateDemandedPitchForAltitude(
                altDelta,
                _parentAircraft.Position.Pitch,
                zeroVsPitch,
                idlePitch,
                maxPitch,
                (pitch) =>
                    PerfDataHandler.CalculatePerformance(_parentAircraft.PerformanceData, pitch, _parentAircraft.Data.ThrustLeverPos / 100.0,
                        _parentAircraft.Position.IndicatedAirSpeed, _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos,
                        _parentAircraft.Data.Config).vs / 60,
                intervalMs
            );

            return Math.Abs(zeroVsPitch - pitchTarget) < double.Epsilon;
        }

        private void PitchHandleAsel(int intervalMs)
        {
            double altDelta = _parentAircraft.Position.IndicatedAltitude - _selAlt;
            _curThrustMode = ThrustModeType.SPEED;

            // Calculate required ASEL pitch
            double zeroVsPitch = PerfDataHandler.GetRequiredPitchForVs(_parentAircraft.PerformanceData, 0,
                _parentAircraft.Position.IndicatedAirSpeed, _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg,
                _parentAircraft.Data.SpeedBrakePos, _parentAircraft.Data.Config);
            double idlePitch = PerfDataHandler.GetRequiredPitchForThrust(_parentAircraft.PerformanceData, 0, 0, _parentAircraft.Position.IndicatedAirSpeed,
                _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos, _parentAircraft.Data.Config);
            double maxPitch = PerfDataHandler.GetRequiredPitchForThrust(_parentAircraft.PerformanceData, 1, 0, _parentAircraft.Position.IndicatedAirSpeed,
                _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos, _parentAircraft.Data.Config);
            _targetPitch = AutopilotUtil.CalculateDemandedPitchForAltitude(
                altDelta,
                _parentAircraft.Position.Pitch,
                zeroVsPitch,
                idlePitch,
                maxPitch,
                (pitch) =>
                    PerfDataHandler.CalculatePerformance(_parentAircraft.PerformanceData, pitch, _parentAircraft.Data.ThrustLeverPos / 100.0,
                        _parentAircraft.Position.IndicatedAirSpeed, _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos,
                        _parentAircraft.Data.Config).vs / 60,
                intervalMs
            );

            _parentAircraft.Position.PitchRate = AutopilotUtil.CalculatePitchRate(_targetPitch, _parentAircraft.Position.Pitch, intervalMs);
        }

        private void PitchHandleFlch(int intervalMs)
        {
            // Figure out thrust setting
            double altDelta = _parentAircraft.Position.IndicatedAltitude - _selAlt;
            _curThrustMode = ThrustModeType.THRUST;

            if (altDelta > 0 && _targetThrust > double.Epsilon)
            {
                _targetThrust = 0;
            }
            else if (altDelta < 0 && _targetThrust < 100)
            {
                _targetThrust = 100;
            }

            // Convert selected mach # to IAS if required
            double selSpdKts = _selSpd;
            if (_spdUnits == McpSpeedUnitsType.MACH)
            {
                selSpdKts = ConvertMachToKts(_selSpd);
            }

            // Calculate pitch limits
            double maxPitch = AutopilotUtil.PITCH_LIMIT_MAX;
            double minPitch = AutopilotUtil.PITCH_LIMIT_MIN;
            double zeroVsPitch = PerfDataHandler.GetRequiredPitchForVs(_parentAircraft.PerformanceData, 0,
                _parentAircraft.Position.IndicatedAirSpeed, _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg,
                _parentAircraft.Data.SpeedBrakePos, _parentAircraft.Data.Config);
            if (altDelta > 0)
            {
                maxPitch = zeroVsPitch;
            }
            else
            {
                minPitch = zeroVsPitch;
            }

            // Calculate required pitch
            double speedDelta = _parentAircraft.Position.IndicatedAirSpeed - selSpdKts;
            double zeroAccelPitch = PerfDataHandler.GetRequiredPitchForThrust(_parentAircraft.PerformanceData, _parentAircraft.Data.ThrustLeverPos / 100.0, 0,
                selSpdKts, _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg,
                _parentAircraft.Data.SpeedBrakePos, _parentAircraft.Data.Config);
            _targetPitch = AutopilotUtil.CalculateDemandedPitchForSpeed(
                speedDelta,
                _parentAircraft.Position.Pitch,
                zeroAccelPitch,
                maxPitch,
                minPitch,
                (pitch) =>
                    PerfDataHandler.CalculatePerformance(_parentAircraft.PerformanceData, pitch, _parentAircraft.Data.ThrustLeverPos / 100.0,
                        _parentAircraft.Position.IndicatedAirSpeed, _parentAircraft.Position.DensityAltitude, _parentAircraft.Data.Mass_kg, _parentAircraft.Data.SpeedBrakePos,
                        _parentAircraft.Data.Config).accelFwd,
                intervalMs
            );
            _parentAircraft.Position.PitchRate = AutopilotUtil.CalculatePitchRate(_targetPitch, _parentAircraft.Position.Pitch, intervalMs);
        }

        private double ConvertMachToKts(double speedMach)
        {
            GribDataPoint gribPoint = _parentAircraft.Position.GribPoint;
            double trueAlt_ft = _parentAircraft.Position.TrueAltitude;
            double trueAlt_m = MathUtil.ConvertFeetToMeters(trueAlt_ft);
            double refPres_hPa = gribPoint?.Level_hPa ?? AtmosUtil.ISA_STD_PRES_hPa;
            double refAlt_ft = gribPoint?.GeoPotentialHeight_Ft ?? 0;
            double refAlt_m = gribPoint?.GeoPotentialHeight_M ?? 0;
            double refTemp_K = gribPoint?.Temp_K ?? AtmosUtil.ISA_STD_TEMP_K;
            double T = AtmosUtil.CalculateTempAtAlt(trueAlt_m, refAlt_m, refTemp_K);
            double selTas = MathUtil.ConvertMpersToKts(AtmosUtil.ConvertMachToTas(speedMach / 100.0, T));
            return AtmosUtil.ConvertTasToIas(selTas, refPres_hPa, trueAlt_ft, refAlt_ft, refTemp_K, out _);
        }


        public void AddArmedLateralMode(LateralModeType mode)
        {
            lock (_armedLatModesLock)
            {
                if (!_armedLatModes.Contains(mode))
                {
                    _armedLatModes.Add(mode);
                }
            }
        }

        public void RemoveArmedLateralMode(LateralModeType mode)
        {
            lock (_armedLatModesLock)
            {
                _armedLatModes.Remove(mode);
            }
        }

        public void ClearArmedLateralModes()
        {
            lock (_armedLatModesLock)
            {
                _armedLatModes.Clear();
            }
        }

        public void AddArmedVerticalMode(VerticalModeType mode)
        {
            lock (_armedVertModesLock)
            {
                if (!_armedVertModes.Contains(mode))
                {
                    _armedVertModes.Add(mode);
                }
            }
        }

        public void RemoveArmedVerticalMode(VerticalModeType mode)
        {
            lock (_armedVertModesLock)
            {
                _armedVertModes.Remove(mode);
            }
        }

        public void ClearArmedVerticalModes()
        {
            lock (_armedVertModesLock)
            {
                _armedVertModes.Clear();
            }
        }

        public void AddArmedThrustMode(ThrustModeType mode)
        {
            lock (_armedThrustModesLock)
            {
                if (!_armedThrustModes.Contains(mode))
                {
                    _armedThrustModes.Add(mode);
                }
            }
        }

        public void RemoveArmedThrustMode(ThrustModeType mode)
        {
            lock (_armedThrustModesLock)
            {
                _armedThrustModes.Remove(mode);
            }
        }

        public void ClearArmedThrustModes()
        {
            lock (_armedThrustModesLock)
            {
                _armedThrustModes.Clear();
            }
        }

        public int SelectedHeading
        {
            get => _selHdg;
            set
            {
                if (value < 0 || value >= 360)
                {
                    _selHdg = 0;
                }
                else
                {
                    _selHdg = value;
                }
            }
        }

        public int SelectedAltitude
        {
            get => _selAlt;
            set
            {
                if (value < 0)
                {
                    _selAlt = 0;
                }
                else if (value > 99999)
                {
                    _selAlt = 99999;
                }
                else
                {
                    _selAlt = value;
                }
            }
        }

        public int SelectedVerticalSpeed
        {
            get => _selVs;
            set => _selVs = value;
        }

        public double SelectedFpa
        {
            get => _selFpa;
            set => _selFpa = value;
        }

        public McpSpeedSelectorType SelectedSpeedMode
        {
            get => _spdMode;
            set => _spdMode = value;
        }

        public McpSpeedUnitsType SelectedSpeedUnits
        {
            get => _spdUnits;
            set
            {
                GribDataPoint gribPoint = _parentAircraft.Position.GribPoint;
                double trueAlt_ft = _parentAircraft.Position.TrueAltitude;
                double trueAlt_m = MathUtil.ConvertFeetToMeters(trueAlt_ft);
                double refPres_hPa = gribPoint?.Level_hPa ?? AtmosUtil.ISA_STD_PRES_hPa;
                double refAlt_ft = gribPoint?.GeoPotentialHeight_Ft ?? 0;
                double refAlt_m = gribPoint?.GeoPotentialHeight_M ?? 0;
                double refTemp_K = gribPoint?.Temp_K ?? AtmosUtil.ISA_STD_TEMP_K;

                if (value == McpSpeedUnitsType.MACH && _spdUnits == McpSpeedUnitsType.KNOTS)
                {
                    // Convert selected speeds from IAS to Mach
                    AtmosUtil.ConvertIasToTas(_selSpd, refPres_hPa, trueAlt_ft, refAlt_ft, refTemp_K, out double maxMach);
                    _selSpd = (int)(maxMach * 100);
                }
                else if (value == McpSpeedUnitsType.KNOTS && _spdUnits == McpSpeedUnitsType.MACH)
                {
                    // Convert selected speeds from Mach to IAS
                    double T = AtmosUtil.CalculateTempAtAlt(trueAlt_m, refAlt_m, refTemp_K);
                    double selTas = MathUtil.ConvertMpersToKts(AtmosUtil.ConvertMachToTas(_selSpd / 100.0, T));
                    _selSpd = (int)AtmosUtil.ConvertTasToIas(selTas, refPres_hPa, trueAlt_ft, refAlt_ft, refTemp_K, out _);
                }

                _spdUnits = value;
            }
        }

        public int SelectedSpeed
        {
            get => _selSpd;
            set => _selSpd = value;
        }

        public LateralModeType CurrentLateralMode
        {
            get => _curLatMode;
            set => _curLatMode = value;
        }

        public List<LateralModeType> ArmedLateralModes
        {
            get
            {
                lock (_armedLatModesLock)
                {
                    return new List<LateralModeType>(_armedLatModes);
                }
            }
        }

        public VerticalModeType CurrentVerticalMode
        {
            get => _curVertMode;
            set => _curVertMode = value;
        }

        public List<VerticalModeType> ArmedVerticalModes
        {
            get
            {
                lock (_armedVertModesLock)
                {
                    return new List<VerticalModeType>(_armedVertModes);
                }
            }
        }

        public ThrustModeType CurrentThrustMode
        {
            get => _curThrustMode;
            set => _curThrustMode = value;
        }

        public List<ThrustModeType> ArmedThrustModes
        {
            get
            {
                lock (_armedThrustModesLock)
                {
                    return new List<ThrustModeType>(_armedThrustModes);
                }
            }
        }

        public McpKnobDirection HdgKnobTurnDirection
        {
            get => _hdgKnobDir;
            set => _hdgKnobDir = value;
        }
    }
}