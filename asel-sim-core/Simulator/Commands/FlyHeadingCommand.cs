using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AselAtcTrainingSim.AselSimCore.Simulator.Aircraft;

namespace AselAtcTrainingSim.AselSimCore.Simulator.Commands
{
    public class FlyHeadingCommand : IAircraftCommand
    {
        public Action<string> Logger { get; set; }

        public VatsimClientPilot Aircraft { get; set; }

        private int Hdg { get; set; }

        public void ExecuteCommand()
        {
            Aircraft.Control.CurrentLateralInstruction = new HeadingHoldInstruction(Hdg);
        }

        public bool HandleCommand(VatsimClientPilot aircraft, Action<string> logger, int hdg)
        {
            Aircraft = aircraft;
            Logger = logger;
            Hdg = hdg;
            Logger?.Invoke($"{Aircraft.Callsign} flying heading {hdg:000} degrees.");
            return true;
        }

        public bool HandleCommand(ref List<string> args)
        {
            // Check argument length
            if (args.Count < 1)
            {
                Logger?.Invoke($"ERROR: Fly Heading requires at least 1 argument!");
                return false;
            }

            // Get heading string
            string headingString = args[0];
            args.RemoveAt(0);

            try
            {
                // Parse heading
                Hdg = Convert.ToInt32(headingString);

                Logger?.Invoke($"{Aircraft.Callsign} flying heading {headingString} degrees.");
            } catch (Exception)
            {
                Logger?.Invoke($"ERROR: Heading {headingString} not valid!");
                return false;
            }            

            return true;
        }
    }
}
