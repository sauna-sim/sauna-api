using AviationCalcUtilNet.Geo;
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
        public static (Latitude lat, Longitude lon) ParseCoordinate(string lat, string lon)
        {
            if (double.TryParse(lat, out double dLat) && double.TryParse(lon, out double dLon))
            {
                return (Latitude.FromDegrees(dLat), Longitude.FromDegrees(dLon)); // if these is already in decimal format, convert to double and return it
            }
            // this isn't in decimal format, try to parse it as DMS // (im gonna remove this comment) N051.40.20.960
            try
            {
                return (Latitude.FromVrc(lat), Longitude.FromVrc(lon));
            }
            catch (Exception e)
            {
                Console.WriteLine($"BLAME CASPIAN there was an exception in the coordinate thing: {e.Message}");
                throw new FormatException($"Could not parse coordinate: {lat} | {lon}");
            }
        }
    }
}