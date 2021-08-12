using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl
{
    public class AltitudeHoldInstruction : IVerticalControlInstruction
    {
        public VerticalControlMode Type => VerticalControlMode.ALT_HOLD;

        public int AssignedAltitude { get; private set; }

        public AltitudeHoldInstruction(int assignedAlt)
        {
            AssignedAltitude = assignedAlt;
        }

        public void UpdatePosition(ref AcftData position, int posCalcInterval)
        {
            int vs = 0;

            // Descend or climb if required
            // Max vertical speed of 500 fpm (for now)
            if (position.IndicatedAltitude > AssignedAltitude)
            {
                vs = -500;
            } else if (position.IndicatedAltitude < AssignedAltitude)
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
                new VerticalSpeedInstruction(vs).UpdatePosition(ref position, posCalcInterval);
            }
        }

        private bool ShouldCaptureAltitude(double indicatedAlt, double verticalSpeed, int posCalcInterval)
        {
            // Calculate whether altitude should be captured based on V/S.
            double nextAlt = indicatedAlt + (verticalSpeed * posCalcInterval / (60 * 1000));

            return (nextAlt <= AssignedAltitude && AssignedAltitude <= indicatedAlt) ||
                (nextAlt >= AssignedAltitude && AssignedAltitude >= indicatedAlt);
        }

        public bool ShouldActivateInstruction(AcftData position, int posCalcInterval)
        {
            return ShouldCaptureAltitude(position.IndicatedAltitude, position.VerticalSpeed, posCalcInterval);
        }
    }
}
