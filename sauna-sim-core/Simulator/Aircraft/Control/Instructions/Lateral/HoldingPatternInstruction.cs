using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.GeoTools.MagneticTools;
using AviationCalcUtilNet.MathTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS.Legs;

namespace SaunaSim.Core.Simulator.Aircraft.Control.Instructions.Lateral
{
    public enum HoldEntryEnum
    {
        DIRECT,
        TEARDROP,
        PARALLEL,
        NONE
    }

    public enum HoldPhaseEnum
    {
        ENTRY,
        TURN_OUTBOUND,
        OUTBOUND,
        TURN_INBOUND,
        INBOUND
    }

    public class HoldingPatternInstruction : ILateralControlInstruction
    {
        private HoldPhaseEnum _holdPhase;
        private HoldEntryEnum _holdEntry;
        private IRoutePoint _routePoint;
        private double _magneticCourse;
        private double _trueCourse;
        private HoldTurnDirectionEnum _turnDir;
        private HoldLegLengthTypeEnum _legLengthType;
        private double _legLength;
        private InterceptCourseInstruction _inboundCourseInstr;
        private InterceptCourseInstruction _outboundCourseInstr;
        private TrackHoldInstruction _turnInstr;
        private double _r;
        private double _teardropDistance;

        public HoldingPatternInstruction(IRoutePoint holdingPoint, BearingTypeEnum courseType, double inboundCourse, HoldTurnDirectionEnum turnDir, HoldLegLengthTypeEnum legType, double legLength)
        {
            _holdPhase = HoldPhaseEnum.ENTRY;
            _holdEntry = HoldEntryEnum.NONE;
            _routePoint = holdingPoint;

            if (courseType == BearingTypeEnum.TRUE)
            {
                _trueCourse = inboundCourse;
                _magneticCourse = MagneticUtil.ConvertTrueToMagneticTile(_trueCourse, holdingPoint.PointPosition);
            }
            else
            {
                _magneticCourse = inboundCourse;
                _trueCourse = MagneticUtil.ConvertMagneticToTrueTile(_magneticCourse, holdingPoint.PointPosition);
            }

            _turnDir = turnDir;
            _legLengthType = legType;

            if (_legLengthType == HoldLegLengthTypeEnum.DEFAULT)
            {
                _legLength = -1;
            }
            else
            {
                _legLength = legLength;
            }
        }

        public HoldingPatternInstruction(IRoutePoint holdingPoint, BearingTypeEnum courseType, double inboundCourse, HoldTurnDirectionEnum turnDir) :
            this(holdingPoint, courseType, inboundCourse, turnDir, HoldLegLengthTypeEnum.DEFAULT, -1)
        { }

        public HoldingPatternInstruction(IRoutePoint holdingPoint, BearingTypeEnum courseType, double inboundCourse) :
            this(holdingPoint, courseType, inboundCourse, HoldTurnDirectionEnum.RIGHT)
        { }

        public LateralControlMode Type => LateralControlMode.HOLDING_PATTERN;

        public HoldPhaseEnum HoldPhase => _holdPhase;

        public InterceptCourseInstruction Instruction => _inboundCourseInstr;

        public double MagneticCourse => _magneticCourse;
        public double TrueCourse => _trueCourse;

