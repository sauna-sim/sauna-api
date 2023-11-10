using System;
using System.Collections.Generic;
using AviationCalcUtilNet.GeoTools;
using NavData_Interface.DataSources;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fix;

namespace SaunaSim.Core.Data.NavData
{
	public class CustomNavDataSource : DataSource
	{
        private List<Fix> _fixes;
        private List<Localizer> _locs;
        private List<PublishedHold> _pubHolds;

		public CustomNavDataSource()
		{
            _fixes = new List<Fix>();
            _locs = new List<Localizer>();
            _pubHolds = new List<PublishedHold>();
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

        public override List<Fix> GetFixesByIdentifier(string identifier)
        {
            List<Fix> retFixes = new List<Fix>();

            foreach (Fix fix in _fixes)
            {
                if (fix.Identifier == identifier)
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
                if (loc.Airport_identifier == airportIdentifier && loc.Runway_identifier == runwayIdentifier)
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
                if (hold.Waypoint.Identifier == fix.Identifier && GeoPoint.DistanceM(hold.Waypoint.Location, fix.Location) < 1000)
                {
                    return hold;
                }
            }
            return null;
        }
    }
}

