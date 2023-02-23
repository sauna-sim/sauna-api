using System;
using System.Collections.Generic;

namespace SaunaSim.Core.Simulator.Aircraft.Performance
{
    public class PerfDataPoint
    {
        public int VsClimb { get; set; }
        public int VsDescent { get; set; }
        public double PitchClimb { get; set; }
        public double PitchDescent { get; set; }
        public double AccelLevelMaxThrust { get; set; }
        public double AccelLevelIdleThrust { get; set; }
        public double N1Climb { get; set; }
        public double N1Descent { get; set; }

        public static PerfDataPoint Interpolate(int x1, int x2, PerfDataPoint x1Dp, PerfDataPoint x2Dp, int x)
        {
            double multiplier = (double)(x - x1) / (x2 - x1);
            PerfDataPoint newPoint = new PerfDataPoint()
            {
                VsClimb = (int)PerfDataHandler.InterpolateNumbers(x1Dp.VsClimb, x2Dp.VsClimb, multiplier),
                VsDescent = (int)PerfDataHandler.InterpolateNumbers(x1Dp.VsDescent, x2Dp.VsDescent, multiplier),
                PitchClimb = PerfDataHandler.InterpolateNumbers(x1Dp.PitchClimb, x2Dp.PitchClimb, multiplier),
                PitchDescent = PerfDataHandler.InterpolateNumbers(x1Dp.PitchDescent, x2Dp.PitchDescent, multiplier),
                AccelLevelMaxThrust = PerfDataHandler.InterpolateNumbers(x1Dp.AccelLevelMaxThrust, x2Dp.AccelLevelMaxThrust, multiplier),
                AccelLevelIdleThrust = PerfDataHandler.InterpolateNumbers(x1Dp.AccelLevelIdleThrust, x2Dp.AccelLevelIdleThrust, multiplier),
                N1Climb = PerfDataHandler.InterpolateNumbers(x1Dp.N1Climb, x2Dp.N1Climb, multiplier),
                N1Descent = PerfDataHandler.InterpolateNumbers(x1Dp.N1Descent, x2Dp.N1Descent, multiplier)
            };
            return newPoint;
        }
    }
}