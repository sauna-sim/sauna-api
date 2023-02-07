using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.MathTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AselAtcTrainingSim.AselSimCore.Simulator.Aircraft;

namespace AselAtcTrainingSim.AselSimCore.Simulator.Commands
{
    public class AltitudeCommand : IAircraftCommand
    {
        public VatsimClientPilot Aircraft { get; set; }
        public Action<string> Logger { get; set; }
        private int alt = 0;
        private bool isFlightLevel = false;
        private double altimSetting = -1;

        public void ExecuteCommand()
        {
            if (isFlightLevel && Aircraft.Position.AltimeterSetting_hPa != AtmosUtil.ISA_STD_PRES_hPa)
            {
                Aircraft.Position.AltimeterSetting_hPa = AtmosUtil.ISA_STD_PRES_hPa;
            }
            else if (!isFlightLevel)
            {
                if (altimSetting >= 0)
                {
                    Aircraft.Position.AltimeterSetting_hPa = altimSetting;
                }
                else
                {
                    Aircraft.Position.AltimeterSetting_hPa = Aircraft.Position.SurfacePressure_hPa;
                }
            }

            int vs = 0;
            if (alt < Aircraft.Position.IndicatedAltitude)
            {
                vs = -1800;
            } else if (alt > Aircraft.Position.IndicatedAltitude)
            {
                vs = 2500;
            }
            Aircraft.Control.CurrentVerticalInstruction = new VerticalSpeedInstruction(vs);
            Aircraft.Control.AddArmedVerticalInstruction(new AltitudeHoldInstruction(alt));
        }

        public bool HandleCommand(ref List<string> args)
        {
            // Check argument length
            if (args.Count < 1)
            {
                Logger?.Invoke($"ERROR: Altitude requires at least 1 argument!");
                return false;
            }

            // Parse altitude
            string altStr = args[0];
            args.RemoveAt(0);

            try
            {
                if (altStr.ToUpper().StartsWith("FL"))
                {
                    isFlightLevel = true;
                    alt = Convert.ToInt32(altStr.Substring(2)) * 100;
                } else if (altStr.ToUpper().StartsWith("A"))
                {
                    isFlightLevel = false;
                    alt = Convert.ToInt32(altStr.Substring(1));
                } else
                {
                    alt = Convert.ToInt32(altStr);
                    if (alt < 1000)
                    {
                        alt *= 100;
                        altStr = $"FL{altStr}";
                    }
                }
                Logger?.Invoke($"{Aircraft.Callsign} maintaining {altStr}.");
            }
            catch (Exception)
            {
                Logger?.Invoke($"ERROR: Altitude {altStr} not valid!");
                return false;
            }

            // Parse Pressure if applicable
            if (args.Count >= 2)
            {
                try
                {
                    if (args[0].ToLower().Contains("qnh"))
                    {
                        string qnhStr = args[1];
                        args.RemoveAt(0);
                        args.RemoveAt(0);

                        altimSetting = Convert.ToDouble(qnhStr);

                        Logger?.Invoke($"{Aircraft.Callsign} pressure set to {qnhStr}hPa.");
                    }
                    else if (args[0].ToLower().Contains("alt"))
                    {
                        string inHgStr = args[1];
                        args.RemoveAt(0);
                        args.RemoveAt(0);

                        double inHg = Convert.ToDouble(inHgStr);

                        if (inHg >= 100)
                        {
                            inHg /= 100;
                        }

                        altimSetting = MathUtil.ConvertInhgToHpa(inHg);


                        Logger?.Invoke($"{Aircraft.Callsign} pressure set to {inHg.ToString("00.00")}inHg.");
                    }
                } catch (InvalidCastException)
                {
                    Logger?.Invoke($"ERROR: Pressure {args[1]} not valid!");
                }
            }

            return true;
        }
    }
}
