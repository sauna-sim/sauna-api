using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NavData_Interface.Objects.Fixes;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS;

namespace SaunaSim.Core.Simulator.Commands
{
    public class DirectWaypointCommand : IAircraftCommand
    {
        public SimAircraft Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private Fix wp;

        public void ExecuteCommand()
        {
            RouteWaypoint rwp = new RouteWaypoint(wp);

            Aircraft.Fms.ActivateDirectTo(rwp);
            
            Aircraft.Autopilot.AddArmedLateralMode(LateralModeType.LNAV);
        }

        public bool HandleCommand(SimAircraft aircraft, Action<string> logger, string waypoint)
        {
            this.Aircraft = aircraft;
            this.Logger = logger;

            // Find Waypoint
            wp = DataHandler.GetClosestWaypointByIdentifier(waypoint, Aircraft.Position.Latitude, Aircraft.Position.Longitude);

            if (wp == null)
            {
                Logger?.Invoke($"ERROR: Waypoint {waypoint} not found!");
                return false;
            }

            Logger?.Invoke($"{Aircraft.Callsign} proceeding direct {wp.Identifier}.");

            return true;
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