        public bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            // Holds never get activated early 
            return false;
        }

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            // Check if we need to enter the hold
            if (_holdPhase == HoldPhaseEnum.ENTRY)
            {
                HandleHoldEntry(ref position, ref fms, posCalcInterval);
            }
            else if (_holdPhase == HoldPhaseEnum.TURN_OUTBOUND)
            {
                HandleOutboundTurn(ref position, ref fms, posCalcInterval);
            }
            else if (_holdPhase == HoldPhaseEnum.OUTBOUND)
            {
                HandleOutboundLeg(ref position, ref fms, posCalcInterval);
            }
            else if (_holdPhase == HoldPhaseEnum.TURN_INBOUND)
            {
                HandleInboundTurn(ref position, ref fms, posCalcInterval);
            }
            else
            {
                HandleInboundLeg(ref position, ref fms, posCalcInterval);
            }
        }

        private void HandleHoldEntry(ref AircraftPosition position, ref AircraftFms fms, int posCalcIntvl)
        {
            if (_holdEntry == HoldEntryEnum.NONE)
            {
                DetermineHoldEntry(position);
            }

            if (_holdEntry == HoldEntryEnum.DIRECT)
            {
                _holdPhase = HoldPhaseEnum.TURN_OUTBOUND;
                HandleOutboundTurn(ref position, ref fms, posCalcIntvl);
            }
            else if (_holdEntry == HoldEntryEnum.TEARDROP)
            {
                if (_outboundCourseInstr == null)
                {
                    // Calculate required radius of turn
                    double turnAmt = GetTurnAmount();
                    double outCourse = GeoUtil.NormalizeHeading(_trueCourse + turnAmt);

                    // Calculate teardrop bearing and distance
                    GeoPoint startPoint = _routePoint.PointPosition;
                    double outDist = GetOutboundDistance(position);
                    GeoPoint outPoint = new GeoPoint(GetOutboundStartPoint(position).PointPosition);

                    outPoint.MoveByNMi(outCourse, outDist);
                    double tdBear = GeoPoint.InitialBearing(startPoint, outPoint);

                    outPoint.Alt = position.TrueAltitude;
                    _teardropDistance = GeoPoint.DistanceNMi(new GeoPoint(startPoint.Lat, startPoint.Lon, position.TrueAltitude), outPoint);

                    _outboundCourseInstr = new InterceptCourseInstruction(_routePoint)
                    {
                        TrueCourse = tdBear
                    };
                }

                // Check if we should turn inbound
                if (MathUtil.ConvertMetersToNauticalMiles(_outboundCourseInstr.AlongTrackM) <= -_teardropDistance)
                {
                    _holdPhase = HoldPhaseEnum.TURN_INBOUND;
                    _outboundCourseInstr = null;
                    HandleInboundTurn(ref position, ref fms, posCalcIntvl);
                }
                else
                {
                    _outboundCourseInstr.UpdatePosition(ref position, ref fms, posCalcIntvl);
                }
            }
            else
            {
                if (_outboundCourseInstr == null)
                {
                    double outCourse = GeoUtil.NormalizeHeading(_trueCourse + 180);

                    _outboundCourseInstr = new InterceptCourseInstruction(_routePoint);
                    _outboundCourseInstr.TrueCourse = outCourse;
                }

                // Check if we should turn inbound
                if (MathUtil.ConvertMetersToNauticalMiles(_outboundCourseInstr.AlongTrackM) <= -GetOutboundDistance(position))
                {
                    _holdPhase = HoldPhaseEnum.TURN_INBOUND;
                    _outboundCourseInstr = null;
                    double turnAmt = -GetTurnAmount();
                    TurnDirection dir = turnAmt >= 0 ? TurnDirection.RIGHT : TurnDirection.LEFT;
                    _turnInstr = new TrackHoldInstruction(dir, _trueCourse);
                    HandleInboundTurn(ref position, ref fms, posCalcIntvl);
                }
                else
                {
                    _outboundCourseInstr.UpdatePosition(ref position, ref fms, posCalcIntvl);
                }
            }
        }

        private IRoutePoint GetOutboundStartPoint(AircraftPosition position)
        {
            // Calculate required radius of turn
            double turnAmt = GetTurnAmount();
            double outCourse = GeoUtil.NormalizeHeading(_trueCourse + turnAmt);
            double outR = GeoUtil.CalculateConstantRadiusTurn(_trueCourse, turnAmt, position.WindDirection, position.WindSpeed, position.TrueAirSpeed);
            double inR = GeoUtil.CalculateConstantRadiusTurn(outCourse, turnAmt, position.WindDirection, position.WindSpeed, position.TrueAirSpeed);

            _r = Math.Max(outR, inR);
            double bearingToOutStart = GeoUtil.NormalizeHeading(_trueCourse + (turnAmt / 2));
            return new RoutePointPbd(_routePoint.PointPosition, bearingToOutStart, _r * 2, _routePoint.PointName);
        }

        private double GetTurnAmount()
        {
            return _turnDir == HoldTurnDirectionEnum.RIGHT ? 180 : -180;
        }

        private void HandleOutboundTurn(ref AircraftPosition position, ref AircraftFms fms, int posCalcIntvl)
        {
            double turnAmt = GetTurnAmount();
            double outCourse = GeoUtil.NormalizeHeading(_trueCourse + turnAmt);
            TurnDirection turnD = _turnDir == HoldTurnDirectionEnum.RIGHT ? TurnDirection.RIGHT : TurnDirection.LEFT;
            double halfTurnCourse = GeoUtil.NormalizeHeading(_trueCourse + (turnAmt / 2));

            // Calculate outbound turn parameters
            if (_turnInstr == null)
            {
                SetOutboundCourseInstr(position);
                _turnInstr = new TrackHoldInstruction(turnD, halfTurnCourse, _r);
            }

            if (Math.Abs(_turnInstr.AssignedTrack - outCourse) >= 1)
            {
                double alongTrackM;
                GeoUtil.CalculateCrossTrackErrorM(position.PositionGeoPoint, new GeoPoint(_routePoint.PointPosition), halfTurnCourse, out _, out alongTrackM);

                if (MathUtil.ConvertMetersToNauticalMiles(alongTrackM) <= -_r)
                {
                    _turnInstr = new TrackHoldInstruction(turnD, outCourse, _r);
                }
            }

            // Has turn finished
            if (Math.Abs(outCourse - position.Track_True) < 1)
            {
                _holdPhase = HoldPhaseEnum.OUTBOUND;
                _turnInstr = null;
                HandleOutboundLeg(ref position, ref fms, posCalcIntvl);
            }
            else
            {
                _turnInstr.UpdatePosition(ref position, ref fms, posCalcIntvl);
            }
        }

        private double GetOutboundDistance(AircraftPosition position)
        {
            if (_legLengthType == HoldLegLengthTypeEnum.DISTANCE)
            {
                return _legLength;
            }

            double legLengthMs;

            if (_legLengthType == HoldLegLengthTypeEnum.TIME)
            {
                legLengthMs = _legLength * 60000;
            }
            else
            {
                if (position.IndicatedAltitude < 14000)
                {
                    legLengthMs = 60000;
                }
                else
                {
                    legLengthMs = 90000;
                }
            }

            double inbdGs = GeoUtil.HeadwindComponent(position.WindSpeed, position.WindDirection, _trueCourse) + position.TrueAirSpeed;
            return GeoUtil.CalculateDistanceTravelledNMi(inbdGs, legLengthMs);
        }

        public void SetOutboundCourseInstr(AircraftPosition position)
        {
            double turnAmt = GetTurnAmount();
            double outCourse = GeoUtil.NormalizeHeading(_trueCourse + turnAmt);
            IRoutePoint outStartPoint = GetOutboundStartPoint(position);
            _outboundCourseInstr = new InterceptCourseInstruction(outStartPoint);
            _outboundCourseInstr.TrueCourse = outCourse;
        }

        private void HandleOutboundLeg(ref AircraftPosition position, ref AircraftFms fms, int posCalcIntvl)
        {
            if (_outboundCourseInstr == null)
            {
                SetOutboundCourseInstr(position);
            }

            // Has leg finished
            double aTrackNMi = MathUtil.ConvertMetersToNauticalMiles(_outboundCourseInstr.AlongTrackM);
            double obDistNMi = -GetOutboundDistance(position);
            if (aTrackNMi <= obDistNMi)
            {
                _holdPhase = HoldPhaseEnum.TURN_INBOUND;
                _outboundCourseInstr = null;
                HandleInboundTurn(ref position, ref fms, posCalcIntvl);
            }
            else
            {
                _outboundCourseInstr.UpdatePosition(ref position, ref fms, posCalcIntvl);
            }
        }

        private void HandleInboundTurn(ref AircraftPosition position, ref AircraftFms fms, int posCalcIntvl)
        {
            // Calculate outbound turn parameters
            if (_turnInstr == null)
            {
                TurnDirection turnD = _turnDir == HoldTurnDirectionEnum.RIGHT ? TurnDirection.RIGHT : TurnDirection.LEFT;
                _turnInstr = new TrackHoldInstruction(turnD, _trueCourse, _r);
            }

            // Has turn finished
            if (Math.Abs(_turnInstr.AssignedTrack - position.Track_True) < 1)
            {
                _holdPhase = HoldPhaseEnum.INBOUND;
                _turnInstr = null;
                HandleOutboundLeg(ref position, ref fms, posCalcIntvl);
            }
            else
            {
                _turnInstr.UpdatePosition(ref position, ref fms, posCalcIntvl);
            }
        }

        private void HandleInboundLeg(ref AircraftPosition position, ref AircraftFms fms, int posCalcIntvl)
        {
            if (_inboundCourseInstr == null)
            {
                _inboundCourseInstr = new InterceptCourseInstruction(_routePoint);
                _inboundCourseInstr.TrueCourse = _trueCourse;
            }

            // Check if leg is complete
            if (_inboundCourseInstr.AlongTrackM < 0)
            {
                _holdPhase = HoldPhaseEnum.TURN_OUTBOUND;
                _inboundCourseInstr = null;
                HandleOutboundTurn(ref position, ref fms, posCalcIntvl);
            }
            else
            {
                _inboundCourseInstr.UpdatePosition(ref position, ref fms, posCalcIntvl);
            }
        }

        private void DetermineHoldEntry(AircraftPosition position)
        {
            // Calculate hold entry
            double turnAmt = GeoUtil.CalculateTurnAmount(_trueCourse, position.Track_True);

            if (_turnDir == HoldTurnDirectionEnum.RIGHT)
            {
                if (turnAmt < -70 && turnAmt > -180)
                {
                    _holdEntry = HoldEntryEnum.PARALLEL;
                }
                else if (turnAmt <= 180 && turnAmt > 110)
                {
                    _holdEntry = HoldEntryEnum.TEARDROP;
                }
                else
                {
                    _holdEntry = HoldEntryEnum.DIRECT;
                }
            }
            else
            {
                if (turnAmt > 70 && turnAmt < 180)
                {
                    _holdEntry = HoldEntryEnum.PARALLEL;
                }
                else if (turnAmt >= -180 && turnAmt < -110)
                {
                    _holdEntry = HoldEntryEnum.TEARDROP;
                }
                else
                {
                    _holdEntry = HoldEntryEnum.DIRECT;
                }
            }
        }
    }
}
