using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.GeoTools;

namespace VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl
{
    public class HeadingHoldInstruction : ILateralControlInstruction
    {
        public LateralControlMode Type => LateralControlMode.HEADING_HOLD;

        public int AssignedHeading { get; private set; }

        public TurnDirection TurnDir { get; private set; }

        public HeadingHoldInstruction(TurnDirection turnDir, int assignedHeading)
        {
            TurnDir = turnDir;
            AssignedHeading = (assignedHeading >= 360) ? assignedHeading - 360 : assignedHeading;
        }

        public HeadingHoldInstruction(int assignedHeading) : this(TurnDirection.SHORTEST, assignedHeading) { }

        public void UpdatePosition(ref AcftData position, int posCalcInterval)
        {
            // Calculate next position and heading
            double turnAmount = AcftGeoUtil.CalculateTurnAmount(position.Heading_Mag, AssignedHeading);
            double distanceTravelledNMi = AcftGeoUtil.CalculateDistanceTravelledNMi(position.GroundSpeed, posCalcInterval);

            if (Math.Abs(turnAmount) > 1)
            {
                // Calculate bank angle
                double bankAngle = AcftGeoUtil.CalculateBankAngle(position.GroundSpeed, 25, 3);

                // Calculate radius of turn
                double radiusOfTurn = AcftGeoUtil.CalculateRadiusOfTurn(bankAngle, position.GroundSpeed);

                // Calculate degrees to turn
                double degreesToTurn = Math.Min(Math.Abs(turnAmount), AcftGeoUtil.CalculateDegreesTurned(distanceTravelledNMi, radiusOfTurn));

                // Figure out turn direction
                bool isRightTurn = (TurnDir == TurnDirection.SHORTEST) ?
                    (turnAmount > 0) :
                    (TurnDir == TurnDirection.RIGHT);

                // Calculate end heading
                double endHeading = AcftGeoUtil.CalculateEndHeading(position.Heading_Mag, degreesToTurn, isRightTurn);

                // Calculate chord line data
                Tuple<double, double> chordLine = AcftGeoUtil.CalculateChordHeadingAndDistance(position.Heading_Mag, degreesToTurn, radiusOfTurn, isRightTurn);

                // Calculate new position
                position.Heading_Mag = chordLine.Item1;
                AcftGeoUtil.CalculateNextLatLon(ref position, chordLine.Item2);
                position.Heading_Mag = endHeading;
            }
            else
            {
                if (position.Heading_Mag != AssignedHeading)
                {
                    position.Heading_Mag = AssignedHeading;
                }

                // Calculate new position
                AcftGeoUtil.CalculateNextLatLon(ref position, distanceTravelledNMi);
            }
        }

        public bool ShouldActivateInstruction(AcftData position, int posCalcInterval)
        {
            return true;
        }

        public override string ToString()
        {
            return $"HDG Hold: {AssignedHeading}";
        }
    }
}
