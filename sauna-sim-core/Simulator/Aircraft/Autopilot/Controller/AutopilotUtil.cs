using System;
using AviationCalcUtilNet.Aviation;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Math;
using AviationCalcUtilNet.MathTools;
using AviationCalcUtilNet.Physics;
using AviationCalcUtilNet.Units;
using SaunaSim.Core.Simulator.Aircraft.Performance;

namespace SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller
{
    public static class AutopilotUtil
    {
        // Roll
        public const double ROLL_TIME = 0.5;
        public readonly static AngularVelocity ROLL_RATE_MAX = AngularVelocity.FromDegreesPerSecond(5.0);
        public readonly static Angle ROLL_LIMIT = Angle.FromDegrees(25.0);
        public readonly static AngularVelocity HDG_MAX_RATE = AngularVelocity.FromDegreesPerSecond(3.0);
        public const double ROLL_TIME_BUFFER = 0.1;

        // LNAV
        public readonly static Angle MAX_INTC_ANGLE = Angle.FromDegrees(45);
        public readonly static Length MIN_XTK_M = Length.FromMeters(3);
        public readonly static Angle MAX_CRS_DEV = Angle.FromDegrees(0.1);
        public readonly static Length MAX_INTC_XTK_M = Length.FromMeters(1852.0);
        public const double RADIUS_BUFFER_MULT = 1.1;

        public readonly static Angle LNAV_MAX_COMPUTE_BANK = Angle.FromDegrees(25);
        public readonly static Angle LNAV_MAX_BANK = Angle.FromDegrees(30);
        public readonly static Length TERM_MAX_TURN_LEAD = Length.FromNauticalMiles(10);
        public readonly static Length TERM_ALT_CROSSOVER = Length.FromFeet(20000);
        public readonly static Length ENRTE_MAX_TURN_LEAD = Length.FromNauticalMiles(20);

        // Pitch
        public const double PITCH_TIME = 0.5;
        public readonly static Angle PITCH_LIMIT_MAX = Angle.FromDegrees(30.0);
        public readonly static Angle PITCH_LIMIT_MIN = Angle.FromDegrees(-15.0);
        public const double PITCH_TIME_BUFFER = 0.1;
        public readonly static AngularVelocity PITCH_RATE_NORM_MAX = AngularVelocity.FromDegreesPerSecond(1.0);
        public readonly static AngularVelocity PITCH_RATE_TOLDG_MAX = AngularVelocity.FromDegreesPerSecond(3.0);

        public readonly static double PITCH_NORM_G_LIMIT = 1.1;
        

        // Thrust
        public const double THRUST_TIME = 0.5;
        public const double THRUST_RATE_NORM_MAX = 5.0;
        public const double THRUST_RATE_TOLDG_MAX = 30.0;
        public const double THRUST_TIME_BUFFER = 0.1;

        /// <summary>
        /// Calculates required rate.
        /// </summary>
        /// <param name="demandedValue">Demanded Value (units x)</param>
        /// <param name="measuredValue">Demanded Value (units x)</param>
        /// <param name="corrTime">Correction Time (s)</param>
        /// <param name="rateMax">Maximum Rate (units x/s)</param>
        /// <param name="intervalMs">Update Interval Time (ms)</param>
        /// <returns>Required Rate (units x/s)</returns>
        public static double CalculateRate(double demandedValue, double measuredValue, double corrTime, double rateMax, int intervalMs)
        {
            // Set minimum correction time
            if (corrTime < 2 * intervalMs / 1000.0)
            {
                corrTime = 2 * intervalMs / 1000.0;
            }

            // Calculate value delta
            double valueDelta = demandedValue - measuredValue;

            // Calculate Rate
            double requiredRate = valueDelta / corrTime;

            // Limit Rate
            if (rateMax > 0 && requiredRate < -rateMax)
            {
                requiredRate = -rateMax;
            } else if (rateMax > 0 && requiredRate > rateMax)
            {
                requiredRate = rateMax;
            }

            return requiredRate;
        }

