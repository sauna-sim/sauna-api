using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.Control.Instructions.Lateral;

namespace SaunaSim.Core.Simulator.Commands
{
    public class LocCommand : IAircraftCommand
    {
        public VatsimClientPilot Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private Localizer _loc;

        public void ExecuteCommand()
        {
            IlsApproachInstruction instr = new IlsApproachInstruction(_loc);
            Aircraft.Control.ArmedLateralInstruction = instr;
        }

        public bool HandleCommand(VatsimClientPilot aircraft, Action<string> logger, string runway)
        {
            Aircraft = aircraft;
            Logger = logger;
            // Find Waypoint
            Waypoint wp = DataHandler.GetClosestWaypointByIdentifier($"ILS{runway}", Aircraft.Position.Latitude, Aircraft.Position.Longitude);

            if (wp == null || !(wp is Localizer))
            {
                Logger?.Invoke($"ERROR: Localizer {runway} not found!");
                return false;
            }

            _loc = (Localizer)wp;

            Logger?.Invoke($"{Aircraft.Callsign} intercepting localizer {runway}");

            return true;
        }

        public bool HandleCommand(ref List<string> args)
        {
            // Check argument length
            if (args.Count < 1)
            {
                Logger?.Invoke($"ERROR: LOC requires at least 1 arguments!");
                return false;
            }

            // Get runway string
            string rwyStr = args[0];

            args.RemoveAt(0);

            // Find Waypoint
            Waypoint wp = DataHandler.GetClosestWaypointByIdentifier($"ILS{rwyStr}", Aircraft.Position.Latitude, Aircraft.Position.Longitude);

            if (wp == null || !(wp is Localizer))
            {
                Logger?.Invoke($"ERROR: Localizer {rwyStr} not found!");
                return false;
            }

            _loc = (Localizer)wp;

            Logger?.Invoke($"{Aircraft.Callsign} intercepting localizer {rwyStr}");

            return true;
        }
    }
}
