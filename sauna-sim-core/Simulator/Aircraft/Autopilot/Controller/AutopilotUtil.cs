using System;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.MathTools;
using SaunaSim.Core.Simulator.Aircraft.Performance;

namespace SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller
{
    public static class AutopilotUtil
    {
        // Roll
        public const double ROLL_TIME = 0.5;
        public const double ROLL_RATE_MAX = 5.0;
        public const double ROLL_LIMIT = 25.0;
        public const double HDG_MAX_RATE = 3.0;
        public const double ROLL_TIME_BUFFER = 0.1;

        // LNAV
        public const double MAX_INTC_ANGLE = 45;
        public const double MIN_XTK_M = 3;
        public const double MAX_CRS_DEV = 0.1;
        public const double MAX_INTC_XTK_M = 1852.0;
        public const double RADIUS_BUFFER_MULT = 1.1;

        // Pitch
        public const double PITCH_TIME = 0.5;
        public const double PITCH_LIMIT_MAX = 30.0;
        public const double PITCH_LIMIT_MIN = -15.0;
        public const double PITCH_TIME_BUFFER = 0.1;
        public const double PITCH_RATE_NORM_MAX = 1.0;
        public const double PITCH_RATE_TOLDG_MAX = 3.0;

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
        public static double CalculateRollRate(double demandedRollAngle, double measuredRollAngle, int intervalMs)
        {
            return CalculateRate(demandedRollAngle, measuredRollAngle, ROLL_TIME, ROLL_RATE_MAX, intervalMs);
        }

        /// <summary>
        /// Calculates pitch rate.
        /// </summary>
        /// <param name="demandedPitchAngle">Demanded Pitch Angle (degrees)</param>
        /// <param name="measuredPitchAngle">Measured Pitch Angle (degrees)</param>
        /// <param name="intervalMs">Update Interval Time (ms)</param>
        /// <returns>Pitch Rate (degrees/sec)</returns>
        public static double CalculatePitchRate(double demandedPitchAngle, double measuredPitchAngle, int intervalMs)
        {
            return CalculateRate(demandedPitchAngle, measuredPitchAngle, PITCH_TIME, PITCH_RATE_NORM_MAX, intervalMs);
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
                double inputOutTarget_a = PerfDataHandler.CalculateAcceleration(maxInputTargetRate, 0, maxInputOut_t);
                inputOutTargetDelta = PerfDataHandler.CalculateDisplacement(maxInputTargetRate, inputOutTarget_a, maxInputOut_t);
            }

