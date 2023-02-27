using AviationCalcUtilNet.GeoTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaunaSim.Core.Simulator.Aircraft.Control;
using SaunaSim.Core.Simulator.Aircraft.FMS;

namespace SaunaSim.Core.Simulator.Aircraft
{
    public class TrackHoldInstruction : ILateralControlInstruction
    {
        private double _radiusOfTurn;
        public LateralControlMode Type => LateralControlMode.TRACK_HOLD;

        public double AssignedTrack { get; private set; }

        public TurnDirection TurnDir { get; private set; }

        public TrackHoldInstruction(TurnDirection turnDir, double assignedTrack, double radiusOfTurn)
        {
            TurnDir = turnDir;
            AssignedTrack = GeoUtil.NormalizeHeading(assignedTrack);
            _radiusOfTurn = radiusOfTurn;
        }

        public TrackHoldInstruction(TurnDirection turnDir, double assignedTrack) : this(turnDir, assignedTrack, -1) { }

        public TrackHoldInstruction(double assignedTrack) : this(TurnDirection.SHORTEST, assignedTrack) { }

        public TrackHoldInstruction(double assignedTrack, double radiusOfTurn) : this(TurnDirection.SHORTEST, assignedTrack, radiusOfTurn) { }

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            // Calculate next position and track
            double turnAmount = GeoUtil.CalculateTurnAmount(position.Track_True, AssignedTrack);
            double distanceTravelledNMi = GeoUtil.CalculateDistanceTravelledNMi(position.GroundSpeed, posCalcInterval);

            if (Math.Abs(turnAmount) > 1)
            {
                // Calculate bank angle & radius of turn
                double bankAngle;
                double r;

                if (_radiusOfTurn < 0)
                {
                    bankAngle = GeoUtil.CalculateMaxBankAngle(position.GroundSpeed, 25, 3);
                    r = GeoUtil.CalculateRadiusOfTurn(bankAngle, position.GroundSpeed);
                }
                else
                {
                    bankAngle = GeoUtil.CalculateBankAngle(_radiusOfTurn, position.GroundSpeed);
                    r = _radiusOfTurn;
                }

                // Calculate degrees to turn
                double degreesToTurn = Math.Min(Math.Abs(turnAmount), GeoUtil.CalculateDegreesTurned(distanceTravelledNMi, r));

                // Figure out turn direction
                bool isRightTurn = (TurnDir == TurnDirection.SHORTEST) ?
                    (isRightTurn = turnAmount > 0) :
                    (isRightTurn = TurnDir == TurnDirection.RIGHT);

                // Calculate end heading
                double endHeading = GeoUtil.CalculateEndHeading(position.Track_True, degreesToTurn, isRightTurn);

                // Calculate chord line data
                Tuple<double, double> chordLine = GeoUtil.CalculateChordHeadingAndDistance(position.Track_True, degreesToTurn, r, isRightTurn);

                // Calculate new position
                position.Track_True = chordLine.Item1;
                AircraftPositionUtil.SetNextLatLon(ref position, chordLine.Item2);
                position.Track_True = endHeading;
            }
            else
            {
                position.Track_True = AssignedTrack;

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
            return $"TRK Hold: {AssignedTrack}";
        }
    }
}
