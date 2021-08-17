using CoordinateSharp;
using CoordinateSharp.Magnetic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.GeoTools;
using VatsimAtcTrainingSimulator.Core.GeoTools.Helpers;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Commands
{
    public class DirectWaypointCommand : IAircraftCommand
    {
        public VatsimClientPilot Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private Waypoint wp;

        public void ExecuteCommand()
        {
            RouteWaypoint rwp = new RouteWaypoint(wp);

            Aircraft.Control.FMS.ActivateDirectTo(rwp, Aircraft.Position);

            LnavRouteInstruction instr = new LnavRouteInstruction();
            Aircraft.Control.CurrentLateralInstruction = instr;
        }

        public bool HandleCommand(ref List<string> args)
        {
            // Check argument length
            if (args.Count < 1)
            {
                Logger?.Invoke($"ERROR: Direct Waypoint requires at least 1 argument!");
                return false;
            }

            // Get waypoint string
            string wpStr = args[0];
            args.RemoveAt(0);

            // Find Waypoint
            wp = DataHandler.GetClosestWaypointByIdentifier(wpStr, Aircraft.Position.Latitude, Aircraft.Position.Longitude);

            if (wp == null)
            {
                Logger?.Invoke($"ERROR: Waypoint {wpStr} not found!");
                return false;
            }

            Logger?.Invoke($"{Aircraft.Callsign} proceeding direct {wp.Identifier}.");

            return true;
        }
    }
}
