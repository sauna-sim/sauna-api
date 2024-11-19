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
        public bool LimitCrossed { get; set; }

        // Information from last iteration
        public Length LastAlt { get; set; } // Altitude last waypoint was crossed at
        public int LastSpeed { get; set; } // Target speed last waypoint was crossed at
        public Length LaterDecelLength { get; set; } // Decel length left
        public int LaterDecelSpeed { get; set; }

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

        private static double GetKnotsSpeed(McpSpeedUnitsType units, int speed, Length altitude, GribDataPoint gribPoint)
        {
            if (units == McpSpeedUnitsType.MACH)
            {
                Temperature t0 = gribPoint != null ? gribPoint.Temp : AtmosUtil.ISA_STD_TEMP;
                Length h0 = gribPoint != null ? gribPoint.GeoPotentialHeight : Length.FromMeters(0);
                Pressure p0 = gribPoint != null ? gribPoint.LevelPressure : AtmosUtil.ISA_STD_PRES;
                Temperature t = AtmosUtil.CalculateTempAtAlt(altitude, h0, t0);

                Velocity tas = AtmosUtil.ConvertMachToTas(speed / 100.0, t);
                return AtmosUtil.ConvertTasToIas(tas, p0, altitude, h0, t0).ias.Knots;
            }

            return speed;
        }

        public static (McpSpeedUnitsType units, int selectedSpeed) GetConversionSpeed(Velocity ias, double mach, Length altitude, GribDataPoint gribPoint)
        {
            double curIasMach;
            if (gribPoint != null)
            {
                (_, curIasMach) = AtmosUtil.ConvertIasToTas(ias, gribPoint.LevelPressure, altitude, gribPoint.GeoPotentialHeight, gribPoint.Temp);
            } else
            {
                (_, curIasMach) = AtmosUtil.ConvertIasToTas(ias, AtmosUtil.ISA_STD_PRES, altitude, (Length)0, AtmosUtil.ISA_STD_TEMP);
            }

            if (curIasMach >= mach)
            {
                return (McpSpeedUnitsType.MACH, (int)(mach * 100));
            } else
            {
                return (McpSpeedUnitsType.KNOTS, (int)ias.Knots);
            }
        }

        public static (McpSpeedUnitsType units, int selectedSpeed) CalculateFmsSpeed(FmsPhaseType fmsPhase, Length distanceToDest, Length indicatedAltitude, PerfData perfData, Length departureAirportElevation, PerfInit perfInit, GribDataPoint gribPoint)
        {
            if (fmsPhase == FmsPhaseType.CLIMB)
            {
                if (indicatedAltitude < departureAirportElevation + Length.FromFeet(1000))
                {
                    return (McpSpeedUnitsType.KNOTS, perfData.V2_KIAS);
                } else if (indicatedAltitude < departureAirportElevation + Length.FromFeet(3000))
                {
                    return (McpSpeedUnitsType.KNOTS, Math.Min(210, perfInit.ClimbKias));
                } else if (indicatedAltitude.Feet < perfInit.LimitAlt)
                {
                    return (McpSpeedUnitsType.KNOTS, Math.Min(perfInit.ClimbKias, perfInit.LimitSpeed));
                } else
                {
                    if (perfInit.ClimbMach <= 0)
                    {
                        return (McpSpeedUnitsType.KNOTS, perfInit.ClimbKias);
                    }

                    return GetConversionSpeed(Velocity.FromKnots(perfInit.ClimbKias), perfInit.ClimbMach / 100.0, indicatedAltitude, gribPoint);
                }
            } else if (fmsPhase == FmsPhaseType.CRUISE)
            {
                if (perfInit.CruiseAlt < perfInit.LimitAlt)
                {
                    return (McpSpeedUnitsType.KNOTS, perfInit.LimitSpeed);
                } else
                {
                    if (perfInit.CruiseMach <= 0)
                    {
                        return (McpSpeedUnitsType.KNOTS, perfInit.CruiseKias);
                    }

                    return GetConversionSpeed(Velocity.FromKnots(perfInit.CruiseKias), perfInit.CruiseMach / 100.0, indicatedAltitude, gribPoint);
                }
            } else if (fmsPhase == FmsPhaseType.DESCENT)
            {
                if (indicatedAltitude.Feet < perfInit.LimitAlt)
                {
                    return (McpSpeedUnitsType.KNOTS, Math.Min(perfInit.DescentKias, perfInit.LimitSpeed));
                } else
                {
                    if (perfInit.DescentMach <= 0)
                    {
                        return (McpSpeedUnitsType.KNOTS, perfInit.DescentKias);
                    }

                    return GetConversionSpeed(Velocity.FromKnots(perfInit.DescentKias), perfInit.DescentMach / 100.0, indicatedAltitude, gribPoint);
                }
            } else if (fmsPhase == FmsPhaseType.APPROACH)
            {
                var speedGates = perfData.ApproachSpeedGates;

                if (speedGates == null || speedGates.Count <= 0)
                {
                    return (McpSpeedUnitsType.KNOTS, Math.Min(perfInit.DescentKias, perfInit.LimitSpeed));
                }

                // Use distance from threshold to determine approach speed
                foreach ((int distance, int speed) in speedGates)
                {
                    if (distanceToDest.NauticalMiles <= distance)
                    {
                        return (McpSpeedUnitsType.KNOTS, speed);
                    }
                }

                return (McpSpeedUnitsType.KNOTS, speedGates[speedGates.Count - 1].Item2);
            } else
            {
                // Go Around
                return (McpSpeedUnitsType.KNOTS, 135); // TODO: DEFINITELY CHANGE THIS!!!!!!!!!!!!!
            }
        }

        public static FmsVnavLegIterator ProcessLegForVnav(IRouteLeg leg, FmsVnavLegIterator iterator, PerfData perfData, double mass_kg, PerfInit perfInit, Length depArptElev)
        {
            // Ensure current leg has an endpoint and a leg length
            if (leg.EndPoint == null || leg.LegLength <= Length.FromMeters(0))
            {
                // Skip this leg
                if (iterator.ShouldRewind)
                {
                    iterator.Index++;
                } else
                {
                    iterator.Index--;
                }

                return iterator;
            }

            FmsPoint endPoint = leg.EndPoint;
            McpSpeedUnitsType targetSpeedUnits;
            int targetSpeed;

            // If it's the first leg, set Vnav point
            if (iterator.LastAlt == null)
            {
                iterator.LastAlt = endPoint.LowerAltitudeConstraint > 0 ? Length.FromFeet(endPoint.LowerAltitudeConstraint) : Length.FromMeters(0);
                var startGribPoint = new GribDataPoint(endPoint.Point.PointPosition.Lat, endPoint.Point.PointPosition.Lon, AtmosUtil.ISA_STD_PRES); // TODO: Change to actually use wind-uplink.
                iterator.DistanceToRwy = Length.FromMeters(0);
                (targetSpeedUnits, targetSpeed) = CalculateFmsSpeed(FmsPhaseType.APPROACH, Length.FromMeters(0), iterator.LastAlt, perfData, depArptElev, perfInit, startGribPoint);
                iterator.LastSpeed = targetSpeed;
                iterator.AlongTrackDistance = Length.FromMeters(0);

                // Calculate approach angle if it's not set
                if (iterator.ApchAngle == null)
                {
                    iterator.ApchAngle = endPoint.AngleConstraint > 0 ? Angle.FromDegrees(endPoint.AngleConstraint) : Angle.FromDegrees(3.0);
                }

                endPoint.VnavPoints.Add(
                    new FmsVnavPoint()
                    {
                        AlongTrackDistance = Length.FromMeters(0),
                        Alt = iterator.LastAlt,
                        Angle = iterator.ApchAngle,
                        Speed = targetSpeed,
                        SpeedUnits = targetSpeedUnits
                    }
                );
            }

            if (iterator.AlongTrackDistance <= Length.FromMeters(0))
            {
                // Check if we are rewinding
                if (iterator.ShouldRewind && !FmsVnavUtil.DoesPointMeetConstraints(endPoint, iterator.EarlyUpperAlt, iterator.EarlyLowerAlt, iterator.EarlySpeed))
                {
                    iterator.Index++;
                    iterator.DistanceToRwy -= leg.LegLength;
                    endPoint.VnavPoints = new List<FmsVnavPoint>();
                    iterator.AlongTrackDistance = Length.FromMeters(0);
                    return iterator;
                }

                // Not rewinding
                iterator.ShouldRewind = false;

                // Clear early restrictions if we're at earlyIndex
                if (iterator.Index == iterator.EarlySpeedI)
                {
                    iterator.EarlySpeedI = -2;
                    iterator.EarlySpeed = -1;
                }

                if (iterator.Index == iterator.EarlyLowerAltI)
                {
                    iterator.EarlyLowerAltI = -2;
                    iterator.EarlyLowerAlt = null;
                }

                if (iterator.Index == iterator.EarlyUpperAltI)
                {
                    iterator.EarlyUpperAltI = -2;
                    iterator.EarlyUpperAlt = null;
                }

                // Determine if constraints at end point are met
                if (endPoint.SpeedConstraint > 0 && (endPoint.SpeedConstraintType == ConstraintType.LESS || endPoint.SpeedConstraintType == ConstraintType.EXACT) && iterator.LastSpeed > endPoint.SpeedConstraint)
                {
                    iterator.EarlySpeed = (int)endPoint.SpeedConstraint;
                    iterator.EarlySpeedI = iterator.Index;

                    iterator.ShouldRewind = true;
                    iterator.Index++;
                    iterator.DistanceToRwy -= leg.LegLength;
                    endPoint.VnavPoints = new List<FmsVnavPoint>();
                    return iterator;
                }

                if (endPoint.UpperAltitudeConstraint > 0 && iterator.LastAlt.Feet > endPoint.UpperAltitudeConstraint)
                {
                    iterator.EarlyUpperAlt = Length.FromFeet(endPoint.UpperAltitudeConstraint);
                    iterator.EarlyUpperAltI = iterator.Index;

                    iterator.ShouldRewind = true;
                    iterator.Index++;
                    iterator.DistanceToRwy -= leg.LegLength;
                    endPoint.VnavPoints = new List<FmsVnavPoint>();
                    return iterator;
                }

                if (endPoint.LowerAltitudeConstraint > 0 && iterator.LastAlt.Feet < endPoint.LowerAltitudeConstraint)
                {
                    iterator.EarlyLowerAlt = Length.FromFeet(endPoint.LowerAltitudeConstraint);
                    iterator.EarlyLowerAltI = iterator.Index;

                    iterator.ShouldRewind = true;
                    iterator.Index++;
                    iterator.DistanceToRwy -= leg.LegLength;
                    endPoint.VnavPoints = new List<FmsVnavPoint>();
                    return iterator;
                }
            }

            // Calculate idle/approach descent angle
            Angle targetAngle = Angle.FromDegrees(0);
            if (iterator.DistanceToRwy < Length.FromNauticalMiles(15))
            {
                targetAngle = iterator.ApchAngle;
            } else
            {
                // Calculate idle descent angle
                targetAngle = Angle.FromDegrees(3.0); // TODO: Actually calculate this based off performance
            }

            // Get grib point for current position
            var gribPoint = new GribDataPoint(endPoint.Point.PointPosition.Lat, endPoint.Point.PointPosition.Lon, AtmosUtil.ISA_STD_PRES); // TODO: Change to actually use wind-uplink.

            // Calculate density altitude
            var densAlt = FmsVnavUtil.CalculateDensityAltitude(iterator.LastAlt, gribPoint);

            // Calculate current speed
            if (iterator.DistanceToRwy < Length.FromNauticalMiles(15))
            {
                (targetSpeedUnits, targetSpeed) = CalculateFmsSpeed(FmsPhaseType.APPROACH, iterator.DistanceToRwy, iterator.LastAlt, perfData, depArptElev, perfInit, gribPoint);
            } else
            {
                (targetSpeedUnits, targetSpeed) = CalculateFmsSpeed(FmsPhaseType.DESCENT, iterator.DistanceToRwy, iterator.LastAlt, perfData, depArptElev, perfInit, gribPoint);
            }

            // Convert mach to knots if required
            int targetSpeedKts = (int)GetKnotsSpeed(targetSpeedUnits, targetSpeed, iterator.LastAlt, gribPoint);

            // Check if prior restriction is less than target speed
            if (iterator.EarlySpeed > 0 && iterator.EarlySpeed < targetSpeedKts)
            {
                targetSpeedKts = iterator.EarlySpeed;
                targetSpeed = targetSpeedKts;
                targetSpeedUnits = McpSpeedUnitsType.KNOTS;
            }

            // Figure out if decel point is required
            if (iterator.AlongTrackDistance <= Length.FromMeters(0))
            {
                var speedConstraint = leg.EndPoint.SpeedConstraint;
                var speedConstraintType = leg.EndPoint.SpeedConstraintType;
                if (speedConstraint > 0)
                {
                    if ((speedConstraintType == ConstraintType.EXACT || speedConstraintType == ConstraintType.LESS) && speedConstraint < targetSpeedKts)
                    {
                        // Calculate deceleration distance
                        iterator.LastSpeed = Convert.ToInt32(speedConstraint);
                        iterator.LaterDecelSpeed = Convert.ToInt32(speedConstraint);
                        iterator.LaterDecelLength = FmsVnavUtil.CalculateDecelLength(iterator.LastSpeed, targetSpeedKts, iterator.LastAlt, densAlt, mass_kg, leg.FinalTrueCourse, perfData, gribPoint);
                    }
                }
            }
            if (iterator.AlongTrackDistance >= leg.LegLength)
            {
                // Increment to next leg, but check alt constraints
                if (iterator.LastAlt > iterator.EarlyUpperAlt)
                {
                    iterator.LastAlt = iterator.EarlyUpperAlt;

                    // Add level off fix
                    var lastVnav = endPoint.VnavPoints[endPoint.VnavPoints.Count - 1];

                    var levelOffDist = FmsVnavUtil.CalculateDistanceForAltitude(lastVnav.Alt, iterator.EarlyUpperAlt, lastVnav.Angle) + lastVnav.AlongTrackDistance;

                    endPoint.VnavPoints.Add(new FmsVnavPoint
                    {
                        AlongTrackDistance = levelOffDist,
                        Alt = iterator.LastAlt,
                        Angle = Angle.FromRadians(0),
                        Speed = targetSpeed,
                        SpeedUnits = targetSpeedUnits
                    });
                } else if (iterator.LastAlt < iterator.EarlyLowerAlt)
                {
                    iterator.LastAlt = iterator.EarlyLowerAlt;

                    // Modify last VNAV angle to force it to meet constraint
                    var lastVnav = endPoint.VnavPoints[endPoint.VnavPoints.Count - 1];
                    lastVnav.Angle = Angle.FromRadians(Math.Atan2((iterator.LastAlt - lastVnav.Alt).Meters, (leg.LegLength - lastVnav.AlongTrackDistance).Meters));
                }

                iterator.AlongTrackDistance = Length.FromMeters(0);
                iterator.Index--;
                iterator.DistanceToRwy += leg.LegLength;
                iterator.LastSpeed = targetSpeed;
                return iterator;
            }

            // Check for previous decel point
            if (iterator.LaterDecelLength != null)
            {
                // Add initial point
                endPoint.VnavPoints.Add(new FmsVnavPoint
                {
                    AlongTrackDistance = iterator.AlongTrackDistance,
                    Alt = iterator.LastAlt,
                    Angle = Angle.FromRadians(0),
                    Speed = iterator.LaterDecelSpeed,
                    SpeedUnits = McpSpeedUnitsType.KNOTS
                });

                // Check if deceleration point doesn't fit within the current leg
                if (iterator.LaterDecelLength + iterator.AlongTrackDistance > leg.LegLength)
                {
                    iterator.LaterDecelLength -= (leg.LegLength - iterator.AlongTrackDistance);
                    iterator.AlongTrackDistance = leg.LegLength;
                    return iterator;
                }

                iterator.AlongTrackDistance += iterator.LaterDecelLength;
                iterator.LaterDecelLength = null;
                iterator.LaterDecelSpeed = -1;
                return iterator;
            }

            // Calculate new last alt
            var newLastAlt = FmsVnavUtil.CalculateStartAltitude(iterator.LastAlt, leg.LegLength - iterator.AlongTrackDistance, targetAngle);

            // Check limit alt
            Length limitAlt = Length.FromFeet(perfInit.LimitAlt);
            if (newLastAlt >= limitAlt && iterator.LastAlt < limitAlt && !iterator.LimitCrossed)
            {
                iterator.LimitCrossed = true;

                // Calculate FMS speed above limit alt
                (var aboveLimitSpeedUnits, var aboveLimitSpeed) = CalculateFmsSpeed(FmsPhaseType.DESCENT, iterator.DistanceToRwy, newLastAlt, perfData, depArptElev, perfInit, gribPoint);
                int aboveLimitSpeedKts = (int)GetKnotsSpeed(aboveLimitSpeedUnits, aboveLimitSpeed, limitAlt, gribPoint);

                if (iterator.EarlySpeed > 0 && iterator.EarlySpeed < aboveLimitSpeedKts)
                {
                    aboveLimitSpeed = iterator.EarlySpeed;
                    aboveLimitSpeedUnits = McpSpeedUnitsType.KNOTS;
                    aboveLimitSpeedKts = iterator.EarlySpeed;
                }

                // If there is a speed difference crossing the limit alt, create slow down point
                if (perfInit.LimitSpeed < aboveLimitSpeedKts)
                {
                    var todDist = FmsVnavUtil.CalculateDistanceForAltitude(iterator.LastAlt, limitAlt, targetAngle);

                    iterator.AlongTrackDistance += todDist;
                    iterator.LastSpeed = perfInit.LimitSpeed;
                    iterator.LaterDecelSpeed = perfInit.LimitSpeed;

                    densAlt = FmsVnavUtil.CalculateDensityAltitude(limitAlt, gribPoint);
                    var decelDist = FmsVnavUtil.CalculateDecelLength(aboveLimitSpeedKts, targetSpeedKts, limitAlt, densAlt, mass_kg, leg.FinalTrueCourse, perfData, gribPoint);
                    iterator.LaterDecelLength = decelDist;

                    return iterator;
                }
            }

            iterator.LastAlt = newLastAlt;
            iterator.AlongTrackDistance = leg.LegLength;

            return iterator;
        }
    }
}