            // Calculate time and target delta to get from current input to maximum input if there is a difference
            if (Math.Abs(curInput - maxInput) > double.Epsilon)
            {
                double maxInputInRate = inputRateFunction(maxInput, curInput);
                maxInputIn_t = Math.Abs(Math.Abs(curInput - maxInput) / maxInputInRate) * (1 + inputTimeBuffer);
                double inputInTarget_a = PerfDataHandler.CalculateAcceleration(curInputTargetRate, maxInputTargetRate, maxInputIn_t);
                inputInTargetDelta = PerfDataHandler.CalculateDisplacement(curInputTargetRate, inputInTarget_a, maxInputIn_t);
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
            (double m1, double b1) = PerfDataHandler.CreateLineEquation(0, curInput, inputInTargetDelta, maxInput);
            (double m2, double b2) = PerfDataHandler.CreateLineEquation(deltaToTarget - inputOutTargetDelta, maxInput, deltaToTarget, zeroTargetRateInput);
            (double m3, double b3) = (0, maxInput);
            /*Console.WriteLine($"f(x) = {m1}x+{b1}");
            Console.WriteLine($"g(x) = {m2}x+{b2}");
            Console.WriteLine($"h(x) = {m3}x+{b3}");
            Console.WriteLine($"x = {deltaToTarget}");*/

            (double inputOutIntersectionX, double inputOutIntersectionY) = PerfDataHandler.FindLinesIntersection(m2, b2, m3, b3);
            (double midPointTargetDelta, double midPointInput) = PerfDataHandler.FindLinesIntersection(m1, b1, m2, b2);

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
        public static double CalculateDemandedRollForTurn(double turnAmt, double curRoll, double groundSpeed, int intervalMs)
        {
            return CalculateDemandedRollForTurn(turnAmt, curRoll, 0, groundSpeed, intervalMs);
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
        public static double CalculateDemandedRollForTurn(double turnAmt, double curRoll, double zeroRoll, double groundSpeed, int intervalMs)
        {
            double maxRoll = GeoUtil.CalculateMaxBankAngle(groundSpeed, ROLL_LIMIT, HDG_MAX_RATE);
            return CalculateDemandedInput(
                turnAmt,
                curRoll,
                maxRoll,
                -maxRoll,
                (double demandedRoll, double measuredRoll) => CalculateRollRate(demandedRoll, measuredRoll, intervalMs),
                (double roll) => Math.Tan(MathUtil.ConvertDegreesToRadians(roll)) * 1091 / groundSpeed,
                zeroRoll,
                ROLL_TIME_BUFFER
            ).demandedInput;
        }

        public static double CalculateDemandedThrottleForSpeed(double speedDelta, double curThrottle, double thrustForZeroAccel, Func<double, double> thrustToSpeedAccelFunction, int intervalMs)
        {
            return CalculateDemandedInput(
                -speedDelta,
                curThrottle,
                100,
                0,
                (demandedThrottle, measuredThrottle) => CalculateThrustRate(demandedThrottle, measuredThrottle, intervalMs),
                thrustToSpeedAccelFunction,
                thrustForZeroAccel,
                THRUST_TIME_BUFFER
            ).demandedInput;
        }

        public static double CalculateDemandedPitchForSpeed(double speedDelta, double curPitch, double pitchForZeroAccel, double maxPitch, double minPitch, Func<double, double> pitchToSpeedAccelFunction,
            int intervalMs)
        {
            double inputToTargetRateFunc(double pitch) => -pitchToSpeedAccelFunction(pitch);
            return CalculateDemandedInput(
                speedDelta,
                curPitch,
                maxPitch,
                minPitch,
                (demandedPitch, measuredPitch) => CalculatePitchRate(demandedPitch, measuredPitch, intervalMs),
                inputToTargetRateFunc,
                pitchForZeroAccel,
                PITCH_TIME_BUFFER
            ).demandedInput;
        }

        public static double CalculateDemandedPitchForAltitude(double altDelta, double curPitch, double pitchForZeroVs, double pitchIdle, double pitchMax, Func<double, double> pitchToVsFunction, int intervalMs)
        {
            return CalculateDemandedInput(
                -altDelta,
                curPitch,
                pitchMax,
                pitchIdle,
                (demandedPitch, measuredPitch) => CalculatePitchRate(demandedPitch, measuredPitch, intervalMs),
                pitchToVsFunction,
                pitchForZeroVs,
                PITCH_TIME_BUFFER
            ).demandedInput;
        }

        private static double CalculateCrossTrackRateForTrack(double curTrueTrack, double courseTrueTrack, double groundSpeed)
        {
            return MathUtil.ConvertKtsToMpers(groundSpeed) * Math.Sin(MathUtil.ConvertDegreesToRadians(curTrueTrack - courseTrueTrack));
        }

        private static double CalculateRateForNavTurn(double curTrueTrack, double targetTrueTrack, double curRoll, double groundSpeed, int intervalMs)
        {
            double turnAmt = GeoUtil.CalculateTurnAmount(curTrueTrack, targetTrueTrack);
            double maxRoll = GeoUtil.CalculateMaxBankAngle(groundSpeed, ROLL_LIMIT, HDG_MAX_RATE);
            double time = CalculateDemandedInput(
                turnAmt,
                curRoll,
                maxRoll,
                -maxRoll,
                (double demandedRoll, double measuredRoll) => CalculateRollRate(demandedRoll, measuredRoll, intervalMs),
                (double roll) => Math.Tan(MathUtil.ConvertDegreesToRadians(roll)) * 1091 / groundSpeed,
                0,
                ROLL_TIME_BUFFER
            ).timeToTarget;

            return time < double.Epsilon ? 0 : turnAmt / time;
        }

        public static (double demandedRoll, double demandedTrack) CalculateDemandedTrackOnCurrentTrack(double courseDeviation, double curTrueTrack, double courseTrueTrack,
            double curRoll, double groundSpeed, int intervalMs)
        {
            double trackDelta = GeoUtil.CalculateTurnAmount(courseTrueTrack, curTrueTrack);
            double minTrack = courseTrueTrack;
            double maxTrack = courseTrueTrack;

            if (trackDelta > 0)
            {
                maxTrack = minTrack + trackDelta;
            } else
            {
                minTrack = maxTrack + trackDelta;
            }

            double demandedTrack = CalculateDemandedInput(
                -courseDeviation,
                courseTrueTrack + trackDelta,
                maxTrack,
                minTrack,
                (double demanded, double measured) => CalculateRateForNavTurn(measured, demanded, curRoll, groundSpeed, intervalMs),
                (double track) => CalculateCrossTrackRateForTrack(track, courseTrueTrack, groundSpeed),
                courseTrueTrack,
                ROLL_TIME_BUFFER
            ).demandedInput;

            demandedTrack = GeoUtil.NormalizeHeading(demandedTrack);

            double turnAmt = GeoUtil.CalculateTurnAmount(curTrueTrack, demandedTrack);

            return (CalculateDemandedRollForTurn(turnAmt, curRoll, groundSpeed, intervalMs), demandedTrack);
        }

        public static (double demandedPitch, double demandedFpa) CalculateDemandedPitchForVnav(double vTk_m, double curFpa, double reqFpa, PerfData perfData, double ias_kts, double dens_alt_ft, double mass_kg, double spdBrakePos, int config, double gs_kts, int intervalMs)
        {
            // Get max and min Fpa
            double maxFpa = 0;
            double minFpa = reqFpa - 1;

            // Find demanded FPA
            double targetFpa = CalculateDemandedInput(
                -vTk_m,
                curFpa,
                maxFpa,
                minFpa,
                (double startFpa, double endFpa) =>
                {
                    double startPitch = PerfDataHandler.GetRequiredPitchForVs(perfData, PerfDataHandler.ConvertFpaToVs(startFpa, gs_kts), ias_kts, dens_alt_ft, mass_kg, spdBrakePos, config);

                    double endPitch = PerfDataHandler.GetRequiredPitchForVs(perfData, PerfDataHandler.ConvertFpaToVs(endFpa, gs_kts), ias_kts, dens_alt_ft, mass_kg, spdBrakePos, config);

                    double pitchRate = CalculatePitchRate(endPitch, startPitch, intervalMs);

                    if (endPitch - startPitch == 0)
                    {
                        return 0;
                    }

                    return (endFpa - startFpa) * pitchRate / (endPitch - startPitch);
                },
                (double fpa) =>
                {
                    double vs = PerfDataHandler.ConvertFpaToVs(fpa, gs_kts);

                    vs /= 60;

                    return MathUtil.ConvertFeetToMeters(vs);
                },
                reqFpa,
                PITCH_TIME_BUFFER).demandedInput;

            double demandedPitch = PerfDataHandler.GetRequiredPitchForVs(perfData, PerfDataHandler.ConvertFpaToVs(targetFpa, gs_kts),
                ias_kts, dens_alt_ft, mass_kg, 0, config);

            return (demandedPitch, targetFpa);
        }

        public static (double demandedRoll, double demandedTrack) CalculateDemandedRollForNav(double courseDeviation, double curTrueTrack, double courseTrueTrack, double courseTurnRadius, double curRoll, double groundSpeed, int intervalMs)
        {
            double maxTrack = courseTrueTrack + MAX_INTC_ANGLE;
            double minTrack = courseTrueTrack - MAX_INTC_ANGLE;
            double maxAbsTrack = courseTrueTrack + 90;
            double minAbsTrack = courseTrueTrack - 90;

            double trackDelta = GeoUtil.CalculateTurnAmount(courseTrueTrack, curTrueTrack);
            double adjCurTrack = courseTrueTrack + trackDelta;

            double demandedTrack = CalculateDemandedInput(
                    -courseDeviation,
                    trackDelta,
                    MAX_INTC_ANGLE,
                    -MAX_INTC_ANGLE,
                    (double demanded, double measured) => CalculateRateForNavTurn(measured, demanded, curRoll, groundSpeed, intervalMs),
                    (double track) => CalculateCrossTrackRateForTrack(track, 0, groundSpeed),
                    0,
                    0
                ).demandedInput;

            // Restrict demanded track to intercept angles
            if (demandedTrack > MAX_INTC_ANGLE)
            {
                demandedTrack = MAX_INTC_ANGLE;
            } else if (demandedTrack < -MAX_INTC_ANGLE)
            {
                demandedTrack = -MAX_INTC_ANGLE;
            }

            demandedTrack = GeoUtil.NormalizeHeading(courseTrueTrack + demandedTrack);

            double turnAmt = GeoUtil.CalculateTurnAmount(curTrueTrack, demandedTrack);

            // Figure out if the course is an arc and adjust the "zero" bank accordingly
            if (Math.Abs(courseTurnRadius) < double.Epsilon)
            {
                return (CalculateDemandedRollForTurn(turnAmt, curRoll, groundSpeed, intervalMs), demandedTrack);
            }

            // Calculate Roll Angle for desired turn radius
            double zeroRoll = GeoUtil.CalculateBankAngle(MathUtil.ConvertMetersToNauticalMiles(Math.Abs(courseTurnRadius)), groundSpeed);

            if (courseTurnRadius < 0)
            {
                zeroRoll *= -1;
            }

            return (CalculateDemandedRollForTurn(turnAmt, curRoll, zeroRoll, groundSpeed, intervalMs), demandedTrack);
        }
    }
}