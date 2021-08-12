using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl
{
    public class VerticalSpeedInstruction : IVerticalControlInstruction
    {
        public VerticalControlMode Type => VerticalControlMode.VERTICAL_SPEED;

        public int AssignedVerticalSpeed { get; private set; }

        public VerticalSpeedInstruction(int assignedVerticalSpeed)
        {
            this.AssignedVerticalSpeed = assignedVerticalSpeed;
        }

        public bool ShouldActivateInstruction(AcftData position, int posCalcInterval)
        {
            return true;
        }

        public void UpdatePosition(ref AcftData position, int posCalcInterval)
        {
            // Calculate next altitude
            position.IndicatedAltitude += AssignedVerticalSpeed * posCalcInterval / (60.0 * 1000.0);
            position.VerticalSpeed = AssignedVerticalSpeed;
        }

        public override string ToString()
        {
            return $"V/S Hold: {AssignedVerticalSpeed}fpm";
        }
    }
}
