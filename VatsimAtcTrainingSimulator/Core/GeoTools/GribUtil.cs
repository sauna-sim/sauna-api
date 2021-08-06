using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.GeoTools
{

    public static class GribUtil
    {
        public async static Task GetWindsAloft(AcftPosition pos)
        {
            GribTile tile = GribTile.FindOrCreateGribTile(pos, DateTime.UtcNow);

            if (!tile.Downloaded)
            {
                await tile.DownloadTile();
            }
        }
    }
}
