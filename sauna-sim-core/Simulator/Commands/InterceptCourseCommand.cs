using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NavData_Interface.Objects.Fix;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS;

namespace SaunaSim.Core.Simulator.Commands
{
    public class InterceptCourseCommand : IAircraftCommand
    {
        public SimAircraft Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private int course;
        private Fix wp;

        public void ExecuteCommand()
        {
            Aircraft.Control.ArmedLateralInstruction = new InterceptCourseInstruction(new RouteWaypoint(wp), course);
        }

        public bool HandleCommand(SimAircraft aircraft, Action<string> logger, string waypoint, int course)
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

            this.course = course;
            Logger?.Invoke($"{Aircraft.Callsign} intercepting course {course} of waypoint {wp.Identifier}");

            return true;
        }

        public bool HandleCommand(ref List<string> args)
        {
            // Check argument length
            if (args.Count < 2)
            {
                Logger?.Invoke($"ERROR: Intercept Course requires at least 2 arguments!");
                return false;
            }

            // Get waypoint string
            string wpStr = args[0];

            // Get course string
            string courseStr = args[1];

            args.RemoveAt(1);
            args.RemoveAt(0);

            // Find Waypoint
            wp = DataHandler.GetClosestWaypointByIdentifier(wpStr, Aircraft.Position.Latitude, Aircraft.Position.Longitude);

            if (wp == null)
            {
                Logger?.Invoke($"ERROR: Waypoint {wpStr} not found!");
                return false;
            }

            try
            {
                // Parse course
                course = Convert.ToInt32(courseStr);

                Logger?.Invoke($"{Aircraft.Callsign} intercepting course {courseStr} of waypoint {wp.Identifier}");
            }
            catch (Exception)
            {
                Logger?.Invoke($"ERROR: Course {courseStr} not valid!");
                return false;
            }

            return true;
        }
    }
}
