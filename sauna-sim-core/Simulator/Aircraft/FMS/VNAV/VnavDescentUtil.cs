using AviationCalcUtilNet.Atmos.Grib;
using AviationCalcUtilNet.Atmos;
using AviationCalcUtilNet.Aviation;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.Units;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;
using SaunaSim.Core.Simulator.Aircraft.Performance;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.VNAV
{
    public static class VnavDescentUtil
    {
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

            return Length.FromMeters((Math.Pow(finalVelocity.MetersPerSecond, 2) - Math.Pow(initialVelocity.MetersPerSecond, 2)) / (double)Acceleration.FromKnotsPerSecond(decelRate * 2)); // Vf^2 - Vi^2 / (2a)
        }

        private static FmsVnavLegIterator GoToNextVnavDescentLeg(IRouteLeg curLeg, IRouteLeg nextLeg, FmsVnavLegIterator iterator)
        {
            if (curLeg == null || curLeg.EndPoint == null || curLeg.LegLength <= Length.FromMeters(0))
            {
                throw new ArgumentException("curLeg was not a valid VNAV leg!");
            }

            if (nextLeg != null && (nextLeg.EndPoint == null || nextLeg.LegLength <= Length.FromMeters(0)))
            {
                throw new ArgumentException("nextLeg was not a valid VNAV leg!");
            }

            if (curLeg.EndPoint.VnavPoints == null || curLeg.EndPoint.VnavPoints.Count < 1)
            {
                throw new ArgumentException("curLeg's VnavPoints must have at least 1 point!");
            }            

            // Clean up iterator
            iterator.DecelDist = null;
            iterator.DecelSpeed = -1;

            // Remove last VNAV point
            curLeg.EndPoint.VnavPoints.RemoveAt(curLeg.EndPoint.VnavPoints.Count - 1);

            // If current leg still has VNAV Points
            if (curLeg.EndPoint.VnavPoints.Count > 0)
            {
                iterator.AlongTrackDistance = curLeg.EndPoint.VnavPoints[curLeg.EndPoint.VnavPoints.Count - 1].AlongTrackDistance;
                return iterator;
            }

            if (nextLeg == null || nextLeg.EndPoint.VnavPoints == null || nextLeg.EndPoint.VnavPoints.Count < 1)
            {
                throw new IndexOutOfRangeException("nextLeg did not have any VNAV Points or is not a valid leg!");
            }

            // Move to next leg
            iterator.Index++;
            iterator.DistanceToRwy -= nextLeg.LegLength;
            iterator.AlongTrackDistance = nextLeg.EndPoint.VnavPoints[nextLeg.EndPoint.VnavPoints.Count - 1].AlongTrackDistance;

            return iterator;
        }

        private static (FmsVnavPoint lastVnavPoint, FmsVnavLegIterator iterator, Length distance) GetLastVnavDescentPoint(IRouteLeg curLeg, IRouteLeg nextLeg, FmsVnavLegIterator iterator, PerfData perfData, PerfInit perfInit, double mass_kg, Length depArptElev)
        {
            Length distance = Length.FromMeters(0);

            // ---  2. Get Last VNAV point
            var curEndPoint = curLeg.EndPoint;
            FmsVnavPoint? lastVnavPoint = null;
            if (curEndPoint.VnavPoints != null && curEndPoint.VnavPoints.Count > 0)
            {
                lastVnavPoint = curEndPoint.VnavPoints[curEndPoint.VnavPoints.Count - 1];
                distance = iterator.AlongTrackDistance - lastVnavPoint.Value.AlongTrackDistance;
            } else if (nextLeg != null && nextLeg.EndPoint.VnavPoints != null && nextLeg.EndPoint.VnavPoints.Count > 0)
            {
                lastVnavPoint = nextLeg.EndPoint.VnavPoints[nextLeg.EndPoint.VnavPoints.Count - 1];
                distance = nextLeg.LegLength - lastVnavPoint.Value.AlongTrackDistance;
            }

            // ---  3. Initialize first leg
            if (!lastVnavPoint.HasValue)
            {
                var alt = curEndPoint.LowerAltitudeConstraint > 0 ? Length.FromFeet(curEndPoint.LowerAltitudeConstraint) : Length.FromMeters(0);
                var startGribPoint = FmsVnavUtil.GetGribPointForLeg(curLeg, alt);
                iterator.DistanceToRwy = Length.FromMeters(0);
                (var targetSpeedUnits, var targetSpeed) = FmsVnavUtil.CalculateFmsSpeed(FmsPhaseType.APPROACH, Length.FromMeters(0), alt, perfData, depArptElev, perfInit, startGribPoint);
                iterator.AlongTrackDistance = Length.FromMeters(0);

                // Calculate approach angle
                iterator.ApchAngle = curEndPoint.AngleConstraint > 0 ? Angle.FromDegrees(curEndPoint.AngleConstraint) : Angle.FromDegrees(3.0);

                // Set last VNAV Point
                lastVnavPoint = new FmsVnavPoint()
                {
                    AlongTrackDistance = iterator.AlongTrackDistance,
                    Alt = alt,
                    Angle = iterator.ApchAngle,
                    Speed = targetSpeed,
                    SpeedUnits = targetSpeedUnits,
                    CmdSpeed = targetSpeed,
                    CmdSpeedUnits = targetSpeedUnits,
                };
                curEndPoint.VnavPoints.Add(lastVnavPoint.Value);
            }

            return (lastVnavPoint.Value, iterator, distance);
        }

        /// <summary>
        /// Completes an iteration for a leg for a VNAV descent calculation.
        /// </summary>
        /// <param name="curLeg">Current Leg. Must contain an endpoint and a leg length.</param>
        /// <param name="nextLeg">Next Leg (nullable). Must contain an endpoint and a leg length.</param>
        /// <param name="iterator">Iterator object</param>
        /// <returns>Modified iterator object</returns>
        /// <exception cref="ArgumentException">If the provided legs are not valid VNAV-able legs</exception>
        public static FmsVnavLegIterator ProcessLegForDescent(IRouteLeg curLeg, IRouteLeg nextLeg, FmsVnavLegIterator iterator, PerfData perfData, PerfInit perfInit, double mass_kg, Length depArptElev)
        {
            // ---  Input validation
            if (curLeg == null || curLeg.EndPoint == null || curLeg.LegLength <= Length.FromMeters(0))
            {
                throw new ArgumentException("curLeg was not a valid VNAV leg!");
            }

            if (nextLeg != null && (nextLeg.EndPoint == null || nextLeg.LegLength <= Length.FromMeters(0)))
            {
                throw new ArgumentException("nextLeg was not a valid VNAV leg!");
            }

            // ---  Get Last VNAV Point
            FmsVnavPoint lastVnavPoint;
            Length lastVnavPointDist;
            (lastVnavPoint, iterator, lastVnavPointDist) = GetLastVnavDescentPoint(curLeg, nextLeg, iterator, perfData, perfInit, mass_kg, depArptElev);

            // ---  Check for constraint search
            var gribPoint = FmsVnavUtil.GetGribPointForLeg(curLeg, lastVnavPoint.Alt);

            if (iterator.EarlySpeedSearch)
            {
                if (FmsVnavUtil.GetKnotsSpeed(lastVnavPoint.CmdSpeedUnits, lastVnavPoint.CmdSpeed, lastVnavPoint.Alt, gribPoint) <= iterator.EarlySpeed)
                {
                    iterator.EarlySpeedSearch = false;
                    return iterator;
                }

                try
                {
                    return GoToNextVnavDescentLeg(curLeg, nextLeg, iterator);
                } catch (IndexOutOfRangeException)
                {
                    // Give up searching for speed and force last vnav point to speed
                    iterator.EarlySpeedSearch = false;
                    lastVnavPoint.CmdSpeed = iterator.EarlySpeed;
                    lastVnavPoint.CmdSpeedUnits = Autopilot.McpSpeedUnitsType.KNOTS;
                    lastVnavPoint.Speed = iterator.EarlySpeed;
                    lastVnavPoint.SpeedUnits = Autopilot.McpSpeedUnitsType.KNOTS;
                    return iterator;
                }
            }

            if (iterator.EarlyUpperAltSearch)
            {
                if (lastVnavPoint.Alt <= iterator.EarlyUpperAlt)
                {
                    iterator.EarlyUpperAltSearch = false;
                    return iterator;
                }
                try
                {
                    return GoToNextVnavDescentLeg(curLeg, nextLeg, iterator);
                } catch (IndexOutOfRangeException)
                {
                    // Give up searching for speed
                    iterator.EarlyUpperAltSearch = false;
                    lastVnavPoint.Alt = iterator.EarlyUpperAlt;
                    return iterator;
                }
            }

            // ---  Clear earlier constraints
            if (iterator.AlongTrackDistance <= Length.FromMeters(0))
            {
                if (iterator.Index == iterator.EarlySpeedIndex)
                {
                    iterator.EarlySpeed = -1;
                    iterator.EarlySpeedIndex = -1;
                }

                if (iterator.Index == iterator.EarlyUpperAltIndex)
                {
                    iterator.EarlyUpperAltIndex = -1;
                    iterator.EarlyUpperAlt = null;
                }
            }

            // ---  Move to previous leg
            if (iterator.AlongTrackDistance >= curLeg.LegLength)
            {
                iterator.Index--;
                iterator.AlongTrackDistance = Length.FromMeters(0);
                iterator.DistanceToRwy += curLeg.LegLength;
                return iterator;
            }

            // ---  Calculate Parameters
            // Set current phase
            var currentPhase = FmsPhaseType.DESCENT;
            if (iterator.DistanceToRwy + iterator.AlongTrackDistance <= Length.FromNauticalMiles(15))
            {
                currentPhase = FmsPhaseType.APPROACH;
            }

            // Calculate current alt and grib point
            var curAlt = FmsVnavUtil.CalculateStartAltitude(lastVnavPoint.Alt, lastVnavPointDist, lastVnavPoint.Angle);
            gribPoint = FmsVnavUtil.GetGribPointForLeg(curLeg, curAlt);
            var curDensAlt = FmsVnavUtil.CalculateDensityAltitude(curAlt, gribPoint);

            // Calculate target speed
            (var targetSpeedUnits, var targetSpeed) = FmsVnavUtil.CalculateFmsSpeed(currentPhase, iterator.DistanceToRwy + iterator.AlongTrackDistance, curAlt, perfData, depArptElev, perfInit, gribPoint);

            // Check for decel speed
            if (iterator.DecelSpeed > 0)
            {
                var targetSpeedInKts = FmsVnavUtil.GetKnotsSpeed(targetSpeedUnits, targetSpeed, curAlt, gribPoint);
                iterator.DecelDist = CalculateDecelLength(iterator.DecelSpeed, Convert.ToInt32(Math.Round(targetSpeedInKts, MidpointRounding.AwayFromZero)), curAlt, curDensAlt, mass_kg, curLeg.FinalTrueCourse, perfData, gribPoint);
                targetSpeed = iterator.DecelSpeed;
                targetSpeedUnits = Autopilot.McpSpeedUnitsType.KNOTS;
                iterator.DecelSpeed = -1;
            }

            // ---  Handle Constraints
            if (iterator.AlongTrackDistance <= Length.FromMeters(0))
            {
                // Does speed constraint exist
                if (currentPhase == FmsPhaseType.DESCENT && 
                    (curLeg.EndPoint.SpeedConstraintType == ConstraintType.LESS || curLeg.EndPoint.SpeedConstraintType == ConstraintType.EXACT) && 
                    curLeg.EndPoint.SpeedConstraint > 0)
                {
                    // Was speed constraint violated
                    if (lastVnavPoint.CmdSpeedUnits == Autopilot.McpSpeedUnitsType.MACH || lastVnavPoint.CmdSpeed > curLeg.EndPoint.SpeedConstraint)
                    {
                        // Go back to last time speed constraint was followed
                        try
                        {
                            return GoToNextVnavDescentLeg(curLeg, nextLeg, iterator);
                        } catch (IndexOutOfRangeException) {

                        }
                    }
                }
            }

            /*
             *  If AlongTrackDistance == 0
		            // Handle Speed Constraint
		            If Speed Constraint Exists And DistanceToRwy > 15
			            If LastVnavPt Speed > Speed Constraint
				            // Go back to last point speed constraint was met
				            EarlySpeed = Speed Constraint
				            EarlySpeedI = Index
				            EarlySpeedSearch = True
				            return GoBackOne(iterator)
			            If TargetSpeed > SpeedConstraint
				            // Calculate Decel length
				            DecelDist = CalculateDecelDist
				            TargetSpeed = Speed Constraint
		            // Handle Upper Alt Constraint
		            If UpperAltConstraint exists And CurAlt > UpperAltConstraint
			            EarlyUpperAlt  = UpperAltConstraint
			            EarlyUpperAltI = Index
			            EarlyUpperAltSearch = True
			            return GoBackOne(iterator)

	            // Calculate Angle
	            If DecelDist != null || EarlyUpperAlt >= CurAlt
		            TargetAngle = 0
	            Else If DistanceToRwy < 15
		            TargetAngle = ApchAngle
	            Else
		            TargetAngle = Idle Des Angle
		
	            // Add Vnav point
	            Add Vnav Point
		            AlongTrackDistance = AlongTrackDistance
		            Alt = CurAlt
		            Speed = TargetSpeed
		            SpeedUnits = TargetSpeedUnits
		            CmdSpeed = DecelDist != null ? lastVnavPt.CmdSpeed : TargetSpeed
		            CmdSpeedUnits = DecelDist != null ? lastVnavPt.CmdSpeedUnits : TargetSpeedUnits
	
	            // Adjust iterator
	            NewAlongTrack = LegLength
	
	            If DecelDist != null
		            If DecelDist + AlongTrackDistance < LegLength
			            NewAlongTrack = AlongTrackDistance + DecelDist
			            DecelDist = null
		            Else
			            DecelDist -= (LegLength - AlongTrackDistance)
		
	            Else If TargetAngle > 0
		            // Calculate alt at start of leg
		            StartAlt = CalculateStartAlt			
		
		            If StartAlt >= LimitAlt and CurAlt < LimitAlt and CurSpeed > LimitSpeed
			            NewAlongTrack = Point where we reach LimitAlt
			            DecelSpeed = LimitSpeed
		            If StartAlt > EarlyUpperAlt And EarlyUpperAlt < LimitAlt
			            NewAlongTrack = Point where we reach EarlyUpperAlt
			
		            AlongTrackDistance = NewAlongTrack
		
	            // Check for passing 15nmi
	            If DistanceToRwy + AlongTrackDistance < 15 And DistanceToRwy + NewAlongTrack > 15
		            AlongTrackDistance = 15nmi - DistanceToRwy
		            DecelSpeed = TargetSpeed
	            return iterator
            */
        }
    }
}
