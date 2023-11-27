using System;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.MathTools;
using NUnit.Framework.Constraints;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.Performance;

namespace sauna_tests
{
    public class AutopilotTests
    {
        private PerfData _perfData;

        [SetUp]
        public void Setup()
        {
            _perfData = PerfDataHandler.LookupForAircraft("A320");
        }

        [Test]
        public void HdgTest1()
        {
            double demandedRoll = AutopilotUtil.CalculateDemandedRollForTurn(30, 0, 250, 100);
            Assert.That(demandedRoll, Is.EqualTo(25));
        }

        [Test]
        public void FlchTest1()
        {
            double speedDelta = 10; // 10 kts fast
            double targetSpd = 250;
            double densAlt = 10000;
            double massKg = _perfData.MTOW_kg;

            double maxPitch = AutopilotUtil.PITCH_LIMIT_MAX;
            double minPitch = AutopilotUtil.PITCH_LIMIT_MIN;
            double zeroVsPitch = PerfDataHandler.GetRequiredPitchForVs(_perfData, 0, targetSpd + speedDelta, densAlt, massKg, 0, 0);
            double zeroAccelPitch = PerfDataHandler.GetRequiredPitchForThrust(_perfData, 0, 0, targetSpd, densAlt, massKg, 0, 0);
            double demandedPitch = AutopilotUtil.CalculateDemandedPitchForSpeed(speedDelta, 10, zeroAccelPitch, maxPitch, minPitch, (double pitch) => PerfDataHandler.CalculatePerformance(_perfData, pitch, 0, targetSpd + speedDelta, densAlt, massKg, 0, 0).accelFwd, 100);
            Assert.That(demandedPitch, Is.EqualTo(zeroAccelPitch));
        }

        [Test]
        public void ShouldAselTest1()
        {
            double altDelta = 100;
            double ias = 250;
            double densAlt = 10000;
            double massKg = _perfData.MTOW_kg;

            // Calculate required ASEL pitch
            double zeroVsPitch = PerfDataHandler.GetRequiredPitchForVs(_perfData, 0, ias, densAlt, massKg, 0, 0);
            double idlePitch = PerfDataHandler.GetRequiredPitchForThrust(_perfData, 0, 0, ias, densAlt, massKg, 0, 0);
            double maxPitch = PerfDataHandler.GetRequiredPitchForThrust(_perfData, 1, 0, ias, densAlt, massKg, 0, 0);
            double pitchTarget = AutopilotUtil.CalculateDemandedPitchForAltitude(
                altDelta,
                idlePitch,
                zeroVsPitch,
                idlePitch,
                maxPitch,
                (pitch) => PerfDataHandler.CalculatePerformance(_perfData, pitch, 0, ias, densAlt, massKg, 0, 0).vs / 60,
                100
            );

            Assert.That(Math.Abs(zeroVsPitch - pitchTarget), Is.LessThan(double.Epsilon));
        }

        [Test]
        public void LnavTest1()
        {
            double gs = 250;
            (double demandedRoll, double demandedTrack) = AutopilotUtil.CalculateDemandedRollForNav(-10000, 0, 0, 0, 0, gs, 100);
            Assert.Multiple(() =>
            {
                Assert.That(demandedRoll, Is.EqualTo(25));
                Assert.That(demandedTrack, Is.EqualTo(45));
            });
        }

        [Test]
        public void LnavTest2()
        {
            double gs = 250;
            (double demandedRoll, double demandedTrack) = AutopilotUtil.CalculateDemandedRollForNav(-10, 45, 0, 0, 0, gs, 100);
            Assert.Multiple(() =>
            {
                Assert.That(demandedRoll, Is.EqualTo(-25));
                Assert.That(demandedTrack, Is.EqualTo(0));
            });
        }
    }
}

