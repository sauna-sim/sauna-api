using AviationCalcUtilNet.GeoTools;
using System;

namespace SaunaSim.Api.Utilities
{
    public static class CoordinateUtil
    {
        /// <summary>
        /// Takes a string coordinate from a scenario file in either DMS or decimal format and parses it into a double.
        /// </summary>
        /// <param name="coord">The string coordinate to parse</param>
        /// <returns>The coordinate as a double</returns>
        /// <throws>FormatException: If the coordinate is not in a supported format</throws>
        public static (double lat, double lon) ParseCoordinate(string lat, string lon)
        {
            double latResult = 0;
            double lonResult = 0;

            if (double.TryParse(lat, out latResult) && double.TryParse(lon, out lonResult))
            {
                return (latResult, lonResult); // if these is already in decimal format, convert to double and return it
            }
            // this isn't in decimal format, try to parse it as DMS // (im gonna remove this comment) N051.40.20.960
            try
            {
                GeoUtil.ConvertVrcToDecimalDegs(lat, lon, out latResult, out lonResult);
            }
            catch (Exception e)
            {
                Console.WriteLine($"BLAME CASPIAN there was an exception in the coordinate thing: {e.Message}");
                throw new FormatException($"Could not parse coordinate: {lat} | {lon}");
            }

            return (latResult, lonResult);
        }
    }
}