using NavData_Interface.Objects.Fixes.Waypoints;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace NavData_Interface.DataSources.DFDUtility.Factory
{
    internal static class TerminalWaypointFactory
    {
        public static Waypoint Factory(SQLiteDataReader reader)
        {
            WaypointType type;

            try
            {
                type = SQLHelper.waypointTypeFromTypeString(reader["waypoint_type"].ToString(), true);
            }
            catch (FormatException ex)
            {
                // If we can't parse the waypoint type, we are fine with the default.
                type = new WaypointType();
            }

            var waypoint = new Waypoint(
                    reader["waypoint_identifier"].ToString(),
                    reader["waypoint_name"].ToString(),
                    SQLHelper.locationFromColumns(reader, "waypoint_latitude", "waypoint_longitude"),
                    reader["area_code"].ToString(),
                    reader["icao_code"].ToString(),
                    reader["region_code"].ToString(),
                    type
                );
            return waypoint;
        }
    }
}
