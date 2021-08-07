using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator
{
    public class FlyHeadingCommand : IAircraftCommand
    {
        public string CommandName => "fh";

        public Action<string> Logger { get; set; }

        public List<string> HandleCommand(VatsimClientPilot aircraft, List<string> args)
        {
            // Check argument length
            if (args.Count < 1)
            {
                throw new ArgumentOutOfRangeException("args", "must have at least 1 value");
            }

            // Get heading string
            string headingString = args[0];
            args.RemoveAt(0);
            
            try
            {
                // Parse heading
                aircraft.Assigned_Heading = Convert.ToInt32(headingString);

                // Set turn direction
                aircraft.Assigned_TurnDirection = TurnDirection.SHORTEST;


                Logger?.Invoke($"{aircraft.Callsign} flying heading {aircraft.Assigned_Heading} degrees.");
            } catch (InvalidCastException)
            {
                Logger?.Invoke($"ERROR: Heading {headingString} not valid!");
            }

            return args;
        }
    }
}
