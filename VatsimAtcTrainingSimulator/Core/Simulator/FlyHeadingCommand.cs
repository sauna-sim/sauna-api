using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
                int hdg = Convert.ToInt32(headingString);

                Thread t = new Thread(
                () =>
                {
                    // Generate random delay
                    int delay = new Random().Next(0, 3000);
                    Thread.Sleep(delay);

                    // Set heading
                    aircraft.Assigned_Heading = hdg;

                    // Set turn direction
                    aircraft.Assigned_TurnDirection = TurnDirection.SHORTEST;
                });

                t.Start();

                Logger?.Invoke($"{aircraft.Callsign} flying heading {aircraft.Assigned_Heading} degrees.");
            } catch (InvalidCastException)
            {
                Logger?.Invoke($"ERROR: Heading {headingString} not valid!");
            }            

            return args;
        }
    }
}
