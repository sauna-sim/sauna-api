using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Units;
using NavData_Interface;
using NavData_Interface.DataSources;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fixes;
using System;
using System.Collections.Generic;
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
        private static Mutex _navdataMutex;

        public static readonly string FAKE_AIRPORT_NAME = "_FAKE_AIRPORT";

        private static readonly int NAVIGRAPH_PRIORITY = 0;
        private static readonly int SCT_PRIORITY = 1;
        private static readonly int MEM_PRIORITY = 2;

        static DataHandler()
        {
            _navdataMutex = new Mutex();
            _navdataSource = new CombinedSource("_combined_source_sauna_api");
            _navdataSource.AddSource(new InMemorySource("sauna_api_in_memory_navdata_source"), MEM_PRIORITY);
        }

        public static bool HasNavigraphDataLoaded()
        {
            _navdataMutex.WaitOne();
            var hasNavigraph = _navdataSource.HasSourceType<DFDSource>();
            _navdataMutex.ReleaseMutex();
            return hasNavigraph;
        }

        public static string GetNavigraphFileUuid()
        {
            _navdataMutex.WaitOne();

            // Find DFD Source
            foreach (KeyValuePair<int, DataSource> pair in _navdataSource)
            {
                if (pair.Value is DFDSource dfdSource)
                {
                    _navdataMutex.ReleaseMutex();
                    return dfdSource.GetId();
                }
            }

            _navdataMutex.ReleaseMutex();
            return "";
        }

        public static List<string> GetSectorFilesLoaded()
        {
            List<string> retList = new List<string>();

            _navdataMutex.WaitOne();

            foreach (KeyValuePair<int, DataSource> pair in _navdataSource)
            {
                if (pair.Value is SCTSource sctSource)
                {
                    retList.Add(sctSource.FileName);
                }
            }

            _navdataMutex.ReleaseMutex();

            return retList;
        }

        public static void LoadSectorFile(string filename)
        {
            _navdataMutex.WaitOne();
            _navdataSource.AddSource(new SCTSource(filename), SCT_PRIORITY);
            _navdataMutex.ReleaseMutex();
        }

        public static void LoadNavigraphDataFile(string fileName, string uuid)
        {
            _navdataMutex.WaitOne();
            _navdataSource.AddSource(new DFDSource(fileName, uuid), NAVIGRAPH_PRIORITY);
            _navdataMutex.ReleaseMutex();
        }

        public static void AddAirport(Airport airport)
        {
            _navdataMutex.WaitOne();
            _navdataSource.GetSourceType<InMemorySource>()?.AddAirport(airport);
            _navdataMutex.ReleaseMutex();
        }

        public static void AddPublishedHold(PublishedHold hold)
        {
            _navdataMutex.WaitOne();
            _navdataSource.GetSourceType<InMemorySource>()?.AddPublishedHold(hold);
            _navdataMutex.ReleaseMutex();
        }

        public static PublishedHold GetPublishedHold(string wp, GeoPoint point)
        {
            _navdataMutex.WaitOne();
            Fix fix = _navdataSource.GetClosestFixByIdentifier(point, wp);

            if (fix != null)
            {
                var hold = _navdataSource.GetSourceType<InMemorySource>()?.GetPublishedHold(fix);
                _navdataMutex.ReleaseMutex();
                return hold;
            }

            _navdataMutex.ReleaseMutex();
            return null;
        }

        public static void AddLocalizer(Localizer wp)
        {
            _navdataMutex.WaitOne();
            _navdataSource.GetSourceType<InMemorySource>()?.AddLocalizer(wp);
            _navdataMutex.ReleaseMutex();
        }

        public static Airport GetAirportByIdentifier(string airportIdent)
        {
            _navdataMutex.WaitOne();
            var airport = _navdataSource.GetAirportByIdentifier(airportIdent.ToUpper());
            _navdataMutex.ReleaseMutex();

            return airport;
        }

        public static Localizer GetLocalizer(string airportIdent, string rwyIdent)
        {
            _navdataMutex.WaitOne();
            var airport = _navdataSource.GetLocalizerFromAirportRunway(airportIdent.ToUpper(), rwyIdent.ToUpper());
            _navdataMutex.ReleaseMutex();

            return airport;
        }

        public static Fix GetClosestWaypointByIdentifier(string wpId, GeoPoint point)
        {
            _navdataMutex.WaitOne();
            var airport = _navdataSource.GetClosestFixByIdentifier(point, wpId);
            _navdataMutex.ReleaseMutex();

            return airport;
        }
    }
}
