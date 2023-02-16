using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaunaSim.Core.Simulator.Aircraft
{
    public enum VerticalControlMode
    {
        ALT_HOLD,
        VERTICAL_SPEED,
        FLIGHT_PATH_ANGLE,
        GLIDESLOPE,
        VERTICAL_NAV
    }

    public interface IVerticalControlInstruction : IControlInstruction
    {
        VerticalControlMode Type { get; }
    }
}
