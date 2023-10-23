using System;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.GeoTools.MagneticTools;
using AviationCalcUtilNet.MathTools;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;

namespace SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller
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

    public class ApFmsHoldController
    {
        private HoldPhaseEnum _holdPhase;
        private HoldEntryEnum _holdEntry;
        private IRoutePoint _routePoint;
        private double _magneticCourse;
        private double _trueCourse;
        private HoldTurnDirectionEnum _turnDir;
        private HoldLegLengthTypeEnum _legLengthType;
        private double _legLength;

        private IRoutePoint _outStartPoint = null;
        private IRoutePoint _outEndPoint = null;
        private IRoutePoint _inStartPoint = null;

        private RadiusToFixLeg _outboundTurnLeg = null;
        private TrackToFixLeg _outboundLeg = null;
        private RadiusToFixLeg _inboundTurnLeg = null;
        private TrackToFixLeg _inboundLeg = null;

        public double AlongTrack_M { get; private set; }

        public ApFmsHoldController(IRoutePoint holdingPoint, BearingTypeEnum courseType, double inboundCourse, HoldTurnDirectionEnum turnDir, HoldLegLengthTypeEnum legType, double legLength)
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

        public ApFmsHoldController(IRoutePoint holdingPoint, BearingTypeEnum courseType, double inboundCourse, HoldTurnDirectionEnum turnDir) :
            this(holdingPoint, courseType, inboundCourse, turnDir, HoldLegLengthTypeEnum.DEFAULT, -1)
        { }

        public ApFmsHoldController(IRoutePoint holdingPoint, BearingTypeEnum courseType, double inboundCourse) :
            this(holdingPoint, courseType, inboundCourse, HoldTurnDirectionEnum.RIGHT)
        { }

        public LateralControlMode Type => LateralControlMode.HOLDING_PATTERN;

        public HoldPhaseEnum HoldPhase => _holdPhase;

        public double MagneticCourse => _magneticCourse;
        public double TrueCourse => _trueCourse;

        public (double requiredTrueCourse, double crossTrackError, double turnRadius) UpdateForLnav(SimAircraft aircraft, int intervalMs)
        {
            // Check if we need to enter the hold
            if (_holdPhase == HoldPhaseEnum.ENTRY)
            {
                return HandleHoldEntry(aircraft, intervalMs);
            }
            else if (_holdPhase == HoldPhaseEnum.TURN_OUTBOUND)
            {
                return HandleOutboundTurn(aircraft, intervalMs);
            }
            else if (_holdPhase == HoldPhaseEnum.OUTBOUND)
            {
                return HandleOutboundLeg(aircraft, intervalMs);
            }
            else if (_holdPhase == HoldPhaseEnum.TURN_INBOUND)
            {
                return HandleInboundTurn(aircraft, intervalMs);
            }
            else
            {
                return HandleInboundLeg(aircraft, intervalMs);
            }
        }

        private (double requiredTrueCourse, double crossTrackError, double turnRadius) HandleHoldEntry(SimAircraft aircraft, int posCalcIntvl)
        {
            if (_holdEntry == HoldEntryEnum.NONE)
            {
                DetermineHoldEntry(aircraft.Position);
            }

            if (_holdEntry == HoldEntryEnum.DIRECT)
            {
                CalculateDirectEntry(aircraft);
                _holdPhase = HoldPhaseEnum.TURN_OUTBOUND;
                return HandleOutboundTurn(aircraft, posCalcIntvl);
            }
            else if (_holdEntry == HoldEntryEnum.TEARDROP)
            {
                CalculateTeardropEntry(aircraft);
                _holdPhase = HoldPhaseEnum.OUTBOUND;
                return HandleOutboundLeg(aircraft, posCalcIntvl);
            }
            else
            {
                CalculateParallelEntry(aircraft);
                _holdPhase = HoldPhaseEnum.OUTBOUND;
                return HandleOutboundLeg(aircraft, posCalcIntvl);
            }
        }

        private double GetTurnAmount()
        {
            return _turnDir == HoldTurnDirectionEnum.RIGHT ? 180 : -180;
        }

        private double CalculateMinRadiusOfTurn(double turnAmt, double inboundCourse, double outboundCourse, double windDir, double windSpd, double tas)
        {
            double outR = GeoUtil.CalculateConstantRadiusTurn(inboundCourse, turnAmt, windDir, windSpd, tas);
            double inR = GeoUtil.CalculateConstantRadiusTurn(outboundCourse, turnAmt, windDir, windSpd, tas);
            return Math.Max(outR, inR);
        }

        private void CalculateParallelEntry(SimAircraft aircraft)
        {
            // Find courses and leg lengths
            double turnAmt = GetTurnAmount();
            double outboundCourse = GeoUtil.NormalizeHeading(_trueCourse + turnAmt);
            double outboundLegLength = GetOutboundDistance(aircraft.Position);
            double aircraftTurnAmt = GeoUtil.CalculateTurnAmount(aircraft.Position.Track_True, outboundCourse);
            (double chordHdg, double chordDist) = GeoUtil.CalculateChordHeadingAndDistance(
                aircraft.Position.Track_True,
                Math.Abs(aircraftTurnAmt),
                GeoUtil.CalculateConstantRadiusTurn(aircraft.Position.Track_True, aircraftTurnAmt, aircraft.Position.WindDirection, aircraft.Position.WindSpeed, aircraft.Position.TrueAirSpeed),
                aircraftTurnAmt > 0
                );

            // Calculate required radius of turn
            double r = CalculateMinRadiusOfTurn(turnAmt, _trueCourse, outboundCourse, aircraft.Position.WindDirection, aircraft.Position.WindSpeed, aircraft.Position.TrueAirSpeed);

            // Find Points
            _outStartPoint = new RoutePointPbd(_routePoint.PointPosition, chordHdg, chordDist, $"{_routePoint.PointName}H_OS");
            GeoPoint tempOsPoint = new GeoPoint(_routePoint.PointPosition);
            tempOsPoint.MoveByNMi(outboundCourse, outboundLegLength);
            double finOsBearing = GeoPoint.FinalBearing(_routePoint.PointPosition, tempOsPoint);
            GeoPoint intersection = GeoPoint.Intersection(tempOsPoint, GeoUtil.NormalizeHeading(finOsBearing + (turnAmt / 2)), _outStartPoint.PointPosition, outboundCourse);

            if (intersection == null)
            {
                intersection = tempOsPoint;
            }

            _outEndPoint = new RouteWaypoint(intersection);
            double oeCourse = GeoPoint.FinalBearing(_outStartPoint.PointPosition, _outEndPoint.PointPosition);
            _inStartPoint = new RoutePointPbd(_outEndPoint.PointPosition, GeoUtil.NormalizeHeading(oeCourse - (turnAmt / 2)), r * 2, $"{_routePoint.PointName}H_IS1");

            // Create FmsPoints
            FmsPoint iePoint = new FmsPoint(_routePoint, RoutePointTypeEnum.FLY_OVER);
            FmsPoint osPoint = new FmsPoint(_outStartPoint, RoutePointTypeEnum.FLY_OVER);
            FmsPoint oePoint = new FmsPoint(_outEndPoint, RoutePointTypeEnum.FLY_OVER);
            FmsPoint isPoint = new FmsPoint(_inStartPoint, RoutePointTypeEnum.FLY_OVER);
            FmsPoint is2Point = new FmsPoint(new RouteWaypoint(tempOsPoint), RoutePointTypeEnum.FLY_BY);

            // Prepare legs
            _outboundLeg = new TrackToFixLeg(osPoint, oePoint);
            _inboundTurnLeg = new RadiusToFixLeg(oePoint, isPoint, oeCourse, GeoPoint.InitialBearing(tempOsPoint, _routePoint.PointPosition));
            _inboundLeg = new TrackToFixLeg(is2Point, iePoint);
        }

        private void CalculateTeardropEntry(SimAircraft aircraft)
        {
            // Find courses and leg lengths
            double turnAmt = GetTurnAmount();
            double outboundCourse = GeoUtil.NormalizeHeading(_trueCourse + turnAmt);
            double outboundLegLength = GetOutboundDistance(aircraft.Position);

            // Calculate required radius of turn
            double r = CalculateMinRadiusOfTurn(turnAmt, _trueCourse, outboundCourse, aircraft.Position.WindDirection, aircraft.Position.WindSpeed, aircraft.Position.TrueAirSpeed);

            // Find Points
            _inStartPoint = new RoutePointPbd(_routePoint.PointPosition, outboundCourse, outboundLegLength, $"{_routePoint.PointName}H_IS");
            double bearingToOutStart = GeoUtil.NormalizeHeading(GeoPoint.FinalBearing(_routePoint.PointPosition, _inStartPoint.PointPosition) + (turnAmt / 2));
            _outEndPoint = new RoutePointPbd(_inStartPoint.PointPosition, bearingToOutStart, r * 2, $"{_routePoint.PointName}H_OE");

            double dirFinalCourse = GeoUtil.CalculateDirectBearingAfterTurn(
                    aircraft.Position.PositionGeoPoint,
                    _outEndPoint.PointPosition,
                    GeoUtil.CalculateRadiusOfTurn(GeoUtil.CalculateMaxBankAngle(aircraft.Position.GroundSpeed, 25, 3), aircraft.Position.GroundSpeed),
                    aircraft.Position.Track_True);

            if (dirFinalCourse < 0)
            {
                dirFinalCourse = GeoPoint.FinalBearing(aircraft.Position.PositionGeoPoint, _outEndPoint.PointPosition);
            }

            _outStartPoint = new RoutePointPbd(_outEndPoint.PointPosition, GeoUtil.NormalizeHeading(dirFinalCourse + 180), GeoPoint.FlatDistanceNMi(aircraft.Position.PositionGeoPoint, _outEndPoint.PointPosition), $"{_routePoint.PointName}H_OS");

            // Create FmsPoints
            FmsPoint iePoint = new FmsPoint(_routePoint, RoutePointTypeEnum.FLY_OVER);
            FmsPoint osPoint = new FmsPoint(_outStartPoint, RoutePointTypeEnum.FLY_OVER);
            FmsPoint oePoint = new FmsPoint(_outEndPoint, RoutePointTypeEnum.FLY_OVER);
            FmsPoint isPoint = new FmsPoint(_inStartPoint, RoutePointTypeEnum.FLY_OVER);

            // Find other courses
            double isCourse = GeoPoint.InitialBearing(_inStartPoint.PointPosition, _routePoint.PointPosition);

            // Prepare legs
            _outboundLeg = new TrackToFixLeg(osPoint, oePoint);
            _inboundTurnLeg = new RadiusToFixLeg(oePoint, isPoint, dirFinalCourse, isCourse);
            _inboundLeg = new TrackToFixLeg(isPoint, iePoint);
        }

        private void CalculateDirectEntry(SimAircraft aircraft)
        {
            // Find courses and leg lengths
            double turnAmt = GetTurnAmount();
            double outboundCourse = GeoUtil.NormalizeHeading(_trueCourse + turnAmt);
            double outboundLegLength = GetOutboundDistance(aircraft.Position);

            // Calculate required radius of turn
            double r = CalculateMinRadiusOfTurn(turnAmt, _trueCourse, outboundCourse, aircraft.Position.WindDirection, aircraft.Position.WindSpeed, aircraft.Position.TrueAirSpeed);

            // Find Points
            _inStartPoint = new RoutePointPbd(_routePoint.PointPosition, outboundCourse, outboundLegLength, $"{_routePoint.PointName}H_IS");
            double bearingToOutStart = GeoUtil.NormalizeHeading(GeoPoint.FinalBearing(_routePoint.PointPosition, _inStartPoint.PointPosition) + (turnAmt / 2));
            _outEndPoint = new RoutePointPbd(_inStartPoint.PointPosition, bearingToOutStart, r * 2, $"{_routePoint.PointName}H_OE");
            _outStartPoint = new RoutePointPbd(_outEndPoint.PointPosition, GeoUtil.NormalizeHeading(bearingToOutStart - (turnAmt / 2)), MathUtil.ConvertMetersToNauticalMiles(10), $"{_routePoint.PointName}H_OS");

            // Find other courses
            double oeCourse = GeoPoint.FinalBearing(_outStartPoint.PointPosition, _outEndPoint.PointPosition);
            double osCourse = GeoPoint.InitialBearing(_outStartPoint.PointPosition, _outEndPoint.PointPosition);
            double isCourse = GeoPoint.InitialBearing(_inStartPoint.PointPosition, _routePoint.PointPosition);

            // Create FmsPoints
            FmsPoint iePoint = new FmsPoint(_routePoint, RoutePointTypeEnum.FLY_OVER);
            FmsPoint osPoint = new FmsPoint(_outStartPoint, RoutePointTypeEnum.FLY_OVER);
            FmsPoint oePoint = new FmsPoint(_outEndPoint, RoutePointTypeEnum.FLY_OVER);
            FmsPoint isPoint = new FmsPoint(_inStartPoint, RoutePointTypeEnum.FLY_OVER);

            // Prepare legs
            _outboundTurnLeg = new RadiusToFixLeg(iePoint, osPoint, aircraft.Position.Track_True, osCourse);
            _outboundLeg = new TrackToFixLeg(osPoint, oePoint);
            _inboundTurnLeg = new RadiusToFixLeg(oePoint, isPoint, oeCourse, isCourse);
            _inboundLeg = new TrackToFixLeg(isPoint, iePoint);
        }

        private void CalculateHold(SimAircraft aircraft)
        {
            // Find Courses and leg lengths
            double turnAmt = GetTurnAmount();
            double outboundCourse = GeoUtil.NormalizeHeading(_trueCourse + turnAmt);
            double outboundLegLength = GetOutboundDistance(aircraft.Position);

            // Calculate required radius of turn
            double r = CalculateMinRadiusOfTurn(turnAmt, _trueCourse, outboundCourse, aircraft.Position.WindDirection, aircraft.Position.WindSpeed, aircraft.Position.TrueAirSpeed);
            double bearingToOutStart = GeoUtil.NormalizeHeading(_trueCourse + (turnAmt / 2));

            // Find Points
            _outStartPoint = new RoutePointPbd(_routePoint.PointPosition, bearingToOutStart, r * 2, $"{_routePoint.PointName}H_OS");
            _outEndPoint = new RoutePointPbd(_outStartPoint.PointPosition, outboundCourse, outboundLegLength, $"{_routePoint.PointName}H_OE");
            _inStartPoint = new RoutePointPbd(_routePoint.PointPosition, outboundCourse, outboundLegLength, $"{_routePoint.PointName}H_IS");

            // Find other courses
            double oeCourse = GeoPoint.FinalBearing(_outStartPoint.PointPosition, _outEndPoint.PointPosition);
            double isCourse = GeoPoint.InitialBearing(_inStartPoint.PointPosition, _routePoint.PointPosition);

            // Create FmsPoints
            FmsPoint iePoint = new FmsPoint(_routePoint, RoutePointTypeEnum.FLY_OVER);
            FmsPoint osPoint = new FmsPoint(_outStartPoint, RoutePointTypeEnum.FLY_OVER);
            FmsPoint oePoint = new FmsPoint(_outEndPoint, RoutePointTypeEnum.FLY_OVER);
            FmsPoint isPoint = new FmsPoint(_inStartPoint, RoutePointTypeEnum.FLY_OVER);

            // Prepare legs
            _outboundTurnLeg = new RadiusToFixLeg(iePoint, osPoint, TrueCourse, outboundCourse);
            _outboundLeg = new TrackToFixLeg(osPoint, oePoint);
            _inboundTurnLeg = new RadiusToFixLeg(oePoint, isPoint, oeCourse, isCourse);
            _inboundLeg = new TrackToFixLeg(isPoint, iePoint);
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

        private (double requiredTrueCourse, double crossTrackError, double turnRadius) HandleOutboundTurn(SimAircraft aircraft, int posCalcIntvl)
        {
            if (_outboundTurnLeg.HasLegTerminated(aircraft))
            {
                _holdPhase = HoldPhaseEnum.OUTBOUND;
                return HandleOutboundLeg(aircraft, posCalcIntvl);
            }

            return _outboundTurnLeg.UpdateForLnav(aircraft, posCalcIntvl);
        }

        private (double requiredTrueCourse, double crossTrackError, double turnRadius) HandleOutboundLeg(SimAircraft aircraft, int posCalcIntvl)
        {
            if (_outboundLeg.HasLegTerminated(aircraft))
            {
                _holdPhase = HoldPhaseEnum.TURN_INBOUND;
                return HandleInboundTurn(aircraft, posCalcIntvl);
            }

            return _outboundLeg.UpdateForLnav(aircraft, posCalcIntvl);
        }

        private (double requiredTrueCourse, double crossTrackError, double turnRadius) HandleInboundTurn(SimAircraft aircraft, int posCalcIntvl)
        {
            if (_inboundTurnLeg.HasLegTerminated(aircraft))
            {
                _holdPhase = HoldPhaseEnum.INBOUND;
                return HandleInboundLeg(aircraft, posCalcIntvl);
            }

            return _inboundTurnLeg.UpdateForLnav(aircraft, posCalcIntvl);
        }

        private (double requiredTrueCourse, double crossTrackError, double turnRadius) HandleInboundLeg(SimAircraft aircraft, int posCalcIntvl)
        {
            if (_inboundLeg.HasLegTerminated(aircraft))
            {
                // Recalculate Hold dimensions
                CalculateHold(aircraft);

                _holdPhase = HoldPhaseEnum.TURN_OUTBOUND;
                return HandleOutboundTurn(aircraft, posCalcIntvl);
            }

            return _inboundLeg.UpdateForLnav(aircraft, posCalcIntvl);
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

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            double crossTrackError = GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _routePoint.PointPosition, _trueCourse,
    out double requiredTrueCourse, out double alongTrackDistance);

            return (requiredTrueCourse, crossTrackError, alongTrackDistance);
        }
    }
}

