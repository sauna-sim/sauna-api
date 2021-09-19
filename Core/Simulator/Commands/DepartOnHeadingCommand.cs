using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS.Legs;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Commands
{
    public class DepartOnHeadingCommand : IAircraftCommand
    {
        public VatsimClientPilot Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private int hdg;
        private IRoutePoint point;

        public void ExecuteCommand() { }

        public void OnReachingWaypoint(object sender, WaypointPassedEventArgs e)
        {
            if (e.RoutePoint.Equals(point))
            {
                Aircraft.Control.CurrentLateralInstruction = new HeadingHoldInstruction(hdg);
            }
        }

        public bool HandleCommand(ref List<string> args)
        {
            // Check argument length
            if (args.Count < 2)
            {
                Logger?.Invoke($"ERROR - Depart On Heading requires at least 2 arguments!");
                return false;
            }

            // Get waypoint string
            string wpStr = args[0];
            args.RemoveAt(0);

            // Find Waypoint
            Waypoint wp = DataHandler.GetClosestWaypointByIdentifier(wpStr, Aircraft.Position.Latitude, Aircraft.Position.Longitude);

            if (wp == null)
            {
                Logger?.Invoke($"ERROR - Waypoint {wpStr} not found!");
                return false;
            }

            // Get Route Leg
            IRouteLeg leg = Aircraft.Control.FMS.GetLegToPoint(new RouteWaypoint(wp));

            if (leg == null)
            {
                Logger?.Invoke($"ERROR - Waypoint {wpStr} does not exist in route!");
                return false;
            }

            // Get heading string
            string headingString = args[0];
            args.RemoveAt(0);

            try
            {
                // Parse heading
                hdg = Convert.ToInt32(headingString);

                leg.EndPoint.PointType = RoutePointTypeEnum.FLY_OVER;
                point = leg.EndPoint.Point;
                Aircraft.Control.FMS.WaypointPassed += OnReachingWaypoint;

                Logger?.Invoke($"{Aircraft.Callsign} will depart {wpStr} heading {headingString} degrees.");
            }
            catch (Exception)
            {
                Logger?.Invoke($"ERROR - Heading {headingString} not valid!");
                return false;
            }

            return true;
        }
    }
}
