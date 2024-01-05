using System;
using System.Collections.Generic;
using AviationCalcUtilNet.GeoTools;
using NavData_Interface.DataSources;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fixes;

namespace SaunaSim.Core.Data.NavData
{
	public class CustomNavDataSource : DataSource
	{
        private List<Fix> _fixes;
        private List<Localizer> _locs;
        private List<PublishedHold> _pubHolds;
        private List<Airport> _airports;

		public CustomNavDataSource()
		{
            _fixes = new List<Fix>();
            _locs = new List<Localizer>();
            _pubHolds = new List<PublishedHold>();
            _airports = new List<Airport>();
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
                if (hold.Waypoint.Identifier.ToUpper() == fix.Identifier.ToUpper() && GeoPoint.Distance(hold.Waypoint.Location, fix.Location).Meters < 1000)
                {
                    return hold;
                }
            }
            return null;
        }
    }
}

