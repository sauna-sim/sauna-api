using AviationCalcUtilNet.GeoTools;
using NavData_Interface.Objects.Fixes;
using NavData_Interface.Objects.Fixes.Waypoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.LegCollections.Legs
{
    public enum LegType
    {
        COURSE_TO_ALT,
        COURSE_TO_FIX,
        DIRECT_TO_FIX,
        FIX_TO_ALT,
        FIX_TO_MANUAL,
        HOLD_TO_ALT,
        HOLD_TO_FIX,
        HOLD_TO_MANUAL,
        INITIAL_FIX,
        TRACK_TO_FIX,
        RADIUS_TO_FIX,
        HEADING_TO_ALT,
        HEADING_TO_INTC,
        HEADING_TO_DME,
        HEADING_TO_MANUAL,
        DISCO
    }

    public enum SpeedRestrictionType
    {
        BELOW,
        AT,
        ABOVE
    }

    public class Leg
    {
        public LegType Type;

        public int? SpeedRestriction { get; }

        public SpeedRestrictionType? SpeedType { get; }

        public int? LowerAltitudeConstraint { get; }

        public int? HigherAltitudeConstraint { get; }

        public Fix StartPoint { get; }

        public Fix EndPoint { get; }

        public WaypointDescription StartPointDescription { get; }

        public WaypointDescription EndPointDescription { get; }

        public bool IsFinalLeg => EndPointDescription.IsEndOfRoute;

        public double? OutboundMagneticCourse { get; }

        public double? InboundMagneticCourse { get; }

        public Leg(
        LegType type,
        int? speedRestriction,
        SpeedRestrictionType? speedType,
        int? lowerAltitudeConstraint,
        int? higherAltitudeConstraint,
        Fix startPoint,
        Fix endPoint,
        WaypointDescription startPointDescription,
        WaypointDescription endPointDescription,
        double? outboundMagneticCourse,
        double? inboundMagneticCourse)
        {
            Type = type;
            SpeedRestriction = speedRestriction;
            SpeedType = speedType;
            LowerAltitudeConstraint = lowerAltitudeConstraint;
            HigherAltitudeConstraint = higherAltitudeConstraint;
            StartPoint = startPoint;
            EndPoint = endPoint;
            StartPointDescription = startPointDescription;
            EndPointDescription = endPointDescription;
            OutboundMagneticCourse = outboundMagneticCourse;
            InboundMagneticCourse = inboundMagneticCourse;
        }

        public Leg(
        LegType type,
        Fix startPoint,
        Fix endPoint,
        WaypointDescription startPointDescription,
        WaypointDescription endPointDescription)
        : this(type, null, null, null, null, startPoint, endPoint, startPointDescription, endPointDescription, null, null)
        {
        }
    }
}
