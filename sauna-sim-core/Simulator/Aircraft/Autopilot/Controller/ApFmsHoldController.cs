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
        private double _r;
        private double _teardropDistance;
        private double _outCourse = -1;
        private IRoutePoint _outStartPoint = null;

        private RadiusToFixLeg _outboundTurnLeg = null;
        private RadiusToFixLeg _inboundTurnLeg = null;

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
                _holdPhase = HoldPhaseEnum.TURN_OUTBOUND;
                CalculateHold(aircraft);
                return HandleOutboundTurn(aircraft, posCalcIntvl);
            }
            else if (_holdEntry == HoldEntryEnum.TEARDROP)
            {
                if (_outCourse < 0)
                {
                    // Calculate required radius of turn
                    double turnAmt = GetTurnAmount();
                    double outCourse = GeoUtil.NormalizeHeading(_trueCourse + turnAmt);

                    // Calculate teardrop bearing and distance
                    GeoPoint startPoint = _routePoint.PointPosition;
                    double outDist = GetOutboundDistance(aircraft.Position);
                    GeoPoint outPoint = new GeoPoint(GetOutboundStartPoint(aircraft.Position).PointPosition);

                    outPoint.MoveByNMi(outCourse, outDist);
                    double tdBear = GeoPoint.InitialBearing(startPoint, outPoint);

                    outPoint.Alt = aircraft.Position.TrueAltitude;
                    _teardropDistance = GeoPoint.DistanceNMi(new GeoPoint(startPoint.Lat, startPoint.Lon, aircraft.Position.TrueAltitude), outPoint);

                    _outStartPoint = _routePoint;
                    _outCourse = tdBear;
                }

                // Check if we should turn inbound
                double crossTrackError = GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _outStartPoint.PointPosition, _outCourse,
                out double requiredTrueCourse, out double alongTrackDistance);
                if (MathUtil.ConvertMetersToNauticalMiles(alongTrackDistance) <= -_teardropDistance)
                {
                    


                    return HandleInboundTurn(aircraft, posCalcIntvl);
                }
                else
                {
                    return (requiredTrueCourse, crossTrackError, 0);
                }
            }
            else
            {
                if (_outCourse < 0)
                {
                    _outCourse = GeoUtil.NormalizeHeading(_trueCourse + 180);
                    _outStartPoint = _routePoint;
                }

                // Check if we should turn inbound
                double crossTrackError = GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _outStartPoint.PointPosition, _outCourse,
                out double requiredTrueCourse, out double alongTrackDistance);
                if (MathUtil.ConvertMetersToNauticalMiles(alongTrackDistance) <= -GetOutboundDistance(aircraft.Position))
                {
                    _holdPhase = HoldPhaseEnum.TURN_INBOUND;
                    _outCourse = -1;
                    _outStartPoint = null;
                    double turnAmt = -GetTurnAmount();
                    TurnDirection dir = turnAmt >= 0 ? TurnDirection.RIGHT : TurnDirection.LEFT;
                    return HandleInboundTurn(aircraft, posCalcIntvl);
                }
                else
                {
                    return (requiredTrueCourse, crossTrackError, 0);
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

        private void CalculateHold(SimAircraft aircraft)
        {
            // Prepare outbound leg
            double turnAmt = GetTurnAmount();
            _outCourse = GeoUtil.NormalizeHeading(_trueCourse + turnAmt);
            _outStartPoint = GetOutboundStartPoint(aircraft.Position);

            // Prepare RF turn to outbound leg
            _outboundTurnLeg = new RadiusToFixLeg(new FmsPoint(new RouteWaypoint(_routePoint.PointPosition), RoutePointTypeEnum.FLY_OVER), new FmsPoint(new RouteWaypoint(_outStartPoint.PointPosition), RoutePointTypeEnum.FLY_OVER), TrueCourse, _outCourse);
        }

        private (double requiredTrueCourse, double crossTrackError, double turnRadius) HandleOutboundTurn(SimAircraft aircraft, int posCalcIntvl)
        {
            if (_outboundTurnLeg.HasLegTerminated(aircraft))
            {
                return HandleOutboundLeg(aircraft, posCalcIntvl);
            } else
            {
                return _outboundTurnLeg.UpdateForLnav(aircraft, posCalcIntvl);
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

            // TODO: This needs to be changed (store result, not calculate it every time)

            double inbdGs = GeoUtil.HeadwindComponent(position.WindSpeed, position.WindDirection, _trueCourse) + position.TrueAirSpeed;
            return GeoUtil.CalculateDistanceTravelledNMi(inbdGs, legLengthMs);
        }

        private (double requiredTrueCourse, double crossTrackError, double turnRadius) HandleOutboundLeg(SimAircraft aircraft, int posCalcIntvl)
        {
            double crossTrackError = GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _outStartPoint.PointPosition, _outCourse,
                out double requiredTrueCourse, out double alongTrackDistance);

            // Has leg finished
            double aTrackNMi = MathUtil.ConvertMetersToNauticalMiles(alongTrackDistance);
            double obDistNMi = -GetOutboundDistance(aircraft.Position);
            if (aTrackNMi <= obDistNMi)
            {
                return StartInboundTurn(aircraft, posCalcIntvl);
            }
            else
            {
                return (requiredTrueCourse, crossTrackError, -1);
            }
        }

        private (double requiredTrueCourse, double crossTrackError, double turnRadius) StartInboundTurn(SimAircraft aircraft, int posCalcIntvl)
        {
            _holdPhase = HoldPhaseEnum.TURN_INBOUND;

            GeoPoint outEndPoint = new GeoPoint(_outStartPoint.PointPosition);
            outEndPoint.MoveByNMi(_outCourse, GetOutboundDistance(aircraft.Position));

            double finalOutCourse = GeoPoint.FinalBearing(_outStartPoint.PointPosition, outEndPoint);

            GeoPoint inboundTurnEndPoint = GeoUtil.FindIntersection(outEndPoint, _routePoint.PointPosition, GeoUtil.NormalizeHeading(finalOutCourse + 90), TrueCourse);

            _inboundTurnLeg = new RadiusToFixLeg(new FmsPoint(new RouteWaypoint(outEndPoint), RoutePointTypeEnum.FLY_OVER), new FmsPoint(new RouteWaypoint(inboundTurnEndPoint), RoutePointTypeEnum.FLY_OVER), finalOutCourse, TrueCourse);

            _outCourse = -1;
            _outStartPoint = null;

            return HandleInboundTurn(aircraft, posCalcIntvl);
        }

        private (double requiredTrueCourse, double crossTrackError, double turnRadius) HandleInboundTurn(SimAircraft aircraft, int posCalcIntvl)
        {
            if (_inboundTurnLeg.HasLegTerminated(aircraft))
            {
                _holdPhase = HoldPhaseEnum.INBOUND;
                return HandleInboundLeg(aircraft, posCalcIntvl);
            } else
            {
                return _inboundTurnLeg.UpdateForLnav(aircraft, posCalcIntvl);
            }
        }

        private (double requiredTrueCourse, double crossTrackError, double turnRadius) HandleInboundLeg(SimAircraft aircraft, int posCalcIntvl)
        {
            double crossTrackError = GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _routePoint.PointPosition, _trueCourse,
                out double requiredTrueCourse, out double alongTrackDistance);

            AlongTrack_M = alongTrackDistance;

            // Check if leg is complete
            if (alongTrackDistance < 0)
            {
                return CalculateHold(aircraft, posCalcIntvl);
            }
            else
            {
                return (requiredTrueCourse, crossTrackError, -1);
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

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            double crossTrackError = GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _routePoint.PointPosition, _trueCourse,
    out double requiredTrueCourse, out double alongTrackDistance);

            return (requiredTrueCourse, crossTrackError, alongTrackDistance);
        }
    }
}

