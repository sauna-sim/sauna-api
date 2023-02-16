using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS;

namespace SaunaSim.Core.Simulator.Aircraft
{
    public class AltitudeHoldInstruction : IVerticalControlInstruction
    {
        public VerticalControlMode Type => VerticalControlMode.ALT_HOLD;

        public int AssignedAltitude { get; private set; }

        public AltitudeHoldInstruction(int assignedAlt)
        {
            AssignedAltitude = assignedAlt;
        }

        private bool ShouldCaptureAltitude(double indicatedAlt, double verticalSpeed, int posCalcInterval)
        {
            // Calculate whether altitude should be captured based on V/S.
            double nextAlt = indicatedAlt + (verticalSpeed * posCalcInterval / (60 * 1000));

            return (nextAlt <= AssignedAltitude && AssignedAltitude <= indicatedAlt) ||
                (nextAlt >= AssignedAltitude && AssignedAltitude >= indicatedAlt);
        }

        public override string ToString()
        {
            return $"ALT Hold: {AssignedAltitude}";
        }

        public bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            return ShouldCaptureAltitude(position.IndicatedAltitude, position.VerticalSpeed, posCalcInterval);
        }

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            int vs = 0;

            // Descend or climb if required
            // Max vertical speed of 500 fpm (for now)
            if (position.IndicatedAltitude > AssignedAltitude)
            {
                vs = -500;
            }
            else if (position.IndicatedAltitude < AssignedAltitude)
            {
                vs = 500;
            }

            // Check if altitude should be captured. Otherwise, continue climbing/descending
            if (ShouldCaptureAltitude(position.IndicatedAltitude, vs, posCalcInterval))
            {
                position.IndicatedAltitude = AssignedAltitude;
            }
            else
            {
                new VerticalSpeedInstruction(vs).UpdatePosition(ref position, ref fms, posCalcInterval);
            }
        }
    }
}
