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
            GeoPoint point = new GeoPoint(pos.Latitude, pos.Longitude, pos.AbsoluteAltitude);
            point.MoveByNMi(pos.Track_True, distanceNMi);

            pos.Latitude = point.Lat;
            pos.Longitude = point.Lon;
        }

        /// <summary>
        /// Calculates the direct course to intercept towards a waypoint.
        /// Returns -1 if direct course is not possible to achieve.
        /// </summary>
        /// <param name="aircraft">Aircraft position</param>
        /// <param name="waypoint">Waypoint position</param>
        /// <param name="r">Radius of Turn</param>
        /// <param name="curBearing">Aircraft's current bearing</param>
        /// <returns><c>double</c> Direct bearing to waypoint after turn</returns>
        public static double CalculateDirectBearingAfterTurn(GeoPoint aircraft, GeoPoint waypoint, double r, double curBearing)
        {
            // Set waypoint's altitude to aircraft's altitude to minimize error
            waypoint.Alt = aircraft.Alt;

            // Get direct bearing to waypoint from aircraft. Use this to figure out right or left turn.
            bool isRightTurn = CalculateTurnAmount(curBearing, GeoPoint.InitialBearing(aircraft, waypoint)) > 0;

            // If distance is less than the diameter or turn, direct is impossible
            double dirDist = GeoPoint.DistanceNMi(aircraft, waypoint);

            if (dirDist < r * 2)
            {
                return -1;
            } else if (dirDist == r * 2)
            {
                // Make a 180 degree turn
                return NormalizeHeading(isRightTurn ? curBearing + 180 : curBearing - 180);
            }

            // Calculate bearing to circle center point
            double bearingToC = NormalizeHeading(isRightTurn ? curBearing + 90 : curBearing - 90);

            // Find center point
            GeoPoint c = aircraft;
            c.MoveByNMi(bearingToC, r);

            // Find distance and bearing from c to waypoint
            double finalBearingC = GeoPoint.FinalBearing(c, waypoint);
            double distC = GeoPoint.DistanceNMi(c, waypoint);

            // Find angle between finalBearingC and desired bearing
            double ang = RadiansToDegrees(Math.Asin(r / distC));

            // Calculate final bearing to waypoint
            double turnDirBearing = isRightTurn ? finalBearingC + ang : finalBearingC - ang;

            return NormalizeHeading(turnDirBearing);
        }

        public static double CalculateCrossTrackErrorM(GeoPoint aircraft, GeoPoint waypoint, double course)
        {
            // Set waypoint's altitude to aircraft's altitude to minimize error
            waypoint.Alt = aircraft.Alt;

            // Find radial
            double finalDirBearing = GeoPoint.FinalBearing(aircraft, waypoint);
            double dist = GeoPoint.DistanceM(aircraft, waypoint);

            double radial = Math.Abs(CalculateTurnAmount(course, finalDirBearing)) < 90 ? NormalizeHeading(course + 180) : course;

            // Calculate radius
            double R = EARTH_RADIUS_M + (aircraft.Alt / CONV_FACTOR_M_FT);

            // Calculate angular distance between aircraft and waypoint
            double sigma13 = dist / R;

            // Initial bearing from waypoint to aircraft
            double theta13 = DegreesToRadians(NormalizeHeading(finalDirBearing + 180));

            // Radial in radians
            double theta12 = DegreesToRadians(radial);

            double xTrackM = Math.Asin(Math.Sin(sigma13) * Math.Sin(theta13 - theta12)) * R;

            return xTrackM;
        }

        public static double CalculateTurnLeadDistance(GeoPoint point, double theta, double r)
        {
            if (point == null)
            {
                return -1;
            }

            double halfAngle = DegreesToRadians(theta / 2);

            double halfTan = Math.Tan(halfAngle);

            // Find lead in distance
            double leadDist = r * halfTan;

            return leadDist;
        }

        public static double CalculateTurnLeadDistance(AcftData pos, Waypoint wp, double course)
        {
            // Find intersection
            GeoPoint intersection = FindIntersection(pos, wp, course);            

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
        public static GeoPoint FindIntersection(AcftData position, Waypoint wp, double course)
        {
            GeoPoint point1 = new GeoPoint(position.Latitude, position.Longitude);
            GeoPoint point2 = new GeoPoint(wp.Latitude, wp.Longitude);

            // Try both radials and see which one works
            GeoPoint intersection1 = GeoPoint.Intersection(point1, position.Track_True, point2, course);
            GeoPoint intersection2 = GeoPoint.Intersection(point1, position.Track_True, point2, (course + 180) % 360);

            if (intersection1 == null)
            {
                return intersection2;
            }

            if (intersection2 == null)
            {
                return intersection1;
            }

            double dist1 = GeoPoint.FlatDistanceNMi(point1, intersection1);
            double dist2 = GeoPoint.FlatDistanceNMi(point1, intersection2);

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
