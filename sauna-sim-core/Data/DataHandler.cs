using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Units;
using NavData_Interface;
using NavData_Interface.DataSources;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fixes;
using NavData_Interface.Objects.LegCollections.Airways;
using NavData_Interface.Objects.LegCollections.Procedures;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SaunaSim.Core.Data
{
    public static class DataHandler
    {
        private static CombinedSource _navdataSource;
        private static readonly object _navdataMutex = new object();

        public static readonly string FAKE_AIRPORT_NAME = "_FAKE_AIRPORT";

        private static readonly int NAVIGRAPH_PRIORITY = 0;
        private static readonly int SCT_PRIORITY = 1;
        private static readonly int MEM_PRIORITY = 2;

        static DataHandler()
        {
            _navdataSource = new CombinedSource("_combined_source_sauna_api");
            _navdataSource.AddSource(new InMemorySource("sauna_api_in_memory_navdata_source"), MEM_PRIORITY);
        }

        public static bool HasNavigraphDataLoaded()
        {
            lock (_navdataMutex)
            {
                return _navdataSource.HasSourceType<DFDSource>();
            }
        }

        public static string GetNavigraphFileUuid()
        {
            lock (_navdataMutex)
            {
                // Find DFD Source
                foreach (KeyValuePair<int, DataSource> pair in _navdataSource)
                {
                    if (pair.Value is DFDSource dfdSource)
                    {
                        return dfdSource.GetId();
                    }
                }
                return "";
            }
        }

        public static List<string> GetSectorFilesLoaded()
        {
            List<string> retList = new List<string>();

            lock (_navdataMutex)
            {
                foreach (KeyValuePair<int, DataSource> pair in _navdataSource)
                {
                    if (pair.Value is SCTSource sctSource)
                    {
                        retList.Add(sctSource.FileName);
                    }
                }

                return retList;
            }
        }

        public static void LoadSectorFile(string filename)
        {
            lock (_navdataMutex)
            {
                _navdataSource.AddSource(new SCTSource(filename), SCT_PRIORITY);
            }
        }

        public static void LoadNavigraphDataFile(string fileName, string uuid)
        {
            lock (_navdataMutex)
            {
                _navdataSource.AddSource(new DFDSource(fileName, uuid), NAVIGRAPH_PRIORITY);

            }
        }

        public static void AddAirport(Airport airport)
        {
            lock (_navdataMutex)
            {
                _navdataSource.GetSourceType<InMemorySource>()?.AddAirport(airport);
            }
        }

        public static void AddPublishedHold(PublishedHold hold)
        {
            lock (_navdataMutex)
            {
                _navdataSource.GetSourceType<InMemorySource>()?.AddPublishedHold(hold);
            }
        }

        public static PublishedHold GetPublishedHold(string wp, GeoPoint point)
        {
            lock (_navdataMutex)
            {
                Fix fix = _navdataSource.GetClosestFixByIdentifier(point, wp);

                if (fix != null)
                {
                    var hold = _navdataSource.GetSourceType<InMemorySource>()?.GetPublishedHold(fix);
                    return hold;
                }

                return null;
            }
        }

        public static void AddLocalizer(Localizer wp)
        {
            lock (_navdataMutex)
            {
                _navdataSource.GetSourceType<InMemorySource>()?.AddLocalizer(wp);
            }
        }

        public static Airport GetAirportByIdentifier(string airportIdent)
        {
            lock (_navdataMutex)
            {
                var airport = _navdataSource.GetAirportByIdentifier(airportIdent.ToUpper());
                return airport;
            }
        }

        public static Localizer GetLocalizer(string airportIdent, string rwyIdent)
        {
            lock (_navdataMutex)
            {
                var airport = _navdataSource.GetLocalizerFromAirportRunway(airportIdent.ToUpper(), rwyIdent.ToUpper());
                return airport;
            }
        }

        public static Fix GetClosestWaypointByIdentifier(string wpId, GeoPoint point)
        {
            lock (_navdataMutex)
            {
                var airport = _navdataSource.GetClosestFixByIdentifier(point, wpId);
                return airport;
            }
        }

        public static Sid GetSidByAirportAndIdentifier(string aiportIdentifier, string sidIdentifier)
        {
            lock (_navdataMutex)
            {
                var sid = _navdataSource.GetSidByAirportAndIdentifier(aiportIdentifier, sidIdentifier);

                return sid;
            }
        }

        public static Sid GetSidByAirportAndIdentifier(Fix airport, string sidIdentifier)
        {
            lock (_navdataMutex)
            {
                var sid = _navdataSource.GetSidByAirportAndIdentifier(airport.Identifier, sidIdentifier);

                return sid;
            }
        }

        public static Airway GetAirwayFromIdentifierAndFixes(string airwayIdentifier, Fix startFix, Fix endFix)
        {
            lock (_navdataMutex)
            {
                var airway = _navdataSource.GetAirwayFromIdentifierAndFixes(airwayIdentifier, startFix, endFix);

                return airway;
            }
        }

        public static bool IsValidAirwayIdentifier(string airwayIdentifier)
        {
            lock (_navdataMutex)
            {
                var result = _navdataSource.IsValidAirwayIdentifier(airwayIdentifier);

                return result;
            }
        }

        public static Star GetStarByAirportAndIdentifier(Fix airport, string sidIdentifier)
        {
            lock (_navdataMutex)
            {
                var sid = _navdataSource.GetStarByAirportAndIdentifier(airport.Identifier, sidIdentifier);

                return sid;
            }
        }
    }
}
