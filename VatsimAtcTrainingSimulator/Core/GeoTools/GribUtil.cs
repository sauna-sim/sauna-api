using Grib.Api;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft;

namespace VatsimAtcTrainingSimulator.Core.GeoTools
{
    public static class GribUtil
    {
        public static GribDataPoint GetClosestGribPoint(AircraftPosition pos)
        {
            GribTile tile = GribTile.FindOrCreateGribTile(pos, DateTime.UtcNow);

            return tile.GetClosestPoint(pos);
        }
    }
}
