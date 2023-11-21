using System;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.MathTools;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;

namespace sauna_tests
{
	public class AutopilotTests
	{
        [SetUp]
        public async Task Setup()
        {
        }

        [Test]
        public async Task LnavTest1()
        {
            double gs = 250;
            (double demandedRoll, double time) = AutopilotUtil.CalculateDemandedRollForNav(-10000, 0, 0, 0, 0, gs, 100);
            double maxRoll = GeoUtil.CalculateMaxBankAngle(gs, AutopilotUtil.ROLL_LIMIT, AutopilotUtil.HDG_MAX_RATE);
            (double rollInDemand, double rollInTime) = AutopilotUtil.CalculateDemandedInput(
                90,
                0,
                maxRoll,
                -maxRoll,
                (double demandedRoll, double measuredRoll) => AutopilotUtil.CalculateRollRate(demandedRoll, measuredRoll, 100),
                (double roll) => Math.Tan(MathUtil.ConvertDegreesToRadians(roll)) * 1091 / gs,
                0,
                AutopilotUtil.ROLL_TIME_BUFFER
            );
        }
    }
}

