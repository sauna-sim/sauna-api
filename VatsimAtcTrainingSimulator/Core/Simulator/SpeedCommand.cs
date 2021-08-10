using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator
{
    public class SpeedCommand : IAircraftCommand
    {
        public Action<string> Logger { get; set; }

        public VatsimClientPilot Aircraft { get; set; }

        private AssignedIASType Type { get; set; }

        private int Ias { get; set; }

        public void ExecuteCommand()
        {
            // Add speed assignment to aircraft
            Aircraft.Assigned_IAS = Ias;
            Aircraft.Assigned_IAS_Type = Type;
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
                    Type = AssignedIASType.FREE;
                    Ias = -1;

                    Logger?.Invoke($"{Aircraft.Callsign} resuming normal speed.");
                }
                else if (speed.StartsWith(">"))
                {
                    Type = AssignedIASType.MORE;
                    Ias = Convert.ToInt32(speed.Substring(1));

                    Logger?.Invoke($"{Aircraft.Callsign} maintaining {Ias}kts or greater.");
                }
                else if (speed.StartsWith("<"))
                {
                    Type = AssignedIASType.LESS;
                    Ias = Convert.ToInt32(speed.Substring(1));

                    Logger?.Invoke($"{Aircraft.Callsign} maintaining {Ias}kts or less.");
                }
                else
                {
                    Type = AssignedIASType.EXACT;
                    Ias = Convert.ToInt32(speed);

                    Logger?.Invoke($"{Aircraft.Callsign} maintaining {Ias}kts.");
                }                
            }
            catch (InvalidCastException)
            {
                Logger?.Invoke($"ERROR: Speed {speed} not valid!");
                return false;
            }

            // Return remaining arguments
            return true;
        }
    }
}
