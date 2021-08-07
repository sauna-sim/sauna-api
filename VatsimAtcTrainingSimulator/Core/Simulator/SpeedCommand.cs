using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator
{
    public class SpeedCommand : IAircraftCommand
    {
        public string CommandName => "spd";

        public Action<string> Logger { get; set; }

        public List<string> HandleCommand(VatsimClientPilot aircraft, List<string> args)
        {
            // Check argument length
            if (args.Count < 1)
            {
                throw new ArgumentOutOfRangeException("args", "must have at least 1 value");
            }

            // Get speed string
            string speed = args[0];
            args.RemoveAt(0);

            // Default values
            AssignedIASType type = AssignedIASType.EXACT;
            int ias = 0;

            // Get speed value
            try
            {
                if (speed.ToLower().Equals("none"))
                {
                    type = AssignedIASType.FREE;
                    ias = -1;

                    Logger?.Invoke($"{aircraft.Callsign} resuming normal speed.");
                }
                else if (speed.StartsWith(">"))
                {
                    type = AssignedIASType.MORE;
                    ias = Convert.ToInt32(speed.Substring(1));

                    Logger?.Invoke($"{aircraft.Callsign} maintaining {ias}kts or greater.");
                }
                else if (speed.StartsWith("<"))
                {
                    type = AssignedIASType.LESS;
                    ias = Convert.ToInt32(speed.Substring(1));

                    Logger?.Invoke($"{aircraft.Callsign} maintaining {ias}kts or less.");
                }
                else
                {
                    type = AssignedIASType.EXACT;
                    ias = Convert.ToInt32(speed);

                    Logger?.Invoke($"{aircraft.Callsign} maintaining {ias}kts.");
                }

                // Add speed assignment to aircraft
                aircraft.Assigned_IAS = ias;
                aircraft.Assigned_IAS_Type = type;
            }
            catch (InvalidCastException)
            {
                Logger?.Invoke($"ERROR: Speed {speed} not valid!");
            }

            // Return remaining arguments
            return args;
        }
    }
}
