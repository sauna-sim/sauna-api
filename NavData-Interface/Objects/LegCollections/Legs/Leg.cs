using AviationCalcUtilNet.GeoTools;
using NavData_Interface.Objects.Fixes;
using NavData_Interface.Objects.Fixes.Waypoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.LegCollections.Legs
{
    public class Leg
    {
        public Fix StartPoint { get; }

        public WaypointDescription StartPointdescription { get; }

        public Fix EndPoint { get; }

    }
}
