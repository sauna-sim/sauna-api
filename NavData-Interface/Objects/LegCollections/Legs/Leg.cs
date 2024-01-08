using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Units;
using NavData_Interface.Objects.Fixes;
using NavData_Interface.Objects.Fixes.Navaids;
using NavData_Interface.Objects.Fixes.Waypoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.LegCollections.Legs
{
    public enum LegType
    {
        INITIAL_FIX,
        TRACK_TO_FIX,
        COURSE_TO_FIX,
        DIRECT_TO_FIX,
        FIX_TO_ALT,
        TRACK_TO_DISTANCE,
        TRACK_TO_DME,
        FIX_TO_MANUAL,
        COURSE_TO_ALT,
        COURSE_TO_DME,
        COURSE_TO_INTC,
        COURSE_TO_RADIAL,
        RADIUS_TO_FIX,
        ARC_TO_FIX,
        HEADING_TO_ALT,
        HEADING_TO_DME,
        HEADING_TO_INTC,
        HEADING_TO_MANUAL,
        HEADING_TO_RADIAL,
        PROCEDURE_TURN,
        HOLD_TO_ALT,
        HOLD_TO_FIX,
        HOLD_TO_MANUAL,
    }

    public enum SpeedRestrictionType
    {
        BELOW,
        AT,
        ABOVE
    }

    public enum RequiredTurnDirectionType
    {
        LEFT,
        RIGHT,
    }

    public class Leg
    {
        public LegType Type;

        public Velocity SpeedRestriction { get; }

        public SpeedRestrictionType? SpeedType { get; }

        public Length LowerAltitudeConstraint { get; }

        public Length HigherAltitudeConstraint { get; }

        public Fix EndPoint { get; }

        public VhfNavaid RecommendedNavaid { get; }

        public WaypointDescription EndPointDescription { get; }

        public bool IsFinalLeg => EndPointDescription.IsEndOfRoute;

        public Bearing OutboundMagneticCourse { get; }

        public RequiredTurnDirectionType? RequiredTurnDirection { get; }

        public Length ArcRadius { get; }

        public static LegType parseLegType(string legType)
        {
            switch (legType)
            {
                case "IF":
                    return LegType.INITIAL_FIX;
                case "TF":
                    return LegType.TRACK_TO_FIX;
                case "CF":
                    return LegType.COURSE_TO_FIX;
                case "DF":
                    return LegType.DIRECT_TO_FIX;
                case "FA":
                    return LegType.FIX_TO_ALT;
                case "FC":
                    return LegType.TRACK_TO_DISTANCE;
                case "FD":
                    return LegType.TRACK_TO_DME;
                case "FM":
                    return LegType.FIX_TO_MANUAL;
                case "CA":
                    return LegType.COURSE_TO_ALT;
                case "CD":
                    return LegType.COURSE_TO_DME;
                case "CI":
                    return LegType.COURSE_TO_INTC;
                case "CR":
                    return LegType.COURSE_TO_RADIAL;
                case "RF":
                    return LegType.RADIUS_TO_FIX;
                case "AF":
                    return LegType.ARC_TO_FIX;
                case "VA":
                    return LegType.HEADING_TO_ALT;
                case "VD":
                    return LegType.HEADING_TO_DME;
                case "VI":
                    return LegType.HEADING_TO_INTC;
                case "VM":
                    return LegType.HEADING_TO_MANUAL;
                case "VR":
                    return LegType.HEADING_TO_RADIAL;
                case "PI":
                    return LegType.PROCEDURE_TURN;
                case "HA":
                    return LegType.HOLD_TO_ALT;
                case "HF":
                    return LegType.HOLD_TO_FIX;
                case "MH":
                    return LegType.HOLD_TO_MANUAL;
            }

            return LegType.INITIAL_FIX;
        }
    }
}
