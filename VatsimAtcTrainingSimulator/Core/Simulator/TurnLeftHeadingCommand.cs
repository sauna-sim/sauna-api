using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator
{
    public class TurnLeftHeadingCommand : IAircraftCommand
    {
        public string CommandName => "tl";

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
                aircraft.Assigned_TurnDirection = TurnDirection.LEFT;

                Logger?.Invoke($"{aircraft.Callsign} turning left heading {aircraft.Assigned_Heading} degrees.");

                // Check > 180 deg
                double headingDifference = aircraft.Position.Heading_Mag - aircraft.Assigned_Heading;
                if (GeoTools.AcftGeoUtil.CalculateTurnAmount(aircraft.Position.Heading_Mag, aircraft.Assigned_Heading) > 0)
                {
                    Logger?.Invoke($"WARNING: {aircraft.Callsign} left turn exceeds 180 degrees!!");
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
