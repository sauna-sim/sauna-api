using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Aircraft.Autopilot;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;

namespace SaunaSim.Core.Simulator.Commands
{
    public class FlyPresentHeadingCommand : IAircraftCommand
    {
        public SimAircraft Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private int hdg;

        public void ExecuteCommand()
        {
            Aircraft.Autopilot.SelectedHeading = hdg;
            Aircraft.Autopilot.HdgKnobTurnDirection = McpKnobDirection.SHORTEST;
        }

        public bool HandleCommand(SimAircraft aircraft, Action<string> logger)
        {
            Aircraft = Aircraft;
            Logger = logger;
            hdg = (int)Math.Round(Aircraft.Position.Heading_Mag, MidpointRounding.AwayFromZero);
            Logger?.Invoke($"{Aircraft.Callsign} flying present heading {hdg:000} degrees.");
            return true;
        }
        public bool HandleCommand(ref List<string> args)
        {
            hdg = (int)Math.Round(Aircraft.Position.Heading_Mag, MidpointRounding.AwayFromZero);
            Logger?.Invoke($"{Aircraft.Callsign} flying present heading {hdg:000} degrees.");
            return true;
        }
    }
}
