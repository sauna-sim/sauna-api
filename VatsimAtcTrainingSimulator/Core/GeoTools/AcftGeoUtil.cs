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

        public static void CalculateNextLatLon(AcftData pos, double vsFtMin, double nextHeading, double timeMs)
        {
            Coordinate start = new Coordinate(pos.Latitude, pos.Longitude);
            double distanceNMi = (timeMs / 1000.0) * (pos.GroundSpeed / 60.0 / 60.0);
            double distanceM = distanceNMi * 1852;
            start.Move(distanceM, pos.Track_True, Shape.Ellipsoid);
            pos.UpdatePosition(start.Latitude.ToDouble(), start.Longitude.ToDouble(), pos.Altitude + ((timeMs / 1000.0) * (vsFtMin / 60.0)), nextHeading, pos.IndicatedAirSpeed);
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
            double headingDifference = desiredHeading - currentHeading;

            // Normalize for across 360 or 0
            if (headingDifference < 0)
            {
                headingDifference += 360;
            }

            // Right turn less than 180 degrees
            if (headingDifference <= 180)
            {
                return headingDifference;
            }

            // Otherwise it's a left turn
            return -1 * (headingDifference - 180);
        }
    }
}
