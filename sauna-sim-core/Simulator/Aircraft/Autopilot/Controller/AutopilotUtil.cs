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
        public const double ROLL_RATE_MAX = 10.0;
        public const double ROLL_LIMIT = 25.0;
        public const double HDG_MAX_RATE = 3.0;
        public const double ROLL_TIME_BUFFER = 0.1;

        // Pitch
        public const double PITCH_TIME = 0.5;
        public const double PITCH_LIMIT_MAX = 30.0;
        public const double PITCH_LIMIT_MIN = -15.0;
        public const double PITCH_TIME_BUFFER = 0.1;
        public const double PITCH_RATE_MAX = 5.0;

        // Thrust
        public const double THRUST_TIME = 0.5;
        public const double THRUST_RATE_MAX = 30.0;
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
            }
            else if (rateMax > 0 && requiredRate > rateMax)
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
            return CalculateRate(demandedThrust, measuredThrust, THRUST_TIME, THRUST_RATE_MAX, intervalMs);
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
            return CalculateRate(demandedPitchAngle, measuredPitchAngle, PITCH_TIME, PITCH_RATE_MAX, intervalMs);
        }

        public static double CalculateDemandedInput(double deltaToTarget, double curInput, double maxInputLimit, double minInputLimit,
            Func<double, double, double> inputRateFunction, Func<double, double> targetRateFunction, double zeroTargetRateInput, double inputTimeBuffer)
        {
            if (Math.Abs(deltaToTarget) <= double.Epsilon)
            {
                return zeroTargetRateInput;
            }

            // Figure out time to get to 0 from max input and to get to max input from current input
            double maxInput = deltaToTarget < 0 ? minInputLimit : maxInputLimit;
            
            double curInputTargetRate = targetRateFunction(curInput);
            double maxInputTargetRate = targetRateFunction(maxInput);
            double inputOutTargetDelta = 0;
            double inputInTargetDelta = 0;
            
            // Calculate time and target delta to get from maximum input to zero rate input if there is a difference
            if (Math.Abs(zeroTargetRateInput - maxInput) > double.Epsilon)
            {
                double maxInputOutRate = inputRateFunction(zeroTargetRateInput, maxInput);
                double maxInputOut_t = Math.Abs(Math.Abs(maxInput - zeroTargetRateInput) / maxInputOutRate) * (1 + inputTimeBuffer);
                double inputOutTarget_a = PerfDataHandler.CalculateAcceleration(maxInputTargetRate, 0, maxInputOut_t);
                inputOutTargetDelta = PerfDataHandler.CalculateDisplacement(maxInputTargetRate, inputOutTarget_a, maxInputOut_t);
                
            }
            
            // Calculate time and target delta to get from current input to maximum input if there is a difference
            if (Math.Abs(curInput - maxInput) > double.Epsilon)
            {
                double maxInputInRate = inputRateFunction(maxInput, curInput);
                double maxInputIn_t = Math.Abs(Math.Abs(curInput - maxInput) / maxInputInRate) * (1 + inputTimeBuffer);
                double inputInTarget_a = PerfDataHandler.CalculateAcceleration(curInputTargetRate, maxInputTargetRate, maxInputIn_t);
                inputInTargetDelta = PerfDataHandler.CalculateDisplacement(curInputTargetRate, inputInTarget_a, maxInputIn_t);
                
            }
            
            // Return max if we're going in the wrong direction.
            if (inputInTargetDelta > 0 && deltaToTarget < 0 || inputInTargetDelta < 0 && deltaToTarget > 0)
            {
                return maxInput;
            }

            // Find midpoint of the two
            (double m1, double b1) = PerfDataHandler.CreateLineEquation(0, curInput, inputInTargetDelta, maxInput);
            (double m2, double b2) = PerfDataHandler.CreateLineEquation(deltaToTarget - inputOutTargetDelta, maxInput, deltaToTarget, zeroTargetRateInput);
            (double midPointTargetDelta, double midPointInput) = PerfDataHandler.FindLinesIntersection(m1, b1, m2, b2);

            // Figure out the desired input value
            if (midPointTargetDelta <= 0 && deltaToTarget > 0 || midPointTargetDelta >= 0 && deltaToTarget < 0)
            {
                return zeroTargetRateInput;
            }

            if (Math.Abs(midPointInput) >= Math.Abs(maxInput))
            {
                return maxInput;
            }

            return midPointInput;
        }

        /// <summary>
        /// Calculate the required roll rate for a turn.
        /// </summary>
        /// <param name="turnAmt">Amount to turn (degrees)</param>
        /// <param name="curRoll">Current roll (degrees)</param>
        /// <param name="groundSpeed">Current ground speed (knots)</param>
        /// <param name="intervalMs">Update Interval Time (ms)</param>
        /// <returns>Desired roll rate (degrees/s)</returns>
        public static double CalculateDemandedRollForTurn(double turnAmt, double curRoll, double groundSpeed, int intervalMs)
        {
            double maxRoll = GeoUtil.CalculateMaxBankAngle(groundSpeed, ROLL_LIMIT, HDG_MAX_RATE);
            return CalculateDemandedInput(
                turnAmt,
                curRoll,
                maxRoll,
                -maxRoll,
                (double demandedRoll, double measuredRoll) => CalculateRollRate(demandedRoll, measuredRoll, intervalMs),
                (double roll) => Math.Tan(MathUtil.ConvertDegreesToRadians(roll)) * 1091 / groundSpeed,
                0,
                ROLL_TIME_BUFFER
            );
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
            );
        }

        public static double CalculateDemandedPitchForSpeed(double speedDelta, double curPitch, double pitchForZeroAccel, double maxPitch, double minPitch, Func<double, double> pitchToSpeedAccelFunction,
            int intervalMs)
        {
            return CalculateDemandedInput(
                -speedDelta,
                curPitch,
                minPitch,
                maxPitch,
                (demandedPitch, measuredPitch) => CalculatePitchRate(demandedPitch, measuredPitch, intervalMs),
                pitchToSpeedAccelFunction,
                pitchForZeroAccel,
                PITCH_TIME_BUFFER
            );
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
            );
        }
    }
}