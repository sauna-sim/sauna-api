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

        private static NavDataInterface _navDataInterface;
        private static object _navDataInterfaceLock = new object();

        private static List<PublishedHold> publishedHolds = new List<PublishedHold>();
        private static object publishedHoldsLock = new object();

        public static bool HasNavigraphDataLoaded()
        {
            lock (_navDataInterfaceLock)
            {
                return _navDataInterface != null;
            }
        }

        public static void LoadNavigraphDataFile(string fileName)
        {
            lock (_navDataInterfaceLock)
            {
                _navDataInterface = new NavDataInterface(new DFDSource(fileName));
            }
        }

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

        public static Localizer GetLocalizer(string localizerFakeName)
        {
            foreach(var wp in waypoints)
            {
                if (wp.Name == localizerFakeName)
                {
                    return wp;
                }
            }

            return null;
        }

        public static Fix GetClosestWaypointByIdentifier(string wpId, double lat, double lon)
        {
            lock (_navDataInterfaceLock)
            {
                return _navDataInterface.GetClosestFixByIdentifier(new GeoPoint(lat, lon), wpId);
            }
        }
    }
}
