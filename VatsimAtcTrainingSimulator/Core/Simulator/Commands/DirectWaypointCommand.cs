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
using VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Commands
{
    public class DirectWaypointCommand : IAircraftCommand
    {
        public VatsimClientPilot Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private Waypoint wp;

        public void ExecuteCommand()
        {
            // Calculate direct course
            double course = AcftGeoUtil.CalculateDirectBearingAfterTurn(
                new GeoPoint(Aircraft.Position.Latitude, Aircraft.Position.Longitude, Aircraft.Position.AbsoluteAltitude),
                new GeoPoint(wp.Latitude, wp.Longitude),
                AcftGeoUtil.CalculateRadiusOfTurn(AcftGeoUtil.CalculateBankAngle(Aircraft.Position.GroundSpeed, 25, 3), Aircraft.Position.GroundSpeed),
                Aircraft.Position.Track_True);            

            if (course >= 0)
            {
                // Check if waypoint exists in route
                bool foundWaypoint = false;
                LinkedListNode<string> node = null;
                while (Aircraft.Position.Route.Count > 0)
                {
                    if (node == null)
                    {
                        node = Aircraft.Position.Route.First;
                    }

                    Waypoint foundWp = DataHandler.GetClosestWaypointByIdentifier(node.Value, Aircraft.Position.Latitude, Aircraft.Position.Longitude);

                    LinkedListNode<string> nextNode = node.Next;

                    if (foundWp == null)
                    {
                        Aircraft.Position.Route.Remove(node);
                    }
                    else if (foundWp == wp)
                    {
                        foundWaypoint = true;
                        break;
                    }

                    if (nextNode == null)
                    {
                        break;
                    }

                    node = nextNode;
                }

                if (foundWaypoint && node != null)
                {
                    // Remove all other preceding waypoints
                    while (Aircraft.Position.Route.First != node)
                    {
                        Aircraft.Position.Route.RemoveFirst();
                    }

                    // Activate LNAV instruction
                    LnavRouteInstruction instr = new LnavRouteInstruction
                    {
                        InitialTrueCourse = course
                    };

                    Aircraft.Control.CurrentLateralInstruction = instr;
                }
                else
                {
                    InterceptCourseInstruction instr = new InterceptCourseInstruction(wp)
                    {
                        TrueCourse = course
                    };

                    Aircraft.Control.CurrentLateralInstruction = instr;
                }                
            }
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
