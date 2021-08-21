using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.Instructions.Lateral
{
    public class HoldingPatternInstruction : ILateralControlInstruction
    {
        public LateralControlMode Type => LateralControlMode.HOLDING_PATTERN;

        public bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            // Holds never get activated early 
            return false;
        }

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            throw new NotImplementedException();
        }
    }
}
