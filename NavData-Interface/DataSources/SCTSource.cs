using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using AviationCalcUtilNet.GeoTools;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fixes;
using NavData_Interface.Objects.Fixes.Navaids;
using NavData_Interface.Objects.Fixes.Waypoints;

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

                                GeoUtil.ConvertVrcToDecimalDegs(items[2], items[3], out double lat, out double lon);

                                if (sectionName == "VOR")
                                {
                                    _fixes.Add(new VhfNavaid(new GeoPoint(lat, lon), "", "", "", items[0], items[0], freq, "", null, 0, 0, 0, 0));
                                } else if (sectionName == "NDB")
                                {
                                    _fixes.Add(new Ndb(items[0], new GeoPoint(lat, lon), "", "", items[0], freq, 0));
                                } else if (sectionName == "AIRPORT")
                                {
                                    _fixes.Add(new Airport(items[0], new GeoPoint(lat, lon), "", "", "", items[0], false, RunwaySurfaceCode.Undefined, 0, 0, 0, 0, 0, ""));
                                }
                            }
                            break;
                        case "FIXES":
                            items = line.Split(' ');

                            if (items.Length >= 3)
                            {
                                GeoUtil.ConvertVrcToDecimalDegs(items[1], items[2], out double lat, out double lon);
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
        public override Airport GetClosestAirportWithinRadius(GeoPoint position, double radiusM)
        {
            Airport closestAirport = null;
            double closestDistance = double.MaxValue;
            foreach (var fix in _fixes)
            {
                if (fix.GetType() != typeof(Airport))
                {
                    continue;
                }
                double distanceM = GeoPoint.DistanceM(position, fix.Location);

                if (distanceM > radiusM) { continue; }

                if (distanceM < closestDistance)
                {
                    closestDistance = distanceM;
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
    }
}
