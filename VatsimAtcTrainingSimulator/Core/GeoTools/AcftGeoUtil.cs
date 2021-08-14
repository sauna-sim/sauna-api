using CoordinateSharp;
using Grib.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.GeoTools.Helpers;
using VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl;

namespace VatsimAtcTrainingSimulator.Core.GeoTools
{
    public static class AcftGeoUtil
    {
        public const double EARTH_RADIUS_M = 6371e3;
        public const double STD_PRES_HPA = 1013.25;
        public const double STD_TEMP_C = 15;
        public const double STD_LAPSE_RATE = 2.0 / 1000.0;
        public const double STD_PRES_DROP = 30.0;
        public const double CONV_FACTOR_KELVIN_C = 273.15;
        public const double CONV_FACTOR_M_FT = 3.28084;
        public const double CONV_FACTOR_INHG_HPA = 33.86;
        public const double CONV_FACTOR_NMI_M = 1852;

        /// <summary>
        /// Calculates the next Longitude and Latitude point for the aircraft. This is then set inside the Aircraft's position.
        /// </summary>
        /// <param name="pos">Aircraft Position</param>
        /// <param name="distanceNMi">Distance (Nautical Miles) that the aircraft should be moved</param>
        public static void CalculateNextLatLon(ref AcftData pos, double distanceNMi)
        {
            LatLonAltPoint point = new LatLonAltPoint(pos.Latitude, pos.Longitude, pos.AbsoluteAltitude);
            point.MoveByNMi(pos.Track_True, distanceNMi);

            pos.Latitude = point.Lat;
            pos.Longitude = point.Lon;
        }

        public static double CalculateTurnLeadDistance(LatLonAltPoint point, double theta, double r)
        {
            if (point == null)
            {
                return -1;
            }

            double halfAngle = DegreesToRadians(90 - (theta / 2));

            double halfTan = Math.Tan(halfAngle);

            // If sin of theta is 0, return null. This indicates that the turn must begin NOW
            if (halfTan == 0)
            {
                return -1;
            }

            // Find lead in distance
            double leadDist = r / halfTan;

            return leadDist;
        }

        public static double CalculateTurnLeadDistance(AcftData pos, Waypoint wp, double course)
        {
            // Find intersection
            LatLonAltPoint intersection = FindIntersection(pos, wp, course);            

            // Find degrees to turn
            double theta = Math.Abs(CalculateTurnAmount(pos.Track_True, course));

            // Calculate radius of turn
            double r = CalculateRadiusOfTurn(CalculateBankAngle(pos.GroundSpeed, 25, 3), pos.GroundSpeed);

            return CalculateTurnLeadDistance(intersection, theta, r);
        }

        /// <summary>
        /// Calculates the intersection between the aircraft's current track and a course to/from a waypoint.
        /// </summary>
        /// <param name="position">Aircraft Position</param>
        /// <param name="wp">Waypoint</param>
        /// <param name="course">Course To/From Waypoint</param>
        /// <returns>Whether or not an intersection exists.</returns>
        public static LatLonAltPoint FindIntersection(AcftData position, Waypoint wp, double course)
        {
            LatLonAltPoint point1 = new LatLonAltPoint(position.Latitude, position.Longitude);
            LatLonAltPoint point2 = new LatLonAltPoint(wp.Latitude, wp.Longitude);

            // Try both radials and see which one works
            LatLonAltPoint intersection1 = LatLonAltPoint.Intersection(point1, position.Track_True, point2, course);
            LatLonAltPoint intersection2 = LatLonAltPoint.Intersection(point1, position.Track_True, point2, (course + 180) % 360);

            if (intersection1 == null)
            {
                return intersection2;
            }

            if (intersection2 == null)
            {
                return intersection1;
            }

            double dist1 = CalculateFlatDistanceNMi(position.Latitude, position.Longitude, intersection1.Lat, intersection1.Lon);
            double dist2 = CalculateFlatDistanceNMi(position.Latitude, position.Longitude, intersection2.Lat, intersection2.Lon);

            if (dist1 < dist2)
            {
                return intersection1;
            }

            return intersection2;
        }

