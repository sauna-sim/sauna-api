using AviationCalcUtilManaged.GeoTools;
using AviationCalcUtilManaged.GeoTools.GribTools;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AviationSimulation.GeoTools.GribTools
{
    public static class GribUtil
    {
        public static GribDataPoint GetClosestGribPoint(GeoPoint pos)
        {
            GribTile tile = GribTile.FindOrCreateGribTile(pos, DateTime.UtcNow);

            if (tile == null)
            {
                return null;
            }

            return tile.GetClosestPoint(pos);
        }

        /// <summary>
        /// Converts VRC/Euroscope sector file longitudes/latitudes to decimal degrees.
        /// </summary>
        /// <param name="input"><c>string</c> Sector file Lon/Lat.</param>
        /// <returns><c>double</c> Signed Lon/Lat (decimal degrees)</returns>
        public static double ConvertSectorFileDegMinSecToDecimalDeg(string input)
        {
            double retVal = 0;
            string[] items = input.Split('.');

            try
            {
                // Get degrees
                retVal = Convert.ToDouble(items[0].Substring(1));

                // Add minutes
                retVal += Convert.ToInt32(items[1]) / 60.0;

                // Add seconds
                retVal += Convert.ToDouble(items[2] + "." + items[3]) / 3600;

                // Negate if South or West
                switch (items[0].Substring(0, 1))
                {
                    case "S":
                    case "W":
                        retVal *= -1;
                        break;
                }

                return retVal;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
