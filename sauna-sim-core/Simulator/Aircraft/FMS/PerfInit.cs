using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Simulator.Aircraft.FMS
{
    public struct PerfInit
    {
        public int ClimbKias { get; set; }
        public int ClimbMach { get; set; }
        public int CruiseKias { get; set; }
        public int CruiseMach { get; set; }
        public int DescentKias { get; set; }
        public int DescentMach { get; set; }
        public int DescentAngle { get; set; }
    }
}
