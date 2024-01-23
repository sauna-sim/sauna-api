using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Units;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fixes;
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
        public static Sid Factory(SQLiteDataReader reader, SQLiteConnection connection)
        {
            var runwayTransitions = new List<Transition>();
            var commonLegs = new List<Leg>();
            var transitions = new List<Transition>();

            Length transitionAltitude = null;

            // if you're here cause there's an infinite loop in this function
            // try reader.read() instead of HasRows
            // idk documentation is kinda unclear
            reader.Read();

            var airportIdentifier = reader["airport_identifier"].ToString();
            var routeIdentifier = reader["procedure_identifier"].ToString();

            while (reader.HasRows)
            {
                switch (reader["route_type"].ToString())
                {
                    case "4":
                        // This leg is for a runway transition. Read the whole thing
                        {
                            var transitionIdentifier = reader["transition_identifier"].ToString();
                            var transition = ReadTransition(reader, connection, transitionIdentifier);
                            runwayTransitions.Add(transition);
                            continue;
                        }
                    case "5":
                        // This leg is for the common portion. Read just this leg and add it
                        if (runwayTransitions.Count == 0 && reader["transition_altitude"].ToString() != "")
                        {
                            // When there are no runway transitions, the first leg of the common part will have
                            // A non-null transition_altitude, which is the transition altitude for this SID.
                            transitionAltitude = Length.FromFeet(Int32.Parse(reader["transition_altitude"].ToString()));
                        }
                        commonLegs.Add(ReadLeg(reader, connection));
                        reader.Read();
                        continue;
                    case "6":
                        {
                            var transitionIdentifier = reader["transition_identifier"].ToString();
                            var transition = ReadTransition(reader, connection, transitionIdentifier);
                            transitions.Add(transition);
                            continue;
                        }

                    // The following are possible entries
                    // They correspond to rwyTransition, common route and transition
                    // For other types of SIDs
                    // We handle them the same way
                    case "1":
                        goto case "4";
                    case "2":
                        goto case "5";
                    case "3":
                        goto case "6";
                    case "F":
                        goto case "4";
                    case "M":
                        goto case "5";
                    case "S":
                        goto case "6";
                    case "T":
                        goto case "4";
                    case "V":
                        goto case "6";

                }
            }

            return new Sid(airportIdentifier, routeIdentifier, runwayTransitions, commonLegs, transitions, transitionAltitude);
        }

        private static Transition ReadTransition(SQLiteDataReader reader, SQLiteConnection connection, string transitionIdentifier)
        {
            // The transition altitude for this SID is always on the first leg of each transition. Store it now
            var transitionAltitude = Length.FromFeet(Double.Parse(reader["transition_altitude"].ToString()));

            var legs = new List<Leg>();

            while (reader.Read() && reader["transition_identifier"].ToString() == transitionIdentifier)
            {
                legs.Add(ReadLeg(reader, connection));
            }

            return new Transition(legs, transitionIdentifier, transitionAltitude);
        }

        private static Leg ReadLeg(SQLiteDataReader reader, SQLiteConnection connection)
        {
            var waypointIdentifier = reader["waypoint_identifier"].ToString();

            Waypoint waypoint = null;
            WaypointDescription waypointDescription = null;

            if (waypointIdentifier != "")
            {
                waypoint = new Waypoint(waypointIdentifier, SQLHelper.locationFromColumns(reader));
                waypointDescription = SQLHelper.waypointDescriptionFromDescriptionString(reader["waypoint_description_code"].ToString());
            }

            RequiredTurnDirectionType? turnDirection = null;

            if (reader["turn_direction"] != null)
            {
                // The turn is more than 90 degrees. Possibilities are L, R and E
                // E just means either direction is OK. In that case we keep turnDirection null.
                if (reader["turn_direction"].ToString() == "L")
                {
                    turnDirection = RequiredTurnDirectionType.LEFT;
                } else if (reader["turn_direction"].ToString() == "R") 
                {
                    turnDirection = RequiredTurnDirectionType.RIGHT;
                }
            }

            var legType = Leg.parseLegType(reader["path_termination"].ToString());

            Navaid recommendedNavaid = null;
            Bearing theta = null;

            if (reader["recommanded_navaid"].ToString() != "")
            {
                try
                {
                    // Get the recommended navaid from the DB
                    // The id is of the format tablename|id
                    var fullId = reader["recommanded_id"].ToString().Split('|');

                    var table = fullId[0];
                    var id = fullId[1];

                    var cmd = new SQLiteCommand(connection)
                    {
                        CommandText = $"SELECT * FROM {table} WHERE id = @id"
                    };

                    cmd.Parameters.AddWithValue("@id", id);

                    // Depending on the table, we need to choose the right factory
                    if (table == "tbl_vhfnavaids")
                    {
                        recommendedNavaid = VhfNavaidFactory.Factory(cmd.ExecuteReader());
                    }
                    else if (table == "tbl_terminal_ndbnavaids")
                    {
                        recommendedNavaid = TerminalNdbFactory.Factory(cmd.ExecuteReader());
                    }
                    else if (table == "tbl_enroute_ndbnavaids")
                    {
                        recommendedNavaid = NdbFactory.Factory(cmd.ExecuteReader());
                    }
                } catch (Exception e)
                {
                    // Probably doesn't have ids.
                    var navaidIdentifier = reader["recommanded_navaid"].ToString();
                    GeoPoint location = SQLHelper.locationFromColumns(reader, "recommanded_navaid_latitude", "recommanded_navaid_longitude");

                    recommendedNavaid = new VhfNavaid(location, "", "", "", navaidIdentifier, navaidIdentifier, 199.998, "", null, Length.FromNauticalMiles(200));
                } finally
                {
                    theta = Bearing.FromDegrees(Double.Parse(reader["theta"].ToString()));
                }
            }

            var arcRadiusRaw = reader["arc_radius"]?.ToString();

            Length arcRadius = null;

            if (arcRadiusRaw != "")
            {
                arcRadius = Length.FromFeet(Int32.Parse(arcRadiusRaw));
            }

            var magneticCourseRaw = reader["magnetic_course"].ToString();

            Bearing outBoundMagneticCourse = null;

            if (magneticCourseRaw != "")
            {
                outBoundMagneticCourse = Bearing.FromDegrees(Double.Parse(magneticCourseRaw));
            }

            var altRestrictionType = reader["altitude_description"]?.ToString();

            Length upperAlt = null;
            Length lowerAlt = null;

            int altitude1 = 0, altitude2 = 0;

            if (reader["altitude1"].ToString() != "")
            {

                if (reader["altitude1"].ToString().StartsWith("FL") == true)
                {
                    altitude1 = 100 * Int32.Parse(reader["altitude1"].ToString().Substring(2, 3));
                }
                else
                {
                    altitude1 = Int32.Parse(reader["altitude1"].ToString());
                }
            }

            if (reader["altitude2"].ToString() != "")
            {
                
                if (reader["altitude2"].ToString().StartsWith("FL") == true)
                {
                    altitude2 = 100 * Int32.Parse(reader["altitude2"].ToString().Substring(2, 3));
                } else
                {
                    altitude2 = Int32.Parse(reader["altitude2"].ToString());
                }
            }

            if (altRestrictionType == "+")
            {
                // At or above. The altitude given is the lower restriction.
                lowerAlt = Length.FromFeet(altitude1);
            } else if (altRestrictionType == "-")
            {
                // At or below. The altitude given is the upper restriction.
                upperAlt = Length.FromFeet(altitude1);
            } else if (altRestrictionType == null && reader["altitude1"] != null)
            {
                // If the restrictionType is null, and we have an altitude, it's an 'at' restriction
                upperAlt = Length.FromFeet(altitude1);
                lowerAlt = Length.FromFeet(altitude1);
            } else if (altRestrictionType == "B")
            {
                // 'Between' restriction. Altitude1 is always the upper and Altitude2 is always the lower.
                upperAlt = Length.FromFeet(altitude1);
                lowerAlt = Length.FromFeet(altitude2);
            } else if (altRestrictionType == "C")
            {
                // 'Termination at or above'. Altitude2 has the lower altitude. Used for 'X to altitude' legs.
                // Seemingly never actually used in any procedure??? We'll handle it anyway.
                upperAlt = Length.FromFeet(altitude2);
            } 

            SpeedRestrictionType? speedRestrictionType = null;
            var speedRestrictionTypeRaw = reader["speed_limit_description"].ToString();

            Velocity speedLimit = null;

            if (reader["speed_limit"].ToString() != "")
            {
                speedLimit = Velocity.FromKnots(Int32.Parse(reader["speed_limit"].ToString()));
            }

            if (speedRestrictionTypeRaw == "" && reader["speed_limit"].ToString() != "")
            {
                speedRestrictionType = SpeedRestrictionType.AT;
            } else if (speedRestrictionTypeRaw == "+")
            {
                speedRestrictionType = SpeedRestrictionType.ABOVE;
            } else if (speedRestrictionTypeRaw == "-")
            {
                speedRestrictionType = SpeedRestrictionType.BELOW;
            }

            Fix centerFix = null;

            var centerWaypointIdent = reader["center_waypoint"].ToString();
            
            if (centerWaypointIdent != "")
            {
                GeoPoint centerWaypointLocation = SQLHelper.locationFromColumns(reader, "center_waypoint_latitude", "center_waypoint_longitude");

                Waypoint centerPoint = new Waypoint(centerWaypointIdent, centerWaypointLocation);
            }

            double? legLength = null;
            HoldLegLengthTypeEnum? legLengthType = null;

            var legLengthRaw = reader["route_distance_holding_distance_time"].ToString();
            var legLengthTypeRaw = reader["distance_time"].ToString();


            if (legLengthRaw != "")
            {
                legLength = Double.Parse(legLengthRaw);
                
                if (legLengthTypeRaw == "D")
                {
                    legLengthType = HoldLegLengthTypeEnum.DISTANCE;
                } else
                {
                    legLengthType = HoldLegLengthTypeEnum.TIME;
                }
            }

            return new Leg(
                legType,
                speedLimit,
                speedRestrictionType,
                lowerAlt,
                upperAlt,
                waypoint,
                waypointDescription,
                centerFix,
                recommendedNavaid,
                theta,
                outBoundMagneticCourse,
                turnDirection,
                arcRadius,
                legLengthType,
                legLength
                );
        }
    }
}
