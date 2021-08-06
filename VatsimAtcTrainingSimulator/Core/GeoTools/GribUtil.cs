using Grib.Api;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.GeoTools
{
    public static class GribUtil
    {
        public static GribDataPoint GetClosestGribPoint(AcftData pos)
        {
            GribTile tile = GribTile.FindOrCreateGribTile(pos, DateTime.UtcNow);

            return tile.GetClosestPoint(pos);
        }
    }
}
