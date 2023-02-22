using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Simulator.Aircraft.Performance
{
    public static class PerfDataHandler
    {
        public static PerfData LookupForAircraft(string icaoEquip)
        {
            PerfData e175 = new PerfData()
            {
                Climb_KIAS = 270,
                Climb_Mach = 0.73,
                Cruise_KIAS = 290,
                Cruise_Mach = 0.78,
                Descent_KIAS = 290,
                Descent_Mach = 0.78,
                Engines = 2,
                ConfigList = new List<PerfConfigSetting>()
                {
                    new PerfConfigSetting()
                    {
                        GearDown = false,
                        MaxKias = 320,
                        MinKias = 180,
                        NormKias = 250
                    },
                    new PerfConfigSetting()
                    {
                        GearDown = false,
                        MaxKias = 230,
                        MinKias = 160,
                        NormKias = 210
                    },
                    new PerfConfigSetting()
                    {
                        GearDown = false,
                        MaxKias = 215,
                        MinKias = 150
                    },
                    new PerfConfigSetting()
                    {
                        GearDown = true,
                        MaxKias = 200,
                        MinKias = 140,
                        NormKias = 160
                    },
                    new PerfConfigSetting()
                    {
                        GearDown = true,
                        MaxKias = 180,
                        MinKias = 110,
                        NormKias = 140
                    }
                }
            };

            return e175;
        }

        public static (double accelFwd, double vs) CalculatePerformance(PerfData perfData, double pitch_degs, double ias_kts, double dens_alt_ft, double thrustLeverPos)
        {
            PerfDataPoint dataPoint = PerfDataPoint.InterpolateBetween(perfData.DataPoints, (int) dens_alt_ft, (int) ias_kts);
            double pitchPerc = (pitch_degs - dataPoint.PitchIdleThrust) / (dataPoint.PitchMaxThrust - dataPoint.PitchIdleThrust);
            double vs = pitchPerc * (dataPoint.VsMaxThrust - dataPoint.VsIdleThrust);
            double thrustDelta = thrustLeverPos - pitchPerc;
            double zeroAccelThrust = -dataPoint.AccelLevelIdleThrust / (dataPoint.AccelLevelMaxThrust - dataPoint.AccelLevelIdleThrust);
            double accelFwd = ((zeroAccelThrust + thrustDelta) * (dataPoint.AccelLevelMaxThrust - dataPoint.AccelLevelIdleThrust)) + dataPoint.AccelLevelIdleThrust;

            return (accelFwd, vs);
        }
    }
}