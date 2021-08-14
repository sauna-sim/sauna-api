using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Commands
{
    public class InterceptCourseCommand : IAircraftCommand
    {
        public VatsimClientPilot Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private int course;
        private Waypoint wp;

        public void ExecuteCommand()
        {
            Aircraft.Control.ArmedLateralInstruction = new InterceptCourseInstruction(wp, course);
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
