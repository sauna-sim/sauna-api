using AviationCalcUtilNet.GeoTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AselAtcTrainingSim.AselSimCore.Data
{
    public static class DataHandler
    {
        private static List<Waypoint> waypoints = new List<Waypoint>();
        private static object waypointsLock = new object();
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

        public static void AddWaypoint(Waypoint wp)
        {
            lock (waypointsLock)
            {
                waypoints.Add(wp);
            }
        }

        public static Waypoint GetClosestWaypointByIdentifier(string wpId, double lat, double lon)
        {
            Waypoint foundWp = null;
            double minDistance = double.MaxValue;

            lock (waypointsLock)
            {
                foreach (Waypoint wp in waypoints)
                {
                    double dist = GeoPoint.FlatDistanceNMi(new GeoPoint(lat, lon), new GeoPoint(wp.Latitude, wp.Longitude));

                    if (wp.Identifier == wpId.ToUpper() && (foundWp == null || dist < minDistance))
                    {
                        foundWp = wp;
                        minDistance = dist;
                    }
                }
            }

            return foundWp;
        }
    }
}