        /// <summary>
        /// Calculates thrust movement rate.
        /// </summary>
        /// <param name="demandedThrust">Demanded Thrust Setting (%: 0-100)</param>
        /// <param name="measuredThrust">Measured Thrust Setting (%: 0-100)</param>
        /// <param name="intervalMs">Update Interval Time (ms)</param>
        /// <returns>Thrust Movement Rate (%/s)</returns>
        public static double CalculateThrustRate(double demandedThrust, double measuredThrust, int intervalMs)
        {
            return CalculateRate(demandedThrust, measuredThrust, THRUST_TIME, THRUST_RATE_NORM_MAX, intervalMs);
        }

        /// <summary>
        /// Calculates roll rate.
        /// </summary>
        /// <param name="demandedRollAngle">Demanded Roll Angle (degrees)</param>
        /// <param name="measuredRollAngle">Measured Roll Angle (degrees)</param>
        /// <param name="intervalMs">Update Interval Time (ms)</param>
        /// <returns>Roll Rate (degrees/sec)</returns>
        public static AngularVelocity CalculateRollRate(Angle demandedRollAngle, Angle measuredRollAngle, int intervalMs)
        {
            return (AngularVelocity)CalculateRate((double)demandedRollAngle, (double)measuredRollAngle, ROLL_TIME, (double)ROLL_RATE_MAX, intervalMs);
        }

        /// <summary>
        /// Calculates pitch rate.
        /// </summary>
        /// <param name="demandedPitchAngle">Demanded Pitch Angle (degrees)</param>
        /// <param name="measuredPitchAngle">Measured Pitch Angle (degrees)</param>
        /// <param name="intervalMs">Update Interval Time (ms)</param>
        /// <returns>Pitch Rate (degrees/sec)</returns>
        public static AngularVelocity CalculatePitchRate(Angle demandedPitchAngle, Angle measuredPitchAngle, int intervalMs)
        {
            return (AngularVelocity)CalculateRate((double)demandedPitchAngle, (double)measuredPitchAngle, PITCH_TIME, (double)PITCH_RATE_NORM_MAX, intervalMs);
        }

