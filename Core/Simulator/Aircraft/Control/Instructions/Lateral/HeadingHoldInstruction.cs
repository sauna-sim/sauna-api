using AviationCalcUtilNet.GeoTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft
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

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            // Calculate next position and heading
            double turnAmount = GeoUtil.CalculateTurnAmount(position.Heading_Mag, AssignedHeading);
            double distanceTravelledNMi = GeoUtil.CalculateDistanceTravelledNMi(position.GroundSpeed, posCalcInterval);

            if (Math.Abs(turnAmount) > 1)
            {
                // Calculate bank angle
                double bankAngle = GeoUtil.CalculateMaxBankAngle(position.GroundSpeed, 25, 3);

                // Calculate radius of turn
                double radiusOfTurn = GeoUtil.CalculateRadiusOfTurn(bankAngle, position.GroundSpeed);

                // Calculate degrees to turn
                double degreesToTurn = Math.Min(Math.Abs(turnAmount), GeoUtil.CalculateDegreesTurned(distanceTravelledNMi, radiusOfTurn));

                // Figure out turn direction
                bool isRightTurn = (TurnDir == TurnDirection.SHORTEST) ?
                    (turnAmount > 0) :
                    (TurnDir == TurnDirection.RIGHT);

                // Calculate end heading
                double endHeading = GeoUtil.CalculateEndHeading(position.Heading_Mag, degreesToTurn, isRightTurn);

                // Calculate chord line data
                Tuple<double, double> chordLine = GeoUtil.CalculateChordHeadingAndDistance(position.Heading_Mag, degreesToTurn, radiusOfTurn, isRightTurn);

                // Calculate new position
                position.Heading_Mag = chordLine.Item1;
                AircraftPositionUtil.SetNextLatLon(ref position, chordLine.Item2);
                position.Heading_Mag = endHeading;
            }
            else
            {
                if (position.Heading_Mag != AssignedHeading)
                {
                    position.Heading_Mag = AssignedHeading;
                }

                // Calculate new position
                AircraftPositionUtil.SetNextLatLon(ref position, distanceTravelledNMi);
            }
        }

        public bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            return true;
        }

        public override string ToString()
        {
            return $"HDG Hold: {AssignedHeading}";
        }
    }
}
