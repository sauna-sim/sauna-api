using CoordinateSharp;
using Grib.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.GeoTools
{    
    public static class AcftGeoUtil
    {
        public const double STD_PRES_HPA = 1013.25;
        public const double STD_TEMP_C = 15;
        public const double STD_LAPSE_RATE = 2.0 / 1000.0;
        public const double STD_PRES_DROP = 30.0;
        public const double CONV_FACTOR_KELVIN_C = 273.15;
        public const double CONV_FACTOR_M_FT = 3.28084;

        public static void CalculateNextLatLon(AcftData pos, double nextAlt, double distanceNMi)
        {
            Coordinate start = new Coordinate(pos.Latitude, pos.Longitude);
            double distanceM = distanceNMi * 1852;
            start.Move(distanceM, pos.Track_True, Shape.Ellipsoid);
            pos.UpdatePosition(start.Latitude.ToDouble(), start.Longitude.ToDouble(), nextAlt);
        }

        public static double CalculateBankAngle(double groundSpeed, double bankLimit, double turnRate)
        {
            double bankAngle = Math.Atan2(turnRate * groundSpeed,1091) * 180.0 / Math.PI;

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

            if (newHeading >= 360)
            {
                newHeading -= 360;
            }
            else if (newHeading < 0)
            {
                newHeading += 360;
            }

            return newHeading;
        }

        public static Tuple<double, double> CalculateChordHeadingAndDistance(double startHeading, double degreesTurned, double radiusOfTurnNMi, bool isRightTurn)
        {
            double chordLengthNMi = 2 * radiusOfTurnNMi * Math.Sin(degreesTurned * Math.PI / (180.0 * 2));
            double chordHeading = startHeading;

            if (isRightTurn)
            {
                chordHeading += (degreesTurned / 2);
            } else
            {
                chordHeading -= (degreesTurned / 2);
            }

            if (chordHeading >= 360)
            {
                chordHeading -= 360;
            } else if (chordHeading < 0)
            {
                chordHeading += 360;
            }

            return new Tuple<double, double>(chordHeading, chordLengthNMi);
        }

        public static double CalculateAbsoluteAlt(double alt_ind_ft, double pres_set_hpa, double sfc_pres_hpa)
        {
            double pressDiff = pres_set_hpa - sfc_pres_hpa;
            return alt_ind_ft - (STD_PRES_DROP * pressDiff);
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