        /// <summary>
        /// Calculates a demanded input to reach a desired target.
        /// </summary>
        /// <param name="deltaToTarget">How far the target is.</param>
        /// <param name="curInput">Current input</param>
        /// <param name="maxInputLimit">Maximum Input</param>
        /// <param name="minInputLimit">Minimum Input</param>
        /// <param name="inputRateFunction">Function that returns the rate to get from one input to another</param>
        /// <param name="targetRateFunction">Function that returns the target rate for an input.</param>
        /// <param name="zeroTargetRateInput">Input that results in zero target rate.</param>
        /// <param name="inputTimeBuffer">Time contingency</param>
        /// <returns></returns>
        public static (double demandedInput, double timeToTarget) CalculateDemandedInput(double deltaToTarget, double curInput, double maxInputLimit, double minInputLimit,
            Func<double, double, double> inputRateFunction, Func<double, double> targetRateFunction, double zeroTargetRateInput, double inputTimeBuffer)
        {
            // Create a piecewise function with the following:
            // Get from current to max input
            // Remain at max input until it's time to return to zero input
            // Get from max input to zero input.

            // Make sure zero target rate is within bounds
            if (zeroTargetRateInput < minInputLimit)
            {
                zeroTargetRateInput = minInputLimit;
            } else if (zeroTargetRateInput > maxInputLimit)
            {
                zeroTargetRateInput = maxInputLimit;
            }

            // Make sure cur input is within bounds
            if (curInput < minInputLimit)
            {
                curInput = minInputLimit;
            } else if (curInput > maxInputLimit)
            {
                curInput = maxInputLimit;
            }

            // Figure out time to get to 0 from max input and to get to max input from current input
            double maxInput = deltaToTarget < 0 ? minInputLimit : maxInputLimit;

            double curInputTargetRate = targetRateFunction(curInput);
            double maxInputTargetRate = targetRateFunction(maxInput);
            double inputOutTargetDelta = 0;
            double inputInTargetDelta = 0;
            double maxInputIn_t = 0;
            double maxInputOut_t = 0;

            // Calculate time and target delta to get from maximum input to zero rate input if there is a difference
            if (Math.Abs(zeroTargetRateInput - maxInput) > double.Epsilon)
            {
                double maxInputOutRate = inputRateFunction(zeroTargetRateInput, maxInput);
                maxInputOut_t = Math.Abs(Math.Abs(maxInput - zeroTargetRateInput) / maxInputOutRate) * (1 + inputTimeBuffer);
                double inputOutTarget_a = PhysicsUtil.KinematicsAcceleration(maxInputTargetRate, 0, maxInputOut_t);
                inputOutTargetDelta = PhysicsUtil.KinematicsDisplacement2(maxInputTargetRate, inputOutTarget_a, maxInputOut_t);
            }

            // Calculate time and target delta to get from current input to maximum input if there is a difference
            if (Math.Abs(curInput - maxInput) > double.Epsilon)
            {
                double maxInputInRate = inputRateFunction(maxInput, curInput);
                maxInputIn_t = Math.Abs(Math.Abs(curInput - maxInput) / maxInputInRate) * (1 + inputTimeBuffer);
                double inputInTarget_a = PhysicsUtil.KinematicsAcceleration(curInputTargetRate, maxInputTargetRate, maxInputIn_t);
                inputInTargetDelta = PhysicsUtil.KinematicsDisplacement2(curInputTargetRate, inputInTarget_a, maxInputIn_t);
            }

            // Return max if we're going in the wrong direction.
            if (inputInTargetDelta > 0 && deltaToTarget < 0 || inputInTargetDelta < 0 && deltaToTarget > 0)
            {
                double remainingDelta = deltaToTarget > 0 ?
                    deltaToTarget - inputInTargetDelta - inputOutTargetDelta :
                    deltaToTarget + inputInTargetDelta + inputOutTargetDelta;
                double remainingTime = Math.Abs(maxInputTargetRate) > double.Epsilon ? Math.Abs(remainingDelta) / Math.Abs(maxInputTargetRate) : 0;
                double totalTime = maxInputIn_t + maxInputOut_t + remainingTime;
                return (maxInput, totalTime);
            }

            // Create Equations
            Polynomial p1 = MathUtil.CreateLineEquation(0, curInput, inputInTargetDelta, maxInput) ?? new Polynomial(new double[] { 0, double.PositiveInfinity });
            Polynomial p2 = MathUtil.CreateLineEquation(deltaToTarget - inputOutTargetDelta, maxInput, deltaToTarget, zeroTargetRateInput) ?? new Polynomial(new double[] { 0, double.PositiveInfinity });
            (double m3, double b3) = (0, maxInput);
            /*Console.WriteLine($"f(x) = {m1}x+{b1}");
            Console.WriteLine($"g(x) = {m2}x+{b2}");
            Console.WriteLine($"h(x) = {m3}x+{b3}");
            Console.WriteLine($"x = {deltaToTarget}");*/

            (double inputOutIntersectionX, double inputOutIntersectionY) = MathUtil.Find2LinesIntersection(p2.Coefficients[1], p2.Coefficients[0], m3, b3);
            (double midPointTargetDelta, double midPointInput) = MathUtil.Find2LinesIntersection(p1.Coefficients[1], p1.Coefficients[0], p2.Coefficients[1], p2.Coefficients[0]);

            // If midpoint is above max input
            if ((deltaToTarget >= 0 && midPointInput > maxInput) || ((deltaToTarget <= 0 && midPointInput < maxInput)))
            {
                midPointTargetDelta = inputOutIntersectionX;
                midPointInput = inputOutIntersectionY;
            }

            // If we're on target
            if (Math.Abs(deltaToTarget) <= double.Epsilon)
            {
                return (zeroTargetRateInput, 0);
            }

            // Figure out the desired input value
            if (midPointTargetDelta <= 0 && deltaToTarget > 0 || midPointTargetDelta >= 0 && deltaToTarget < 0)
            {
                double rollOutRate = inputRateFunction(zeroTargetRateInput, curInput);
                double rollOut_t = Math.Abs(Math.Abs(curInput - zeroTargetRateInput) / rollOutRate) * (1 + inputTimeBuffer);
                return (zeroTargetRateInput, rollOut_t);
            }

            if (Math.Abs(midPointInput) >= Math.Abs(maxInput))
            {
                double remainingDelta = deltaToTarget > 0 ?
                    deltaToTarget - inputInTargetDelta - inputOutTargetDelta :
                    deltaToTarget + inputInTargetDelta + inputOutTargetDelta;
                double remainingTime = Math.Abs(maxInputTargetRate) > double.Epsilon ? Math.Abs(remainingDelta) / Math.Abs(maxInputTargetRate) : 0;
                double totalTime = maxInputIn_t + maxInputOut_t + remainingTime;
                return (maxInput, totalTime);
            }

            double mpRollInRate = inputRateFunction(midPointInput, curInput);
            double mpRollOutRate = inputRateFunction(zeroTargetRateInput, midPointInput);
            double mpRollIn_t = Math.Abs(Math.Abs(midPointInput - curInput) / mpRollInRate) * (1 + inputTimeBuffer);
            double mpRollOut_t = Math.Abs(Math.Abs(zeroTargetRateInput - midPointInput) / mpRollOutRate) * (1 + inputTimeBuffer);

            return (midPointInput, mpRollIn_t + mpRollOut_t);
        }

