using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Simulator.Aircraft.Performance
{
    public class PerfData
    {
        public int Engines { get; set; }
        public List<PerfConfigSetting> ConfigList { get; set; }
        public List<PerfDataPoint> DataPoints { get; set; }

        // Perf Init Data
        public int Climb_KIAS { get; set; }
        public double Climb_Mach { get; set; }
        public int Cruise_KIAS { get; set; }
        public double Cruise_Mach { get; set; }
        public int Descent_KIAS { get; set; }
        public double Descent_Mach { get; set; }
    }
}