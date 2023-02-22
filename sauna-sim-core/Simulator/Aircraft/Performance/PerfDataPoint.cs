using System;
using System.Collections.Generic;

namespace SaunaSim.Core.Simulator.Aircraft.Performance
{
    public class PerfDataPoint : IComparable<PerfDataPoint>
    {
        public int Kias { get; set; }
        public int VsMaxThrust { get; set; }
        public int VsIdleThrust { get; set; }
        public int Altitude { get; set; }
        public int PitchMaxThrust { get; set; }
        public int PitchIdleThrust { get; set; }
        public double AccelLevelMaxThrust { get; set; }
        public double AccelLevelIdleThrust { get; set; }

        public static PerfDataPoint InterpolateBetween(List<PerfDataPoint> dpList, int altitude, int kias)
        {
            if (dpList.Count < 1)
            {
                return null;
            }
            
            foreach (var dp in dpList)
            {
                if (altitude < dp.Altitude && kias < dp.Kias)
                {
                    return dp;
                }                
            }

            return dpList[dpList.Count - 1];
        }

        public int CompareTo(PerfDataPoint other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var altitudeComparison = Altitude.CompareTo(other.Altitude);
            if (altitudeComparison != 0) return altitudeComparison;
            var kiasComparison = Kias.CompareTo(other.Kias);
            if (kiasComparison != 0) return kiasComparison;
            return PitchIdleThrust.CompareTo(other.PitchIdleThrust);
        }
    }
}