using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.Fixes.Waypoints
{
    /// <summary>
    /// Encodes whether or not the waypoint is essential to a route
    /// </summary>
    public enum WaypointRelevance
    {
        /// <summary>
        /// If the route's course changes at this waypoint, or it is an intersection between airways, it is considered essential.
        /// </summary>
        Essential,
        /// <summary>
        /// If a waypoint is considered non-essential for an enroute airway, but it is essential in a terminal procedure, it is considered transition-essential.
        /// </summary>
        TransitionEssential,
        /// <summary>
        /// Waypoints that are not essential or transition-essential
        /// </summary>
        NonEssential
    }

    /// <summary>
    /// Essentially, the use for this waypoint for a specific leg of a procedure.
    /// </summary>
    public class WaypointDescription
    {
        public WaypointRelevance Relevance { get; }

        public bool IsEndOfRoute { get; }

        public bool IsFlyOver { get; }

        public bool IsAtcCompulsory { get; }

        public bool IsHoldingFix { get; }


        public WaypointDescription() { }
    }
}
