using AviationCalcUtilNet.Atmos;
using AviationCalcUtilNet.Atmos.Grib;
using AviationCalcUtilNet.Aviation;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.Math;
using AviationCalcUtilNet.Units;
using SaunaSim.Core.Simulator.Aircraft.Autopilot;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;
using SaunaSim.Core.Simulator.Aircraft.Performance;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Simulator.Aircraft.FMS
{
    public struct FmsVnavLegIterator
    {
        /// <summary>
        /// Leg Index
        /// </summary>
        public int Index { get; set; }
        public Angle ApchAngle { get; set; } // Approach angle (Only used in the approach phase)
        public Length DistanceToRwy { get; set; } // Distance to the runway threshold
        public bool ShouldRewind { get; set; }
        public Length AlongTrackDistance { get; set; }

        // Information from last iteration
        public Length LastAlt { get; set; } // Altitude last waypoint was crossed at
        public int LastSpeed { get; set; } // Target speed last waypoint was crossed at
        public Length LaterDecelLength { get; set; } // Decel length left

        // Constraints from earlier (further up the arrival)
        public Length EarlyUpperAlt { get; set; }
        public Length EarlyLowerAlt { get; set; }
        public int EarlySpeed { get; set; }

        // Index where constraints were detected
        public int EarlySpeedI { get; set; }
        public int EarlyUpperAltI { get; set; }
        public int EarlyLowerAltI { get; set; }
    }

    public static class FmsVnavUtil
    {
        public static bool DoesPointMeetConstraints(FmsPoint point, Length upperAlt, Length lowerAlt, int speedKts)
        {
            // Check if VnavPoints is not empty
            if (point.VnavPoints.Count == 0) return false;

            // Check upper alt
            if (upperAlt != null && point.VnavPoints[0].Alt > upperAlt) return false;

            // Check lower alt
            if (lowerAlt != null && point.VnavPoints[0].Alt < lowerAlt) return false;

            // Check speed
            if (speedKts > 0)
            {
                if (point.VnavPoints[0].SpeedUnits != Autopilot.McpSpeedUnitsType.KNOTS) return false;
                if (point.VnavPoints[0].Speed >  speedKts) return false;
            }

            return true;
        }

        public static Length CalculateDecelLength(int targetIas_kts, int initialIas_kts, Length alt, Length densAlt, double acftMass_kg, Bearing finalTrueCourse, PerfData perfData, GribDataPoint gribPoint)
        {
            // Get deceleration at level flight at speedconstraint
            var lvlPitch = PerfDataHandler.GetRequiredPitchForVs(perfData, 0, targetIas_kts, densAlt.Feet, acftMass_kg, 0, 0);
            (var decelRate, _) = PerfDataHandler.CalculatePerformance(perfData, lvlPitch, 0, targetIas_kts, densAlt.Feet, acftMass_kg, 0, 0);

            // Calculate initial and final velocities
            var (initialVelocity, _) = AtmosUtil.ConvertIasToTas(Velocity.FromKnots(initialIas_kts), gribPoint.LevelPressure, alt, gribPoint.GeoPotentialHeight, gribPoint.Temp);
            var (finalVelocity, _) = AtmosUtil.ConvertIasToTas(Velocity.FromKnots(targetIas_kts), gribPoint.LevelPressure, alt, gribPoint.GeoPotentialHeight, gribPoint.Temp);
            var (windDir, windSpd) = gribPoint.Wind;
            var headWind = AviationUtil.GetHeadwindComponent(windDir, windSpd, finalTrueCourse);
            initialVelocity += headWind;
            finalVelocity += headWind;

            return Length.FromMeters((Math.Pow(finalVelocity.MetersPerSecond, 2) * Math.Pow(initialVelocity.MetersPerSecond, 2)) / (double)(Acceleration.FromKnotsPerSecond(decelRate * 2))); // Vf^2 - Vi^2 / (2a)
        }

        public static Length CalculateStartAltitude(Length endAlt, Length legLength, Angle angle)
        {
            return endAlt + (legLength * Math.Tan(angle.Radians));
        }

        public static Length CalculateDistanceForAltitude(Length endAlt, Length startAlt, Angle angle)
        {
            return (startAlt - endAlt) / Math.Tan(angle.Radians);
        }

        public static Length CalculateDensityAltitude(Length alt, GribDataPoint gribPoint)
        {
            var temp = AtmosUtil.CalculateTempAtAlt(alt, gribPoint.GeoPotentialHeight, gribPoint.Temp);
            var pressure = AtmosUtil.CalculatePressureAtAlt(alt, gribPoint.GeoPotentialHeight, gribPoint.LevelPressure, temp);
            return  AtmosUtil.CalculateDensityAltitude(pressure, temp);
        }
    }
}
