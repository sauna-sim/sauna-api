using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS;

namespace SaunaSim.Core.Simulator.Aircraft
{
    public class VerticalSpeedInstruction : IVerticalControlInstruction
    {
        public VerticalControlMode Type => VerticalControlMode.VERTICAL_SPEED;

        public int AssignedVerticalSpeed { get; private set; }

        public VerticalSpeedInstruction(int assignedVerticalSpeed)
        {
            this.AssignedVerticalSpeed = assignedVerticalSpeed;
        }

        public override string ToString()
        {
            return $"V/S Hold: {AssignedVerticalSpeed}fpm";
        }

        public bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            return false;
        }

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            // Calculate next altitude
            position.IndicatedAltitude += AssignedVerticalSpeed * posCalcInterval / (60.0 * 1000.0);
            position.VerticalSpeed = AssignedVerticalSpeed;
        }
    }
}
