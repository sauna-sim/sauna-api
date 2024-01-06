using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AviationCalcUtilNet.Geo;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fixes;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Aircraft.FMS;

namespace SaunaSim.Core.Simulator.Commands
{
    public class HoldCommand : IAircraftCommand
    {
        public SimAircraft Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        public void ExecuteCommand()
        {
            
        }

        public bool HandleCommand(SimAircraft aircraft, Action<string> logger, string waypoint, bool isPublishedHold, int inboundCourse, HoldTurnDirectionEnum turnDir, HoldLegLengthTypeEnum legLengthType, double legLength)
        {
            Aircraft = aircraft;
            Logger = logger;

            // Find Waypoint
            Fix wp = DataHandler.GetClosestWaypointByIdentifier(waypoint, Aircraft.Position.PositionGeoPoint);

            if (wp == null)
            {
                Logger?.Invoke($"ERROR - Waypoint {waypoint} not found!");
                return false;
            }

            if (isPublishedHold)
            {
                PublishedHold pubHold = DataHandler.GetPublishedHold(wp.Identifier, wp.Location);

                if (pubHold == null)
                {
                    Logger?.Invoke($"ERROR - No published hold found for waypoint {wp.Identifier}!");
                    return false;
                }
                IRoutePoint holdPt = new RouteWaypoint(wp);

                if (!Aircraft.Fms.AddHold(holdPt, pubHold.InboundCourse, pubHold.TurnDirection, pubHold.LegLengthType, pubHold.LegLength))
                {
                    Logger?.Invoke($"ERROR - {wp.Identifier} not found in flight plan!");
                    return false;
                }

                Logger?.Invoke($"{Aircraft.Callsign} will hold at {wp.Identifier} as published.");
                return true;
            }

            if (!Aircraft.Fms.AddHold(new RouteWaypoint(wp), Bearing.FromDegrees(inboundCourse), turnDir, legLengthType, legLength))
            {
                Logger?.Invoke($"ERROR - {wp.Identifier} not found in flight plan!");
                return false;
            }
            string turnDirStr = turnDir == HoldTurnDirectionEnum.RIGHT ? "Right" : "Left";
            string distanceStr = (legLengthType == HoldLegLengthTypeEnum.DISTANCE) ? $", {legLength}nm legs" :
                ((legLengthType == HoldLegLengthTypeEnum.TIME) ? $", {legLength}min legs" : "");

            Logger?.Invoke($"{Aircraft.Callsign} will hold at {wp.Identifier}, inbound course {inboundCourse:000}, {turnDirStr} turns{distanceStr}.");
            return true;
        }

        public bool HandleCommand(ref List<string> args)
        {
            // Check argument length
            if (args.Count < 1)
            {
                Logger?.Invoke($"ERROR - Hold requires at least 1 argument!");
                return false;
            }

            // Get waypoint string
            string wpStr = args[0];
            args.RemoveAt(0);

            // Find Waypoint
            Fix wp = DataHandler.GetClosestWaypointByIdentifier(wpStr, Aircraft.Position.PositionGeoPoint);

            if (wp == null)
            {
                Logger?.Invoke($"ERROR - Waypoint {wpStr} not found!");
                return false;
            }

            if (args.Count >= 1)
            {
                try
                {
                    string[] items = args[0].Split('/');

                    double inbdCrs = Convert.ToDouble(items[0]);
                    args.RemoveAt(0);
                    HoldTurnDirectionEnum turnDir = HoldTurnDirectionEnum.RIGHT;
                    HoldLegLengthTypeEnum lengthType = HoldLegLengthTypeEnum.DEFAULT;
                    double legLength = -1;

                    if (items.Length > 1)
                    {
                        if (items[1].StartsWith("L"))
                        {
                            turnDir = HoldTurnDirectionEnum.LEFT;
                        }

                        if (items.Length > 2)
                        {
                            try
                            {
                                if (items[2].ToLower().EndsWith("nm"))
                                {
                                    items[2] = items[2].ToLower().Replace("nm", "");
                                    lengthType = HoldLegLengthTypeEnum.DISTANCE;
                                }
                                else
                                {
                                    lengthType = HoldLegLengthTypeEnum.TIME;
                                }

                                legLength = Convert.ToDouble(items[2]);
                            }
                            catch (FormatException)
                            {
                                lengthType = HoldLegLengthTypeEnum.DEFAULT;
                                legLength = -1;
                            }
                            catch (OverflowException)
                            {
                                lengthType = HoldLegLengthTypeEnum.DEFAULT;
                                legLength = -1;
                            }
                        }                        
                    }
                    if (!Aircraft.Fms.AddHold(new RouteWaypoint(wp), Bearing.FromDegrees(inbdCrs), turnDir, lengthType, legLength))
                    {
                        Logger?.Invoke($"ERROR - {wp.Identifier} not found in flight plan!");
                        return false;
                    }
                    string turnDirStr = turnDir == HoldTurnDirectionEnum.RIGHT ? "Right" : "Left";
                    string distanceStr = (lengthType == HoldLegLengthTypeEnum.DISTANCE) ? $", {legLength}nm legs" :
                        ((lengthType == HoldLegLengthTypeEnum.TIME) ? $", {legLength}min legs" : "");

                    Logger?.Invoke($"{Aircraft.Callsign} will hold at {wp.Identifier}, inbound course {inbdCrs:000}, {turnDirStr} turns{distanceStr}.");
                    return true;
                }
                catch (FormatException)
                {
                    return TryGetPublishedHold(wp, ref args);
                }
                catch (OverflowException)
                {
                    return TryGetPublishedHold(wp, ref args);
                }
            }
            else
            {
                return TryGetPublishedHold(wp, ref args);
            }
        }

        public bool TryGetPublishedHold(Fix wp, ref List<string> args)
        {
            PublishedHold pubHold = DataHandler.GetPublishedHold(wp.Identifier, wp.Location);

            if (pubHold == null)
            {
                Logger?.Invoke($"ERROR - No published hold found for waypoint {wp.Identifier}!");
                return false;
            }
            IRoutePoint holdPt = new RouteWaypoint(wp);

            if (!Aircraft.Fms.AddHold(holdPt, pubHold.InboundCourse, pubHold.TurnDirection, pubHold.LegLengthType, pubHold.LegLength))
            {
                Logger?.Invoke($"ERROR - {wp.Identifier} not found in flight plan!");
                return false;
            }

            Logger?.Invoke($"{Aircraft.Callsign} will hold at {wp.Identifier} as published.");
            return true;
        }
    }
}