        /// <summary>
        /// Calculate the required roll for a turn.
        /// </summary>
        /// <param name="turnAmt">Amount to turn (degrees)</param>
        /// <param name="curRoll">Current roll (degrees)</param>
        /// <param name="groundSpeed">Current ground speed (knots)</param>
        /// <param name="intervalMs">Update Interval Time (ms)</param>
        /// <returns>Desired roll (degrees)</returns>
        public static Angle CalculateDemandedRollForTurn(Angle turnAmt, Angle curRoll, Velocity groundSpeed, int intervalMs)
        {
            return CalculateDemandedRollForTurn(turnAmt, curRoll, (Angle)0, groundSpeed, intervalMs);
        }

        /// <summary>
        /// Calculate the required roll for a turn with a potential arc.
        /// </summary>
        /// <param name="turnAmt">Amount to turn (degrees)</param>
        /// <param name="curRoll">Current roll (degrees)</param>
        /// <param name="zeroRoll">Zero roll (degrees)</param>
        /// <param name="groundSpeed">Current ground speed (knots)</param>
        /// <param name="intervalMs">Update Interval Time (ms)</param>
        /// <returns>Desired roll (degrees)</returns>
        public static Angle CalculateDemandedRollForTurn(Angle turnAmt, Angle curRoll, Angle zeroRoll, Velocity groundSpeed, int intervalMs)
        {
            Angle maxRoll = AviationUtil.CalculateMaxBankAngle(groundSpeed, ROLL_LIMIT, HDG_MAX_RATE);
            return (Angle)CalculateDemandedInput(
                (double)turnAmt,
                (double)curRoll,
                (double)maxRoll,
                (double)-maxRoll,
                (double demandedRoll, double measuredRoll) => (double)CalculateRollRate((Angle)demandedRoll, (Angle)measuredRoll, intervalMs),
                (double roll) => GeoUtil.EARTH_GRAVITY.Value() * Math.Tan(roll) / groundSpeed.Value(),
                (double)zeroRoll,
                ROLL_TIME_BUFFER
            ).demandedInput;
        }

        public static double CalculateDemandedThrottleForSpeed(Velocity speedDelta, double curThrottle, double thrustForZeroAccel, Func<double, double> thrustToSpeedAccelFunction, int intervalMs)
        {
            return CalculateDemandedInput(
                (double)-speedDelta,
                curThrottle,
                100,
                0,
                (demandedThrottle, measuredThrottle) => CalculateThrustRate(demandedThrottle, measuredThrottle, intervalMs),
                thrustToSpeedAccelFunction,
                thrustForZeroAccel,
                THRUST_TIME_BUFFER
            ).demandedInput;
        }        

        public static Angle CalculateDemandedPitchForSpeed(Velocity speedDelta, Angle curPitch, Angle pitchForZeroAccel, Angle maxPitch, Angle minPitch, Func<Angle, double> pitchToSpeedAccelFunction,
            int intervalMs)
        {
            double inputToTargetRateFunc(double pitch) => -pitchToSpeedAccelFunction((Angle)pitch);
            return (Angle)CalculateDemandedInput(
                (double)speedDelta,
                (double)curPitch,
                (double)maxPitch,
                (double)minPitch,
                (demandedPitch, measuredPitch) => (double)CalculatePitchRate((Angle)demandedPitch, (Angle)measuredPitch, intervalMs),
                inputToTargetRateFunc,
                (double)pitchForZeroAccel,
                PITCH_TIME_BUFFER
            ).demandedInput;
        }

