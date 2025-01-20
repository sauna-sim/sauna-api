using NavData_Interface.Objects.Fixes;
using NavData_Interface.Objects.Fixes.Waypoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.LegCollections.Airways
{
    internal class AirwayPoint
    {
        internal Waypoint Point { get; }

        internal WaypointDescription Description { get; }

        internal AirwayPoint(Waypoint point, WaypointDescription description)
        {
            Point = point;
            Description = description;
        }
    }
}
