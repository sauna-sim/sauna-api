using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NavData_Interface.Objects.Fix;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Aircraft.Autopilot;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;

namespace SaunaSim.Core.Simulator.Commands
{
    public class DepartOnHeadingCommand : IAircraftCommand
    {
        public SimAircraft Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private int hdg;
        private IRoutePoint point;

        public void ExecuteCommand() { }

        public void OnReachingWaypoint(object sender, WaypointPassedEventArgs e)
        {
            if (e.RoutePoint.Equals(point))
            {
                Aircraft.Autopilot.SelectedHeading = hdg;
                Aircraft.Autopilot.HdgKnobTurnDirection = McpKnobDirection.SHORTEST;
                Aircraft.Autopilot.CurrentLateralMode = LateralModeType.HDG;
            }
        }

        public bool HandleCommand(SimAircraft aircraft, Action<string> logger, string waypoint, int heading)
        {
            Aircraft = aircraft;
            Logger = logger;

            // Find Waypoint
            Fix wp = DataHandler.GetClosestWaypointByIdentifier(waypoint, Aircraft.Position.Latitude, Aircraft.Position.Longitude);

            if (wp == null)
            {
                Logger?.Invoke($"ERROR - Waypoint {waypoint} not found!");
                return false;
            }

            // Get Route Leg
            IRouteLeg leg = Aircraft.Fms.GetLegToPoint(new RouteWaypoint(wp));

            if (leg == null)
            {
                Logger?.Invoke($"ERROR - Waypoint {waypoint} does not exist in route!");
                return false;
            }

            hdg = heading;
            leg.EndPoint.PointType = RoutePointTypeEnum.FLY_OVER;
            point = leg.EndPoint.Point;
            Aircraft.Fms.WaypointPassed += OnReachingWaypoint;

            Logger?.Invoke($"{Aircraft.Callsign} will depart {waypoint} heading {heading:000} degrees.");
            return true;
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
            Fix wp = DataHandler.GetClosestWaypointByIdentifier(wpStr, Aircraft.Position.Latitude, Aircraft.Position.Longitude);

            if (wp == null)
            {
                Logger?.Invoke($"ERROR - Waypoint {wpStr} not found!");
                return false;
            }

            // Get Route Leg
            IRouteLeg leg = Aircraft.Fms.GetLegToPoint(new RouteWaypoint(wp));

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
                Aircraft.Fms.WaypointPassed += OnReachingWaypoint;

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
