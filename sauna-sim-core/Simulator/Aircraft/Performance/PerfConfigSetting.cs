using System;
using System.Transactions;

namespace SaunaSim.Core.Simulator.Aircraft.Performance
{
    public class PerfConfigSetting
    {
        public int MinKias { get; set; }
        public int MaxKias { get; set; }
        public bool GearDown { get; set; }
        public int NormKias { get; set; }
        public int VsPenalty { get; set; }
        public double PitchChange { get; set; }
    }
}