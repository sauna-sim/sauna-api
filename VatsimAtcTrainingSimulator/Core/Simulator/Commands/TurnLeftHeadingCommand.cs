using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Commands
{
    public class TurnLeftHeadingCommand : IAircraftCommand
    {
        public Action<string> Logger { get; set; }

        public VatsimClientPilot Aircraft { get; set; }

        private int Hdg { get; set; }

        public void ExecuteCommand()
        {
            Aircraft.Control.CurrentLateralInstruction = new HeadingHoldInstruction(TurnDirection.LEFT, Hdg);
        }

        public bool HandleCommand(ref List<string> args)
        {
            // Check argument length
            if (args.Count < 1)
            {
                Logger?.Invoke($"ERROR: Turn Left Heading requires at least 1 argument!");
                return false;
            }

            // Get heading string
            string headingString = args[0];
            args.RemoveAt(0);

            try
            {
                // Parse heading
                Hdg = Convert.ToInt32(headingString);

                Logger?.Invoke($"{Aircraft.Callsign} turning left heading {headingString} degrees.");

                // Check > 180 deg
                if (GeoTools.AcftGeoUtil.CalculateTurnAmount(Aircraft.Position.Heading_Mag, Hdg) > 0)
                {
                    Logger?.Invoke($"WARNING: {Aircraft.Callsign} left turn exceeds 180 degrees!!");
                }
            }
            catch (Exception)
            {
                Logger?.Invoke($"ERROR: Heading {headingString} not valid!");
                return false;
            }

            return true;
        }
    }
}
