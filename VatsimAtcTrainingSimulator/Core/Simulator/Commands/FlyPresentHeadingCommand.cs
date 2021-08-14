using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Commands
{
    public class FlyPresentHeadingCommand : IAircraftCommand
    {
        public VatsimClientPilot Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private int hdg;

        public void ExecuteCommand()
        {
            Aircraft.Control.CurrentLateralInstruction = new HeadingHoldInstruction(hdg);
        }

        public bool HandleCommand(ref List<string> args)
        {
            hdg = (int) Math.Round(Aircraft.Position.Heading_Mag, MidpointRounding.AwayFromZero);
            Logger?.Invoke($"{Aircraft.Callsign} flying present heading {hdg:000} degrees.");
            return true;
        }
    }
}
