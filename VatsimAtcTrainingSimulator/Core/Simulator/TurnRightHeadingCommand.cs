using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator
{
    public class TurnRightHeadingCommand : IAircraftCommand
    {
        public string CommandName => "tr";

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

                    // Parse heading
                    aircraft.Assigned_Heading = hdg;

                    // Set turn direction
                    aircraft.Assigned_TurnDirection = TurnDirection.RIGHT;
                });

                t.Start();                

                Logger?.Invoke($"{aircraft.Callsign} turning right heading {aircraft.Assigned_Heading} degrees.");

                // Check > 180 deg
                double headingDifference = aircraft.Position.Heading_Mag - aircraft.Assigned_Heading;
                if (GeoTools.AcftGeoUtil.CalculateTurnAmount(aircraft.Position.Heading_Mag, aircraft.Assigned_Heading) < 0)
                {
                    Logger?.Invoke($"WARNING: {aircraft.Callsign} right turn exceeds 180 degrees!!");
                }
            }
            catch (InvalidCastException)
            {
                Logger?.Invoke($"ERROR: Heading {headingString} not valid!");
            }

            return args;
        }
    }
}
