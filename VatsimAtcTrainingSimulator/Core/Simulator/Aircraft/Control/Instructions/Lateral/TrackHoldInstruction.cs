using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.GeoTools;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft
{
    public class TrackHoldInstruction : ILateralControlInstruction
    {
        public LateralControlMode Type => LateralControlMode.TRACK_HOLD;

        public double AssignedTrack { get; private set; }

        public TurnDirection TurnDir { get; private set; }

        public TrackHoldInstruction(TurnDirection turnDir, double assignedTrack)
        {
            this.TurnDir = turnDir;
            this.AssignedTrack = (assignedTrack >= 360) ? assignedTrack - 360 : assignedTrack; ;
        }

        public TrackHoldInstruction(double assignedTrack) : this(TurnDirection.SHORTEST, assignedTrack) { }

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            // Calculate next position and track
            double turnAmount = AcftGeoUtil.CalculateTurnAmount(position.Track_True, AssignedTrack);
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
                    (isRightTurn = turnAmount > 0) :
                    (isRightTurn = TurnDir == TurnDirection.RIGHT);

                // Calculate end heading
                double endHeading = AcftGeoUtil.CalculateEndHeading(position.Track_True, degreesToTurn, isRightTurn);

                // Calculate chord line data
                Tuple<double, double> chordLine = AcftGeoUtil.CalculateChordHeadingAndDistance(position.Track_True, degreesToTurn, radiusOfTurn, isRightTurn);

                // Calculate new position
                position.Track_True = chordLine.Item1;
                AcftGeoUtil.CalculateNextLatLon(ref position, chordLine.Item2);
                position.Track_True = endHeading;
            }
            else
            {
                position.Track_True = AssignedTrack;

                // Calculate new position
                AcftGeoUtil.CalculateNextLatLon(ref position, distanceTravelledNMi);
            }
        }

        public bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            return true;
        }

        public override string ToString()
        {
            return $"TRK Hold: {AssignedTrack}";
        }
    }
}
