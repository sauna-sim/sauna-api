using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Commands
{
    public class IlsCommand : IAircraftCommand
    {
        public VatsimClientPilot Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private Localizer _loc;

        public void ExecuteCommand()
        {
            Aircraft.Control.ArmedLateralInstruction = new InterceptCourseInstruction(new RouteWaypoint(_loc), _loc.Course);
        }

        public bool HandleCommand(ref List<string> args)
        {
            // Check argument length
            if (args.Count < 1)
            {
                Logger?.Invoke($"ERROR: ILS/LOC requires at least 1 arguments!");
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
