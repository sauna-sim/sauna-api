using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl
{
    public enum LateralControlMode
    {
        HEADING,
        TRACK,
        VOR_LOCALIZER,
        GPS
    }

    public interface ILateralControlInstruction : IControlInstruction
    {
        LateralControlMode Type { get; }
    }
}
