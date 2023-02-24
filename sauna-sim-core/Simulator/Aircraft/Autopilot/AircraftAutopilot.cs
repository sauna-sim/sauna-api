using System;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.GeoTools.GribTools;
using AviationCalcUtilNet.MathTools;
using System.Collections.Generic;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;

namespace SaunaSim.Core.Simulator.Aircraft.Autopilot
{
    public class AircraftAutopilot
    {
        private SimAircraft _parentAircraft;

        // Modes
        private LateralModeType _curLatMode;
        private List<LateralModeType> _armedLatModes;
        private VerticalModeType _curVertMode;
        private List<VerticalModeType> _armedVertModes;
        private ThrustModeType _curThrustMode;
        private List<ThrustModeType> _armedThrustModes;

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

        public void UpdatePosition(int intervalMs)
        {
            UpdateBank(intervalMs);
        }

        private void UpdateBank(int intervalMs)
        {
            if (_curLatMode == LateralModeType.BANK)
            {
                // No change needed
                _parentAircraft.Position.BankRate = 0;
            }
            else if (_curLatMode == LateralModeType.HDG)
            {
                double hdgDelta = GeoUtil.CalculateTurnAmount(_parentAircraft.Position.Heading_Mag, _selHdg);

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
                    } else if (!isRightTurn && hdgDelta > 0)
                    {
                        hdgDelta -= 360;
                    }
                    
                    // Desired bank angle
                    double desiredBank = AutopilotUtil.CalculateDesiredRollAngle(hdgDelta, _parentAircraft.Position.Bank, _parentAircraft.Position.GroundSpeed);
                    _parentAircraft.Position.BankRate = AutopilotUtil.CalculateRollRate(desiredBank, _parentAircraft.Position.Bank);
                }
                else
                {
                    _parentAircraft.Position.BankRate = 0;
                }
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

                if (value == McpSpeedUnitsType.MACH)
                {
                    // Convert selected speeds from IAS to Mach
                    AtmosUtil.ConvertIasToTas(_selSpd, refPres_hPa, trueAlt_ft, refAlt_ft, refTemp_K, out double maxMach);
                    _selSpd = (int)(maxMach * 100);
                }
                else
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
            get => _armedLatModes;
            set => _armedLatModes = value;
        }

        public VerticalModeType CurrentVerticalMode
        {
            get => _curVertMode;
            set => _curVertMode = value;
        }

        public List<VerticalModeType> ArmedVerticalModes
        {
            get => _armedVertModes;
            set => _armedVertModes = value;
        }

        public ThrustModeType CurrentThrustMode
        {
            get => _curThrustMode;
            set => _curThrustMode = value;
        }

        public List<ThrustModeType> ArmedThrustModes
        {
            get => _armedThrustModes;
            set => _armedThrustModes = value;
        }

        public McpKnobDirection HdgKnobTurnDirection
        {
            get => _hdgKnobDir;
            set => _hdgKnobDir = value;
        }
    }
}