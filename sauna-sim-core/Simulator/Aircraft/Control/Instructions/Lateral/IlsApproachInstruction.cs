using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fix;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.FMS;

namespace SaunaSim.Core.Simulator.Aircraft.Control.Instructions.Lateral
{
    public class IlsApproachInstruction : ILateralControlInstruction
    {
        private Localizer _loc;
        private InterceptCourseInstruction _instr;

        public event EventHandler AircraftLanded;

        public IlsApproachInstruction(Localizer loc)
        {
            _loc = loc;
            _instr = new InterceptCourseInstruction(new RouteWaypoint(new Waypoint(_loc.Runway_identifier, _loc.Loc_location, "", "")), _loc.Loc_bearing);
        }

        public LateralControlMode Type => LateralControlMode.APPROACH;

        public bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            return _instr.ShouldActivateInstruction(position, fms, posCalcInterval);
        }

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            _instr.UpdatePosition(ref position, ref fms, posCalcInterval);
            if (_instr.AlongTrackM < 0)
            {
                AircraftLanded?.Invoke(this, new EventArgs());
            }
        }

        public override string ToString()
        {
            return $"APP {_loc.Runway_identifier}";
        }
    }
}