        public static Angle CalculateDemandedPitchForAltitude(Length altDelta, Angle curPitch, Angle pitchForZeroVs, Angle pitchIdle, Angle pitchMax, Func<Angle, Velocity> pitchToVsFunction, int intervalMs)
        {
            return (Angle)CalculateDemandedInput(
                (double)-altDelta,
                (double)curPitch,
                (double)pitchMax,
                (double)pitchIdle,
                (demandedPitch, measuredPitch) => (double)CalculatePitchRate((Angle)demandedPitch, (Angle)measuredPitch, intervalMs),
                (double pitch) => (double) pitchToVsFunction((Angle) pitch),
                (double)pitchForZeroVs,
                PITCH_TIME_BUFFER
            ).demandedInput;
        }

        private static Velocity CalculateCrossTrackRateForTrack(Bearing curTrueTrack, Bearing courseTrueTrack, Velocity groundSpeed)
        {
            return groundSpeed * Math.Sin((double)(curTrueTrack - courseTrueTrack));
        }

        private static AngularVelocity CalculateRateForNavTurn(Bearing curTrueTrack, Bearing targetTrueTrack, Angle curRoll, Velocity groundSpeed, int intervalMs)
        {
            Angle turnAmt = targetTrueTrack - curTrueTrack;
            Angle maxRoll = AviationUtil.CalculateMaxBankAngle(groundSpeed, ROLL_LIMIT, HDG_MAX_RATE);
            var time = TimeSpan.FromSeconds(CalculateDemandedInput(
                (double)turnAmt,
                (double)curRoll,
                (double)maxRoll,
                (double)-maxRoll,
                (double demandedRoll, double measuredRoll) => (double)CalculateRollRate((Angle)demandedRoll, (Angle)measuredRoll, intervalMs),
                (double roll) => GeoUtil.EARTH_GRAVITY.Value() * Math.Tan(roll) / groundSpeed.Value(),
                (double)0,
                ROLL_TIME_BUFFER
            ).timeToTarget);

            return time.TotalSeconds < double.Epsilon ? (AngularVelocity)0 : turnAmt / time;
        }

        public static (Angle demandedRoll, Bearing demandedTrack) CalculateDemandedTrackOnCurrentTrack(Length courseDeviation, Bearing curTrueTrack, Bearing courseTrueTrack,
            Angle curRoll, Velocity groundSpeed, int intervalMs)
        {
            var trackDelta = curTrueTrack - courseTrueTrack;
            Bearing minTrack = (Bearing)courseTrueTrack.Clone();
            Bearing maxTrack = (Bearing)courseTrueTrack.Clone();

            if ((double)trackDelta > 0)
            {
                maxTrack = minTrack + trackDelta;
            } else
            {
                minTrack = maxTrack + trackDelta;
            }

            Bearing demandedTrack = Bearing.FromRadians(CalculateDemandedInput(
                (double)-courseDeviation,
                (courseTrueTrack + trackDelta).Radians,
                maxTrack.Radians,
                minTrack.Radians,
                (double demanded, double measured) => (double) CalculateRateForNavTurn(Bearing.FromRadians(measured), Bearing.FromRadians(demanded), curRoll, groundSpeed, intervalMs),
                (double track) => (double)CalculateCrossTrackRateForTrack(Bearing.FromRadians(track), courseTrueTrack, groundSpeed),
                courseTrueTrack.Radians,
                ROLL_TIME_BUFFER
            ).demandedInput);

            Angle turnAmt = demandedTrack - curTrueTrack;

            return (CalculateDemandedRollForTurn(turnAmt, curRoll, groundSpeed, intervalMs), demandedTrack);
        }

