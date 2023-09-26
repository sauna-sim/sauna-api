using AviationCalcUtilNet.GeoTools;
using NavData_Interface;
using NavData_Interface.DataSources;
using NavData_Interface.Objects.Fix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaunaSim.Core.Data
{
    public static class DataHandler
    {
        private static List<Localizer> waypoints = new List<Localizer>();
        private static object waypointsLock = new object();

        private static NavDataInterface _navDataInterface = new NavDataInterface(new DFDSource("e_dfd_2301.s3db"));
        private static object _navDataInterfaceLock = new object();

        private static List<PublishedHold> publishedHolds = new List<PublishedHold>();
        private static object publishedHoldsLock = new object();

        public static void AddPublishedHold(PublishedHold hold)
        {
            lock (publishedHoldsLock)
            {
                publishedHolds.Add(hold);
            }
        }

        public static PublishedHold GetPublishedHold(string wp)
        {
            lock (publishedHoldsLock)
            {
                foreach (PublishedHold hold in publishedHolds)
                {
                    if (hold.Waypoint == wp)
                    {
                        return hold;
                    }
                }
            }

            return null;
        }

        public static void AddLocalizer(Localizer wp)
        {
            lock(waypointsLock)
            {
                waypoints.Add(wp);
            }
        }

        public static Fix GetClosestWaypointByIdentifier(string wpId, double lat, double lon)
        {
            lock (_navDataInterfaceLock)
            {
                GeoPoint point = new GeoPoint(lat, lon);

                List<Fix> fixes = new List<Fix>();

                var tempFix = _navDataInterface.GetClosestFixByIdentifier(new GeoPoint(lat, lon), wpId);

                if (tempFix != null)
                {
                    fixes.Add(tempFix);
                }
                
                foreach (Fix wp in waypoints)
                {
                    if (wp.Identifier == wpId)
                    {
                        fixes.Add(wp);
                    }
                }

                Fix closestFix = null;
                double closestDistance = double.MaxValue;

                foreach (var fix in fixes)
                {
                    double distance = GeoPoint.DistanceM(point, fix.Location);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestFix = fix;
                    }
                }

                return closestFix;
            }
        }
    }
}
