using AviationCalcUtilNet.GeoTools;
using NavData_Interface.Objects.Fixes;
using NavData_Interface.Objects;
using System;
using System.Collections.Generic;
using NavData_Interface.Objects.LegCollections.Airways;
using AviationCalcUtilNet.Units;

namespace NavData_Interface.DataSources
{
    public class InMemorySource : DataSource
    {
        private List<Fix> _fixes;
        private List<Localizer> _locs;
        private List<PublishedHold> _pubHolds;
        private List<Airport> _airports;

        private string _id;

        public override string GetId()
        {
            return _id;
        }

        public InMemorySource(string id)
        {
            _fixes = new List<Fix>();
            _locs = new List<Localizer>();
            _pubHolds = new List<PublishedHold>();
            _airports = new List<Airport>();

            _id = id;
        }

        public void AddFix(Fix fix)
        {
            _fixes.Add(fix);
        }

        public void AddLocalizer(Localizer loc)
        {
            _locs.Add(loc);
        }

        public void AddPublishedHold(PublishedHold hold)
        {
            _pubHolds.Add(hold);
        }

        public void AddAirport(Airport airport)
        {
            _airports.Add(airport);
        }


        public Airport GetAirportByIdentifier(string identifier)
        {
            foreach (Airport airport in _airports)
            {
                if (airport.Identifier.ToUpper() == identifier.ToUpper()) return airport;
            }
            return null;
        }
        public override List<Fix> GetFixesByIdentifier(string identifier)
        {
            List<Fix> retFixes = new List<Fix>();

            foreach (Fix fix in _fixes)
            {
                if (fix.Identifier.ToUpper() == identifier.ToUpper())
                {
                    retFixes.Add(fix);
                }
            }
            return retFixes;
        }

        public override Localizer GetLocalizerFromAirportRunway(string airportIdentifier, string runwayIdentifier)
        {
            foreach (Localizer loc in _locs)
            {
                if (loc.Airport_identifier.ToUpper() == airportIdentifier.ToUpper() && loc.Runway_identifier.ToUpper() == runwayIdentifier.ToUpper())
                {
                    return loc;
                }
            }
            return null;
        }

        public PublishedHold GetPublishedHold(Fix fix)
        {
            foreach (PublishedHold hold in _pubHolds)
            {
                if (hold.Waypoint.Identifier.ToUpper() == fix.Identifier.ToUpper() && GeoPoint.Distance(hold.Waypoint.Location, fix.Location) < Length.FromMeters(1000))
                {
                    return hold;
                }
            }
            return null;
        }

        public override Airport GetClosestAirportWithinRadius(GeoPoint position, Length radius)
        {
            Airport closestAirport = null;
            double bestDistance = double.MaxValue;
            foreach (var airport in _airports)
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

        public override Runway GetRunwayFromAirportRunwayIdentifier(string airportIdentifier, string runwayIdentifier)
        {
            // TODO !!
            return null;
        }

        public override Airway GetAirwayFromIdentifierAndFixes(string airwayIdentifier, Fix startFix, Fix endFix)
        {
            return null;
        }
    }
}