        public static (Angle demandedPitch, Angle demandedFpa) CalculateDemandedPitchForVnav(Length vTk_m, Angle curFpa, Angle reqFpa, PerfData perfData, Velocity ias_kts, Length dens_alt_ft, double mass_kg, double spdBrakePos, int config, Velocity gs_kts, int intervalMs)
        {
            // Get max and min Fpa
            Angle maxFpa = Angle.FromDegrees(0);
            Angle minFpa = reqFpa - Angle.FromDegrees(1);

            // Find demanded FPA
            Angle targetFpa = (Angle)(CalculateDemandedInput(
                (double)-vTk_m,
                (double)curFpa,
                (double)maxFpa,
                (double)minFpa,
                (double startFpa, double endFpa) =>
                {
                    double startPitch = PerfDataHandler.GetRequiredPitchForVs(perfData, AviationUtil.CalculateVerticalSpeed(gs_kts, (Angle) startFpa).FeetPerMinute, ias_kts.Knots, dens_alt_ft.Feet, mass_kg, spdBrakePos, config);

                    double endPitch = PerfDataHandler.GetRequiredPitchForVs(perfData, AviationUtil.CalculateVerticalSpeed(gs_kts, (Angle)endFpa).FeetPerMinute, ias_kts.Knots, dens_alt_ft.Feet, mass_kg, spdBrakePos, config);

                    double pitchRate = CalculatePitchRate(Angle.FromDegrees(endPitch), Angle.FromDegrees(startPitch), intervalMs).DegreesPerSecond;

                    if (endPitch - startPitch == 0)
                    {
                        return 0;
                    }

                    return (endFpa - startFpa) * pitchRate / (endPitch - startPitch);
                },
                (double fpa) => AviationUtil.CalculateVerticalSpeed(gs_kts, Angle.FromDegrees(fpa)).MetersPerSecond,
                (double)reqFpa,
                PITCH_TIME_BUFFER).demandedInput);

            double demandedPitch = PerfDataHandler.GetRequiredPitchForVs(perfData, AviationUtil.CalculateVerticalSpeed(gs_kts, targetFpa).FeetPerMinute,
                ias_kts.Knots, dens_alt_ft.Feet, mass_kg, 0, config);

            return (Angle.FromDegrees(demandedPitch), targetFpa);
        }

        public static (Angle demandedRoll, Bearing demandedTrack) CalculateDemandedRollForNav(Length courseDeviation, Bearing curTrueTrack, Bearing courseTrueTrack, Length courseTurnRadius, Angle curRoll, Velocity groundSpeed, int intervalMs)
        {
            Angle maxTrack = courseTrueTrack.Angle + MAX_INTC_ANGLE;
            Angle minTrack = courseTrueTrack.Angle - MAX_INTC_ANGLE;
            Angle maxAbsTrack = courseTrueTrack.Angle + Angle.FromDegrees(90);
            Angle minAbsTrack = courseTrueTrack.Angle - Angle.FromDegrees(90);

            Angle trackDelta = curTrueTrack - courseTrueTrack;
            Angle adjCurTrack = courseTrueTrack.Angle + trackDelta;

            Angle demandedTrack = (Angle) (CalculateDemandedInput(
                    (double) -courseDeviation,
                    (double) trackDelta,
                    (double) MAX_INTC_ANGLE,
                    (double) -MAX_INTC_ANGLE,
                    (double demanded, double measured) => (double) CalculateRateForNavTurn((Bearing) measured, (Bearing)demanded, curRoll, groundSpeed, intervalMs),
                    (double track) => (double)CalculateCrossTrackRateForTrack((Bearing)track, (Bearing)0, groundSpeed),
                    0,
                    0
                ).demandedInput);

            // Restrict demanded track to intercept angles
            if (demandedTrack > MAX_INTC_ANGLE)
            {
                demandedTrack = MAX_INTC_ANGLE;
            } else if (demandedTrack < -MAX_INTC_ANGLE)
            {
                demandedTrack = -MAX_INTC_ANGLE;
            }

            Bearing demandedTrackB = courseTrueTrack + demandedTrack;
            Angle turnAmt = demandedTrackB - curTrueTrack;

            // Figure out if the course is an arc and adjust the "zero" bank accordingly
            if (Math.Abs(courseTurnRadius.Meters) < double.Epsilon)
            {
                return (CalculateDemandedRollForTurn(turnAmt, curRoll, groundSpeed, intervalMs), demandedTrackB);
            }

            // Calculate Roll Angle for desired turn radius
            Angle zeroRoll = AviationUtil.CalculateBankAngle(Length.FromMeters(Math.Abs(courseTurnRadius.Meters)), groundSpeed);

            if (courseTurnRadius.Meters < 0)
            {
                zeroRoll *= -1;
            }

            return (CalculateDemandedRollForTurn(turnAmt, curRoll, zeroRoll, groundSpeed, intervalMs), demandedTrackB);
        }
    }
}