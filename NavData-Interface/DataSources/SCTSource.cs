using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Units;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fixes;
using NavData_Interface.Objects.Fixes.Navaids;
using NavData_Interface.Objects.Fixes.Waypoints;
using NavData_Interface.Objects.LegCollections.Airways;

namespace NavData_Interface.DataSources
{
    public class SCTSource : DataSource
    {
        private List<Fix> _fixes;
        private object _fixesLock = new object();

        public string FileName {get; private set; }

        public override string GetId()
        {
            return FileName;
        }

        public SCTSource(string filename)
        {
            _fixes = new List<Fix>();

            // Read file lines
            string[] filelines = System.IO.File.ReadAllLines(filename);

            string sectionName = "";

            // Loop through sector file
            foreach (string line in filelines)
            {
                // Ignore comments
                if (line.Trim().StartsWith(";"))
                {
                    continue;
                }

                if (line.StartsWith("["))
                {
                    // Get section name
                    sectionName = line.Replace("[", "").Replace("]", "").Trim();
                }
                else
                {
                    string[] items;
                    switch (sectionName)
                    {
                        case "VOR":
                        case "NDB":
                        case "AIRPORT":
                            items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if (items.Length >= 4)
                            {
                                double freq = 0;
                                try
                                {
                                    freq = Convert.ToDouble(items[1]);
                                }
                                catch (Exception) { }

                                var lat = Latitude.FromVrc(items[2]);
                                var lon = Longitude.FromVrc(items[3]);

                                if (sectionName == "VOR")
                                {
                                    _fixes.Add(new VhfNavaid(new GeoPoint(lat, lon), "", "", "", items[0], items[0], freq, "", null, null));
                                } else if (sectionName == "NDB")
                                {
                                    _fixes.Add(new Ndb(items[0], new GeoPoint(lat, lon), "", "", items[0], freq, new Length(0)));
                                } else if (sectionName == "AIRPORT")
                                {
                                    _fixes.Add(new Airport(items[0], new GeoPoint(lat, lon), "", "", "", items[0], false, RunwaySurfaceCode.Undefined, null, null, null, null, null, ""));
                                }
                            }
                            break;
                        case "FIXES":
                            items = line.Split(' ');

                            if (items.Length >= 3)
                            {
                                var lat = Latitude.FromVrc(items[1]);
                                var lon = Longitude.FromVrc(items[2]);
                                _fixes.Add(new Waypoint(items[0], items[0], new GeoPoint(lat, lon)));
                            }
                            break;
                    }
                }
            }

            FileName = filename;
        }

        public override List<Fix> GetFixesByIdentifier(string identifier)
        {
            List<Fix> foundFixes = new List<Fix>();

            lock (_fixesLock)
            {
                foreach (var fix in _fixes)
                {
                    if (fix.Identifier == identifier)
                    {
                        foundFixes.Add(fix);
                    }
                }
            }

            return foundFixes;
        }

        public override Localizer GetLocalizerFromAirportRunway(string airportIdentifier, string runwayIdentifier)
        {
            return null;
        }

        /// <summary>
        /// Gets the closest airport within a defined radius
        /// </summary>
        /// <param name="position">The centre point</param>
        /// <param name="radiusM">The radius in which to search for airports</param>
        /// <returns>The closest airport within the radius specified, or null if none found</returns>
        public override Airport GetClosestAirportWithinRadius(GeoPoint position, Length radius)
        {
            Airport closestAirport = null;
            Length closestDistance = new Length(double.MaxValue);
            foreach (var fix in _fixes)
            {
                if (fix.GetType() != typeof(Airport))
                {
                    continue;
                }
                Length distance = GeoPoint.Distance(position, fix.Location);

                if (distance > radius) { continue; }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestAirport = (Airport)fix;
                }
            }
            return closestAirport;
        }

        public override Runway GetRunwayFromAirportRunwayIdentifier(string airportIdentifier, string runwayIdentifier)
        {
            // TODO: !!
            return null;
        }

        public override Airway GetAirwayFromIdentifierAndFixes(string airwayIdentifier, Fix startFix, Fix endFix)
        {
            return null;
        }

        public override Airport GetAirportByIdentifier(string airportIdentifier)
        {
            foreach (var fix in _fixes)
            {
                if (fix is Airport && fix.Identifier == airportIdentifier)
                {
                    return (Airport)fix;
                }
            }

            return null;
        }
    }
}
