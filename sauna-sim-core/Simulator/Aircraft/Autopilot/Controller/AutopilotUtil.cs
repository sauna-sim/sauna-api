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

        // Thrust
        public const double THRUST_TIME = 0.5;
        public const double THRUST_RATE_MAX = 30.0;

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
        /// Calculate the required roll rate for a turn.
        /// </summary>
        /// <param name="turnAmt">Amount to turn (degrees)</param>
        /// <param name="curRoll">Current roll (degrees)</param>
        /// <param name="groundSpeed">Current ground speed (knots)</param>
        /// <param name="intervalMs">Update Interval Time (ms)</param>
        /// <returns>Desired roll rate (degrees/s)</returns>
        public static double CalculateDemandedRollForTurn(double turnAmt, double curRoll, double groundSpeed, int intervalMs)
        {
            if (Math.Abs(turnAmt) <= double.Epsilon)
            {
                return 0;
            }
            // Figure out time to roll out from max roll and roll into max roll
            double maxRoll = GeoUtil.CalculateMaxBankAngle(groundSpeed, ROLL_LIMIT, HDG_MAX_RATE);
            if (turnAmt < 0)
            {
                maxRoll *= -1;
            }
            double maxRollOutRate = CalculateRollRate(0, maxRoll, intervalMs);
            double maxRollOut_t = Math.Abs(maxRoll / maxRollOutRate) * (1 - ROLL_TIME_BUFFER);
            double maxRollInRate = CalculateRollRate(maxRoll, curRoll, intervalMs);
            double maxRollIn_t = Math.Abs(Math.Abs(curRoll - maxRoll) / maxRollInRate)  * (1 - ROLL_TIME_BUFFER);
            
            // Find turn degrees required to roll out from max roll and roll into max roll
            double curRollTurnRate = Math.Tan(MathUtil.ConvertDegreesToRadians(curRoll)) * 1091 / groundSpeed;
            double maxRollTurnRate = Math.Tan(MathUtil.ConvertDegreesToRadians(maxRoll)) * 1091 / groundSpeed;
            double rollInTurn_a = PerfDataHandler.CalculateAcceleration(curRollTurnRate, maxRollTurnRate, maxRollIn_t);
            double rollOutTurn_a = PerfDataHandler.CalculateAcceleration(maxRollTurnRate, 0, maxRollOut_t);
            double rollInTurn_degs = PerfDataHandler.CalculateDisplacement(curRollTurnRate, rollInTurn_a, maxRollIn_t);
            double rollOutTurn_degs = PerfDataHandler.CalculateDisplacement(maxRollTurnRate, rollOutTurn_a, maxRollOut_t);
            
            // Find midpoint of the two
            if (rollInTurn_degs < 0 && rollOutTurn_degs < 0 || rollOutTurn_degs > 0 && rollInTurn_degs > 0)
            {
                if (rollInTurn_degs < 0)
                {
                    double midPointDelta = Math.Abs(rollInTurn_degs - rollOutTurn_degs) / 2;
                }
            }
            double turnAmtMidPoint = (rollOutTurn_degs / (rollInTurn_degs + rollOutTurn_degs)) * turnAmt;

            // Figure out the desired roll angle
            double rollAngle = maxRoll;
            if ((turnAmt < 0 && turnAmt >= turnAmtMidPoint) || (turnAmt > 0 && turnAmt <= turnAmtMidPoint))
            {
                rollAngle = 0;
            }

            return rollAngle;
        }
    }
}