        /// <summary>
        /// Normalizes Longitude between -180 and +180 degrees
        /// </summary>
        /// <param name="lon">Input Longitude (degrees)</param>
        /// <returns>Normalized Longitude (degrees)</returns>
        public static double NormalizeLongitude(double lon)
        {
            return (lon + 540) % 360 - 180;
        }

        /// <summary>
        /// Normalizes Heading between 0 and 360 degrees
        /// </summary>
        /// <param name="hdg">Input Heading (degrees)</param>
        /// <returns>Normalized Heading (degrees)</returns>
        public static double NormalizeHeading(double hdg)
        {
            return (hdg + 360) % 360;
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degrees">Input Angle (degrees)</param>
        /// <returns>Output Angle (radians)</returns>
        public static double DegreesToRadians(double degrees)
        {
            return Math.PI * degrees / 180.0;
        }

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        /// <param name="radians">Input Angle (radians)</param>
        /// <returns>Output Angle (degrees)</returns>
        public static double RadiansToDegrees(double radians)
        {
            return 180.0 * radians / Math.PI;
        }

        /// <summary>
        /// Calculates distance across the Earth's surface between two points.
        /// Assumes that Earth is a sphere.
        /// </summary>
        /// <param name="lat1">Point 1 Latitude (degrees)</param>
        /// <param name="lon1">Point 1 Longitude (degrees)</param>
        /// <param name="lat2">Point 2 Latitude (degrees)</param>
        /// <param name="lon2">Point 2 Longitude (degrees)</param>
        /// <returns></returns>
        public static double CalculateFlatDistanceNMi(double lat1, double lon1, double lat2, double lon2)
        {
            double phi1 = DegreesToRadians(lat1);
            double phi2 = DegreesToRadians(lat2);
            double deltaPhi = DegreesToRadians(lat2 - lat1);
            double deltaLambda = DegreesToRadians(lon2 - lon1);

            double a = Math.Pow(Math.Sin(deltaPhi / 2), 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Pow(Math.Sin(deltaLambda / 2), 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double d = EARTH_RADIUS_M * c;

            return d / CONV_FACTOR_NMI_M;
        }

        public static double CalculateInitialBearing(double lat1, double lon1, double lat2, double lon2)
        {
            // Convert to Radians
            double phi1 = DegreesToRadians(lat1);
            double phi2 = DegreesToRadians(lat2);
            double lambda1 = DegreesToRadians(lon1);
            double lambda2 = DegreesToRadians(lon2);

            // Find angle between the two
            double y = Math.Sin(lambda2 - lambda1) * Math.Cos(phi2);
            double x = Math.Cos(phi1) * Math.Sin(phi2) - Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(lambda2 - lambda1);

            double theta = Math.Atan2(y, x);

            // Convert from -180, +180 to 0, 359
            return NormalizeHeading(RadiansToDegrees(theta));
        }

        public static double CalculateFinalBearing(double lat1, double lon1, double lat2, double lon2)
        {
            // Calculate initial bearing from end to start and reverse
            return (CalculateInitialBearing(lat2, lon2, lat1, lon1) + 180) % 360;
        }

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

        public static double CalculateBankAngle(double groundSpeed, double bankLimit, double turnRate)
        {
            double bankAngle = Math.Atan2(turnRate * groundSpeed, 1091) * 180.0 / Math.PI;

            return bankAngle > bankLimit ? bankLimit : bankAngle;
        }

        public static double CalculateRadiusOfTurn(double bankAngle, double groundSpeed)
        {
            return (Math.Pow(groundSpeed, 2) / (11.26 * Math.Tan(bankAngle * Math.PI / 180.0))) / 6076.1155;
        }

        public static double CalculateDistanceTravelledNMi(double groundSpeedKts, double timeMs)
        {
            return (timeMs / 1000.0) * (groundSpeedKts / 60.0 / 60.0);
        }

        public static double CalculateDegreesTurned(double distTravelledNMi, double radiusOfTurnNMi)
        {
            return (distTravelledNMi / radiusOfTurnNMi) * 180.0 / Math.PI;
        }

        public static double CalculateEndHeading(double startHeading, double degreesTurned, bool isRightTurn)
        {
            double newHeading = startHeading;

            if (isRightTurn)
            {
                newHeading += degreesTurned;
            }
            else
            {
                newHeading -= degreesTurned;
            }

            return NormalizeHeading(newHeading);
        }

        public static Tuple<double, double> CalculateChordHeadingAndDistance(double startHeading, double degreesTurned, double radiusOfTurnNMi, bool isRightTurn)
        {
            double chordLengthNMi = 2 * radiusOfTurnNMi * Math.Sin(degreesTurned * Math.PI / (180.0 * 2));
            double chordHeading = startHeading;

            if (isRightTurn)
            {
                chordHeading += (degreesTurned / 2);
            }
            else
            {
                chordHeading -= (degreesTurned / 2);
            }

            chordHeading = NormalizeHeading(chordHeading);

            return new Tuple<double, double>(chordHeading, chordLengthNMi);
        }

        public static double CalculateAbsoluteAlt(double alt_ind_ft, double pres_set_hpa, double sfc_pres_hpa)
        {
            double pressDiff = pres_set_hpa - sfc_pres_hpa;
            return alt_ind_ft - (STD_PRES_DROP * pressDiff);
        }

        public static double CalculateIndicatedAlt(double alt_abs_ft, double pres_set_hpa, double sfc_pres_hpa)
        {
            double pressDiff = pres_set_hpa - sfc_pres_hpa;
            return alt_abs_ft + (STD_PRES_DROP * pressDiff);
        }

        public static double CalculatePressureAlt(double alt_ind_ft, double pres_set_hpa)
        {
            double pressDiff = pres_set_hpa - STD_PRES_HPA;
            return alt_ind_ft - (STD_PRES_DROP * pressDiff);
        }

        public static double CalculateIsaTemp(double alt_pres_ft)
        {
            if (alt_pres_ft >= 36000)
            {
                return -56.5;
            }

            return STD_TEMP_C - (alt_pres_ft * STD_LAPSE_RATE);
        }

        public static double CalculateDensityAlt(double alt_pres_ft, double sat)
        {
            double isaDev = sat - CalculateIsaTemp(alt_pres_ft);

            return alt_pres_ft + (118.8 * isaDev);
        }

        public static double CalculateTAS(double ias, double pres_set_hpa, double alt_ind_ft, double sat)
        {
            double daStdTemp = CONV_FACTOR_KELVIN_C + STD_TEMP_C - (CalculateDensityAlt(CalculatePressureAlt(alt_ind_ft, pres_set_hpa), sat) * STD_LAPSE_RATE);
            double tempRatio = daStdTemp / (STD_TEMP_C + CONV_FACTOR_KELVIN_C);
            double densityRatio = Math.Pow(tempRatio, 1 / 0.234969);
            double tasCoeff = 1 / (Math.Sqrt(densityRatio));

            return tasCoeff * ias;
        }

        public static double CalculateTurnAmount(double currentHeading, double desiredHeading)
        {
            // Either distance or 360 - distance
            double phi = Math.Abs(desiredHeading - currentHeading) % 360;

            double distance = phi > 180 ? 360 - phi : phi;

            // Figure out if left turn or right turn
            int sign = 1;
            if ((currentHeading - desiredHeading >= 0 && currentHeading - desiredHeading <= 180) ||
                (currentHeading - desiredHeading <= -180 && currentHeading - desiredHeading >= -360))
            {
                sign = -1;
            }

            return distance * sign;
        }
    }
}
