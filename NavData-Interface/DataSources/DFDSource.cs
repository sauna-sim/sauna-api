using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using NavData_Interface.Objects.Fixes;
using AviationCalcUtilNet.GeoTools;
using NavData_Interface.Objects.Fixes.Navaids;
using NavData_Interface.DataSources.DFDUtility;
using NavData_Interface.DataSources.DFDUtility.Factory;
using NavData_Interface.Objects;
using AviationCalcUtilNet.MathTools;
using NavData_Interface.Objects.Fixes.Waypoints;

namespace NavData_Interface.DataSources
{
    public class DFDSource : DataSource
    {
        private SQLiteConnection _connection;

        public string Airac_version { get; }

        public override string GetId()
        {
            return Airac_version;
        }

        /// <summary>
        /// Creates a new DFD data source.
        /// </summary>
        /// <param name="filePath">The path to the DFD file.</param>
        /// <exception cref="System.IO.FileNotFoundException">If the provided file wasn't found</exception>
        public DFDSource(string filePath)
        {
            var connectionString = new SQLiteConnectionStringBuilder()
            {
                DataSource = filePath,
                Version = 3,
                ReadOnly = true
            }.ToString();

            _connection = new SQLiteConnection(connectionString);
            
            try
            {
                _connection.Open();
            } catch (SQLiteException e) { 
                if (e.ResultCode == SQLiteErrorCode.CantOpen)
                {
                    throw new System.IO.FileNotFoundException(filePath);
                }
            }

            try
            {
                var cmd = new SQLiteCommand(_connection)
                {
                    CommandText = "SELECT * FROM tbl_header"
                };

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Airac_version = reader["current_airac"].ToString();
                    } else
                    {
                        throw new FormatException("The navigation data file has an incorrect format.");
                    }
                }
            } catch (Exception e)
            {
                throw new Exception("The navigation data file is invalid.", e);
            }
        }

        override
        public Localizer GetLocalizerFromAirportRunway(string airportIdentifier, string runwayIdentifier)
        {
            var foundLocs = GetObjectsWithQuery<Localizer>(LocalizerLookupByAirportRunway(airportIdentifier, runwayIdentifier), reader => LocalizerFactory.Factory(reader));

            if (foundLocs.Count < 1)
            {
                return null;
            }
            
            return foundLocs[0];
        }

        private SQLiteCommand LocalizerLookupByAirportRunway(string airportIdentifier, string runwayIdentifier)
        {
            runwayIdentifier = "RW" + runwayIdentifier;
            
            var cmd = new SQLiteCommand(_connection)
            {
                CommandText = $"SELECT * from tbl_localizers_glideslopes WHERE airport_identifier = @airportIdentifier AND runway_identifier = @runwayIdentifier"
            };

            cmd.Parameters.AddWithValue("@airportIdentifier", airportIdentifier);
            cmd.Parameters.AddWithValue("@runwayIdentifier", runwayIdentifier);

            return cmd;
        }

        private SQLiteCommand AirportLookupByIdentifier(string identifier)
        {
            var cmd = new SQLiteCommand(_connection)
            {
                CommandText = $"SELECT * FROM tbl_airports WHERE airport_identifier = @identifier"
            };

            cmd.Parameters.AddWithValue("@identifier", identifier);

            return cmd;
        }

        public Airport GetAirportByIdentifier(string identifier)
        {
            var airports = GetObjectsWithQuery<Airport>(AirportLookupByIdentifier(identifier), reader => AirportFactory.Factory(reader));

            if (airports.Count == 1)
            {
                return airports[0];
            } else if (airports.Count > 1)
            {
                Console.Error.WriteLine($"Found two airport results for {identifier}. This should never happen!");
            }

            return null;
        }

        private SQLiteCommand WaypointLookupByIdentifier(bool isTerminal, string identifier)
        {
            var table = isTerminal ? "tbl_terminal_waypoints" : "tbl_enroute_waypoints";

            var cmd = new SQLiteCommand(_connection)
            {
                CommandText = $"SELECT * FROM {table} WHERE waypoint_identifier = @identifier"
            };

            cmd.Parameters.AddWithValue("@identifier", identifier);

            return cmd;
        }

        public List<Waypoint> GetWaypointsByIdentifier(string identifier)
        {
            // We need to combine enroute + terminal waypoints
            
            List<Waypoint> waypoints = GetObjectsWithQuery<Waypoint>(WaypointLookupByIdentifier(true, identifier), reader => WaypointFactory.Factory(reader));
            foreach (var waypoint in GetObjectsWithQuery<Waypoint>(WaypointLookupByIdentifier(false, identifier), reader => WaypointFactory.Factory(reader)))
            {
                waypoints.Add(waypoint);
            }

            return waypoints;
        }

        private SQLiteCommand VhfNavaidLookupByIdentifier(string identifier)
        {
            var cmd = new SQLiteCommand(_connection)
            {
                CommandText = $"SELECT * from tbl_vhfnavaids WHERE vor_identifier = @identifier OR dme_ident = @identifier"
            };
            cmd.Parameters.AddWithValue("@identifier", identifier);

            return cmd;
        }

        public List<VhfNavaid> GetVhfNavaidsByIdentifier(string identifier)
        {
            List<VhfNavaid> navaids = GetObjectsWithQuery<VhfNavaid>(
                VhfNavaidLookupByIdentifier(identifier), 
                reader => VhfNavaidFactory.Factory(reader));

            return navaids;
        }

        public SQLiteCommand NdbLookupByIdentifier(bool isTerminal, string identifier)
        {
            var table = isTerminal ? "tbl_terminal_ndbnavaids" : "tbl_enroute_ndbnavaids";

            var cmd = new SQLiteCommand(_connection)
            {
                CommandText = $"SELECT * FROM {table} WHERE ndb_identifier = @identifier"
            };

            cmd.Parameters.AddWithValue("@identifier", identifier);

            return cmd;
        }

        public SQLiteCommand AirportsFilterByDistance(GeoPoint position, double radiusM)
        {
            radiusM = Math.Min(radiusM, MathUtil.ConvertNauticalMilesToMeters(100));

            // Figure out where our extremities are
            double leftLon, rightLon, bottomLat, topLat;
            {
                var leftPoint = new GeoPoint(position);
                leftPoint.MoveByM(270.0, radiusM);
                leftLon = leftPoint.Lon;

                var rightPoint = new GeoPoint(position);
                rightPoint.MoveByM(90.0, radiusM);
                rightLon = rightPoint.Lon;

                var bottomPoint = new GeoPoint(position);
                bottomPoint.MoveByM(180.0, radiusM);
                bottomLat = bottomPoint.Lat;

                var topPoint = new GeoPoint(position);
                topPoint.MoveByM(360.0, radiusM);
                topLat = topPoint.Lat;
            }

            double maxLat = MathUtil.ConvertRadiansToDegrees(Math.Acos(radiusM / (GeoUtil.EARTH_RADIUS_M * Math.PI)));
            if (topLat > maxLat)
            {
                leftLon = -180;
                rightLon = 180;
                topLat = 90;
            }
            else if (bottomLat < -maxLat)
            {
                leftLon = -180;
                rightLon = 180;
                bottomLat = -90;
            }

            if (rightLon <= leftLon)
            {
                // SELECT * from (airports) WHERE ((latitude) BETWEEN bottomlat AND topLAT) AND (longitude >= leftlon OR longitude <= right)
                var cmd = new SQLiteCommand(_connection)
                {
                    CommandText = $"SELECT * FROM tbl_airports WHERE (airport_ref_latitude BETWEEN @bottomLat AND @topLat) AND (airport_ref_longitude >= @leftLon OR airport_ref_longitude <= @rightLon)"
                };

                cmd.Parameters.AddWithValue("@bottomLat", bottomLat);
                cmd.Parameters.AddWithValue("@topLat", topLat);
                cmd.Parameters.AddWithValue("@leftLon", leftLon);
                cmd.Parameters.AddWithValue("@rightLon", rightLon);

                return cmd;
            }
            else
            {
                // SELECT * FROM (airports) WHERE ((latitude) BETWEEN bottomlat AND topLAT) AND (longitude) BETWEEN leftlon AND rightlon)
                var cmd = new SQLiteCommand(_connection)
                {
                    CommandText = $"SELECT * FROM tbl_airports WHERE (airport_ref_latitude BETWEEN @bottomLat AND @topLat) AND (airport_ref_longitude BETWEEN @leftLon AND @rightLon)"
                };

                cmd.Parameters.AddWithValue("@bottomLat", bottomLat);
                cmd.Parameters.AddWithValue("@topLat", topLat);
                cmd.Parameters.AddWithValue("@leftLon", leftLon);
                cmd.Parameters.AddWithValue("@rightLon", rightLon);

                return cmd;
            }
        }

        public List<Ndb> GetNdbsByIdentifier(string identifier)
        {
            // We need to combine enroute + terminal NDBs

            List<Ndb> ndbs = GetObjectsWithQuery<Ndb>(NdbLookupByIdentifier(true, identifier), reader => NdbFactory.Factory(reader));
            foreach (var ndb in GetObjectsWithQuery<Ndb>(NdbLookupByIdentifier(false, identifier), reader => NdbFactory.Factory(reader)))
            {
                ndbs.Add(ndb);
            }

            return ndbs;
        }

        public List<Navaid> GetNavaidsByIdentifier(string identifier)
        {
            // We get all VHF Navaids + all NDBs with this ident

            var navaids = new List<Navaid>();

            foreach (var vhfNavaid in GetVhfNavaidsByIdentifier(identifier))
            {
                navaids.Add(vhfNavaid);
            }

            foreach (var ndbNavaid in GetNdbsByIdentifier(identifier))
            {
                navaids.Add(ndbNavaid);
            }

            return navaids;
        }

        internal List<T> GetObjectsWithQuery<T>(SQLiteCommand cmd, Func<SQLiteDataReader, T> objectFactory)
        {
            var objects = new List<T>();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var obj = objectFactory(reader);
                    objects.Add(obj);
                }
            }

            return objects;
        }

        public override List<Fix> GetFixesByIdentifier(string identifier)
        {
            List<Fix> foundFixes = new List<Fix>();

            foreach (var waypoint in this.GetWaypointsByIdentifier(identifier))
            {
                foundFixes.Add(waypoint);
            }

            foreach (var navaid in this.GetNavaidsByIdentifier(identifier))
            {
                foundFixes.Add(navaid);
            }

            var airport = GetAirportByIdentifier(identifier);

            if (airport != null)
            {
                foundFixes.Add(airport);
            }

            return foundFixes;
        }

        /// <summary>
        /// Gets the closest airport within a square.
        /// </summary>
        /// <param name="position">The centre of the square</param>
        /// <param name="radiusM">The distance from the centre to each side of the square</param>
        /// <returns>The closest airport within the square, or null if none found</returns>
        public override Airport GetClosestAirportWithinRadius(GeoPoint position, double radiusM)
        {
            List<Airport> airports = GetObjectsWithQuery<Airport>(AirportsFilterByDistance(position, radiusM), reader => AirportFactory.Factory(reader));

            Airport closestAirport = null;
            double bestDistance = double.MaxValue;
            foreach (var airport in airports)
            {
                var distance = GeoPoint.Distance(airport.Location, position).Meters;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    closestAirport = airport;
                }
            }
            return closestAirport;
        }

        private SQLiteCommand RunwayLookupByAirportIdentifier(string airportIdentifier, string runwayIdentifier)
        {
            SQLiteCommand command = new SQLiteCommand();

            command.CommandText = $"SELECT * FROM tbl_runways WHERE airport_identifier == @airport AND runway_identifier == RW@runway";

            command.Parameters.AddWithValue("@airport", airportIdentifier);
            command.Parameters.AddWithValue("@runway", runwayIdentifier);

            return command;
        }

        public override Runway GetRunwayFromAirportRunwayIdentifier(string airportIdentifier, string runwayIdentifier)
        {
            List<Runway> runways = GetObjectsWithQuery<Runway>(
                RunwayLookupByAirportIdentifier(airportIdentifier, runwayIdentifier),
                reader => RunwayFactory.Factory(reader));

            if (runways.Count == 0)
            {
                return null;
            }

            return runways[0];
        }
    }
}