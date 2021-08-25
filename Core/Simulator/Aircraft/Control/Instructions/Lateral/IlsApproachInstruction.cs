using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.Instructions.Lateral
{
    public class IlsApproachInstruction : ILateralControlInstruction
    {
        private Localizer _loc;
        private InterceptCourseInstruction _instr;

        public event EventHandler AircraftLanded;

        public IlsApproachInstruction(Localizer loc)
        {
            _loc = loc;
            _instr = new InterceptCourseInstruction(new RouteWaypoint(_loc), _loc.Course);
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
            return $"APP {_loc.Name}";
        }
    }
}
