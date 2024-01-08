using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Units;
using NavData_Interface.Objects.Fixes.Navaids;
using NavData_Interface.Objects.Fixes.Waypoints;
using NavData_Interface.Objects.LegCollections.Legs;
using NavData_Interface.Objects.LegCollections.Procedures;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace NavData_Interface.DataSources.DFDUtility.Factory
{
    internal class SidFactory
    {
        public Sid Factory(SQLiteDataReader reader, SQLiteConnection connection)
        {
            var runwayTransitions = new List<Transition>();
            var commonLegs = new List<Leg>();
            var transitions = new List<Transition>();

            while (reader.HasRows)
            {
                switch (reader["route_type"].ToString())
                {
                    case "4":
                        var transitionIdentifier = reader["transition_identifier"].ToString();
                        var transition = ReadTransition(reader, connection, transitionIdentifier);
                        runwayTransitions.Add(transition);
                        continue;
                    case "5":  
                        
                }

            }
        }

        private Transition ReadTransition(SQLiteDataReader reader, SQLiteConnection connection, string transitionIdentifier)
        {
            var transitionAltitude = Length.FromFeet(Double.Parse(reader["transition_altitude"].ToString()));
            var legs = new List<Leg>();



        }

        private Leg ReadLeg(SQLiteDataReader reader, SQLiteConnection connection)
        {
            var waypointIdentifier = reader["waypoint_identifier"]?.ToString();

            Waypoint waypoint = null;
            WaypointDescription waypointDescription = null;

            if (waypointIdentifier != null)
            {
                waypoint = new Waypoint(waypointIdentifier, SQLHelper.locationFromColumns(reader));
                waypointDescription = SQLHelper.waypointDescriptionFromDescriptionString(reader["waypoint_description_code"].ToString());
            }

            RequiredTurnDirectionType? turnDirection = null;

            if (reader["turn_direction"] != null)
            {
                if (reader["turn_direction"].ToString() == "L")
                {
                    turnDirection = RequiredTurnDirectionType.LEFT;
                } else if (reader["turn_direction"].ToString() == "R") 
                {
                    turnDirection = RequiredTurnDirectionType.RIGHT;
                }
            }

            var legType = Leg.parseLegType(reader["path_termination"].ToString());

            Navaid recommendedNavaid;

            if (reader["recommended_navaid"].ToString() != null)
            {
                var cmd = new SQLiteCommand(connection)
                {
                    CommandText = $"SELECT * FROM @table WHERE id = @id"
                };

                var table = reader["recommended_id"].ToString().Split('|')[0];

                cmd.Parameters.AddWithValue("table", table);
                cmd.Parameters.AddWithValue("id", reader["recommended_id"].ToString());

                if (table == "tbl_vhfnavaids")
                {
                    recommendedNavaid = VhfNavaidFactory.Factory(cmd.ExecuteReader());
                } else if (table == "tbl_terminal_ndbnavaids")
                {
                    recommendedNavaid = TerminalNdbFactory.Factory(cmd.ExecuteReader());
                } else if (table == "tbl_enroute_ndbnavaids")
                {
                    recommendedNavaid = NdbFactory.Factory(cmd.ExecuteReader());
                }
            }

            var arcRadiusRaw = reader["arc_radius"]?.ToString();

            Length arcRadius = null;

            if (arcRadiusRaw != null)
            {
                arcRadius = Length.FromFeet(Int32.Parse(arcRadiusRaw));
            }

            var magneticCourseRaw = reader["magnetic_course"]?.ToString();

            Bearing outBoundMagneticCourse = null;

            if (magneticCourseRaw != null)
            {
                outBoundMagneticCourse = Bearing.FromDegrees(Double.Parse(magneticCourseRaw));
            }


        }
    }
}
