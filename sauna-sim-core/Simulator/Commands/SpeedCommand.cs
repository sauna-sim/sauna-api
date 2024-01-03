using SaunaSim.Core.Simulator.Aircraft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaunaSim.Core.Simulator.Aircraft.Autopilot;

namespace SaunaSim.Core.Simulator.Commands
{
    public class SpeedCommand : IAircraftCommand
    {
        public Action<string> Logger { get; set; }

        public SimAircraft Aircraft { get; set; }

        private ConstraintType Type { get; set; }

        private int Ias { get; set; }

        public void ExecuteCommand()
        {
            // Add speed assignment to aircraft
            if (Type != ConstraintType.FREE)
            {
                Aircraft.Autopilot.SelectedSpeed = Ias;
                Aircraft.Autopilot.SelectedSpeedUnits = McpSpeedUnitsType.KNOTS;
            }

            Aircraft.Autopilot.SelectedSpeedMode = Type == ConstraintType.FREE ? McpSpeedSelectorType.FMS : McpSpeedSelectorType.MANUAL;            
        }

        public bool HandleCommand(SimAircraft aircraft, Action<string> logger, ConstraintType constraintType, int speed)
        {
            Aircraft = aircraft;
            Logger = logger;

            Type = constraintType;

            if (constraintType == ConstraintType.FREE)
            {
                Ias = -1;
                Logger?.Invoke($"{Aircraft.Callsign} resuming normal speed.");
            } else
            {
                Ias = speed;
                Logger?.Invoke($"{Aircraft.Callsign} maintaining {Ias}kts{(constraintType == ConstraintType.LESS ? " or less" : (constraintType == ConstraintType.MORE ? " or greater" : ""))}.");
            }

            return true;
        }

        public bool HandleCommand(ref List<string> args)
        {
            // Check argument length
            if (args.Count < 1)
            {
                Logger?.Invoke($"ERROR: Turn Right Heading requires at least 1 argument!");
                return false;
            }

            // Get speed string
            string speed = args[0];
            args.RemoveAt(0);

            // Get speed value
            try
            {
                if (speed.ToLower().Equals("none"))
                {
                    Type = ConstraintType.FREE;
                    Ias = -1;

                    Logger?.Invoke($"{Aircraft.Callsign} resuming normal speed.");
                }
                else if (speed.StartsWith(">"))
                {
                    Type = ConstraintType.MORE;
                    Ias = Convert.ToInt32(speed.Substring(1));

                    Logger?.Invoke($"{Aircraft.Callsign} maintaining {Ias}kts or greater.");
                }
                else if (speed.StartsWith("<"))
                {
                    Type = ConstraintType.LESS;
                    Ias = Convert.ToInt32(speed.Substring(1));

                    Logger?.Invoke($"{Aircraft.Callsign} maintaining {Ias}kts or less.");
                }
                else
                {
                    Type = ConstraintType.EXACT;
                    Ias = Convert.ToInt32(speed);

                    Logger?.Invoke($"{Aircraft.Callsign} maintaining {Ias}kts.");
                }                
            }
            catch (Exception)
            {
                Logger?.Invoke($"ERROR: Speed {speed} not valid!");
                return false;
            }

            // Return remaining arguments
            return true;
        }
    }
}
