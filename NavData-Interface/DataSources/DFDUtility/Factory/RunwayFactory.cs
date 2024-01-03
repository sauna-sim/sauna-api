using AviationCalcUtilNet.GeoTools;
using NavData_Interface.Objects.Fixes;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace NavData_Interface.DataSources.DFDUtility.Factory
{
    internal static class RunwayFactory
    {
        internal static Runway Factory(SQLiteDataReader reader)
        {
            GeoPoint location = SQLHelper.locationFromColumns(reader, "runway_latitude", "runway_longitude");

            var airportIdentifier = reader["airport_identifier"].ToString();
            var runwayIdentifier = reader["runway_identifier"].ToString().TrimStart('R', 'W');
            var gradient = Double.Parse(reader["runway_gradient"].ToString());
            var magBearing = Double.Parse(reader["runway_magnetic_bearing"].ToString());
            var truBearing = Double.Parse(reader["runway_true_bearing"].ToString());
            var thrElevation = Int32.Parse(reader["landing_threshold_elevation"].ToString());
            var thrLength = Int32.Parse(reader["displaced_threshold_distance"].ToString());
            var thrCrossingHeight = Int32.Parse(reader["threshold_crossing_height"].ToString());
            var length = Int32.Parse(reader["runway_length"].ToString());
            var width = Int32.Parse(reader["runway_width"].ToString());

            return new Runway(
                runwayIdentifier,
                location,
                airportIdentifier,
                gradient,
                magBearing,
                truBearing,
                thrElevation,
                thrLength,
                thrCrossingHeight,
                length,
                width
                );
        }
    }
}
