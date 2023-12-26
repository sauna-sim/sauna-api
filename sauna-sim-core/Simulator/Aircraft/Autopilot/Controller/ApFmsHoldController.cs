using System;
using System.Collections.Generic;
using System.Net;
using AviationCalcUtilNet.Aviation;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Magnetic;
using AviationCalcUtilNet.Units;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

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
        private Bearing _magneticCourse;
        private Bearing _trueCourse;
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

        public RadiusToFixLeg OutboundTurnLeg => _outboundTurnLeg;
        public TrackToFixLeg OutboundLeg => _outboundLeg;
        public RadiusToFixLeg InboundTurnLeg => _inboundTurnLeg;
        public TrackToFixLeg InboundLeg => _inboundLeg;

        public Length AlongTrack_M { get; private set; }

        public Length CrossTrack_M { get; private set; }

        public Bearing CurrentTrueCourse { get; private set; }

        public Length Radius_M { get; private set; }

        public ApFmsHoldController(IRoutePoint holdingPoint, BearingTypeEnum courseType, Bearing inboundCourse, HoldTurnDirectionEnum turnDir, HoldLegLengthTypeEnum legType, double legLength, MagneticTileManager magTileMgr)
        {
            _holdPhase = HoldPhaseEnum.ENTRY;
            _holdEntry = HoldEntryEnum.NONE;
            _routePoint = holdingPoint;
            ExitArmed = false;

            if (courseType == BearingTypeEnum.TRUE)
            {
                _trueCourse = inboundCourse;
                _magneticCourse = magTileMgr.TrueToMagnetic(holdingPoint.PointPosition, DateTime.UtcNow, _trueCourse);
            } else
            {
                _magneticCourse = inboundCourse;
                _trueCourse = magTileMgr.MagneticToTrue(holdingPoint.PointPosition, DateTime.UtcNow, _magneticCourse);
            }

            _turnDir = turnDir;
            _legLengthType = legType;

            if (_legLengthType == HoldLegLengthTypeEnum.DEFAULT)
            {
                _legLength = -1;
            } else
            {
                _legLength = legLength;
            }
        }

        public ApFmsHoldController(IRoutePoint holdingPoint, BearingTypeEnum courseType, Bearing inboundCourse, HoldTurnDirectionEnum turnDir, MagneticTileManager magTileMgr) :
            this(holdingPoint, courseType, inboundCourse, turnDir, HoldLegLengthTypeEnum.DEFAULT, -1, magTileMgr)
        { }

        public ApFmsHoldController(IRoutePoint holdingPoint, BearingTypeEnum courseType, Bearing inboundCourse, MagneticTileManager magTileMgr) :
            this(holdingPoint, courseType, inboundCourse, HoldTurnDirectionEnum.RIGHT, magTileMgr)
        { }

        public LateralControlMode Type => LateralControlMode.HOLDING_PATTERN;

        public HoldPhaseEnum HoldPhase => _holdPhase;

        public Bearing MagneticCourse => _magneticCourse;
        public Bearing TrueCourse => _trueCourse;

        public bool ExitArmed { get; set; }

        public void ProcessLeg(SimAircraft aircraft, int intervalMs)
        {
            // Check if we need to enter the hold
            if (_holdPhase == HoldPhaseEnum.ENTRY)
            {
                HandleHoldEntry(aircraft, intervalMs);
            } else if (_holdPhase == HoldPhaseEnum.TURN_OUTBOUND)
            {
                HandleOutboundTurn(aircraft, intervalMs);
            } else if (_holdPhase == HoldPhaseEnum.OUTBOUND)
            {
                HandleOutboundLeg(aircraft, intervalMs);
            } else if (_holdPhase == HoldPhaseEnum.TURN_INBOUND)
            {
                HandleInboundTurn(aircraft, intervalMs);
            } else
            {
                HandleInboundLeg(aircraft, intervalMs);
            }
        }

        private void HandleHoldEntry(SimAircraft aircraft, int posCalcIntvl)
        {
            if (_holdEntry == HoldEntryEnum.NONE)
            {
                DetermineHoldEntry(aircraft.Position);
            }

            if (_holdEntry == HoldEntryEnum.DIRECT)
            {
                CalculateDirectEntry(aircraft);
                _holdPhase = HoldPhaseEnum.TURN_OUTBOUND;
            } else if (_holdEntry == HoldEntryEnum.TEARDROP)
            {
                CalculateTeardropEntry(aircraft);
                _holdPhase = HoldPhaseEnum.OUTBOUND;
            } else
            {
                CalculateParallelEntry(aircraft);
                _holdPhase = HoldPhaseEnum.OUTBOUND;
            }
        }

        private Angle GetTurnAmount()
        {
            return _turnDir == HoldTurnDirectionEnum.RIGHT ? Angle.FromDegrees(180) : Angle.FromDegrees(-180);
        }

        private Length CalculateMinRadiusOfTurn(Angle turnAmt, Bearing inboundCourse, Bearing outboundCourse, Bearing windDir, Velocity windSpd, Velocity tas)
        {
            Length outR = AviationUtil.CalculateConstantRadiusTurn(inboundCourse, turnAmt, windDir, windSpd, tas, Angle.FromDegrees(25), AngularVelocity.FromDegreesPerSecond(3));
            Length inR = AviationUtil.CalculateConstantRadiusTurn(outboundCourse, turnAmt, windDir, windSpd, tas, Angle.FromDegrees(25), AngularVelocity.FromDegreesPerSecond(3));
            return (Length) (Math.Max((double)outR, (double)inR) * AutopilotUtil.RADIUS_BUFFER_MULT);
        }

        private void CalculateParallelEntry(SimAircraft aircraft)
        {
            // Find courses and leg lengths
            Angle turnAmt = GetTurnAmount();
            Bearing outboundCourse = _trueCourse + turnAmt;
            Length outboundLegLength = GetOutboundDistance(aircraft.Position);
            Angle aircraftTurnAmt = outboundCourse - aircraft.Position.Track_True;
            (Bearing chordHdg, Length chordDist) = AviationUtil.CalculateChordForTurn(
                aircraft.Position.Track_True,
                aircraftTurnAmt,
                AviationUtil.CalculateConstantRadiusTurn(aircraft.Position.Track_True, aircraftTurnAmt, aircraft.Position.WindDirection, aircraft.Position.WindSpeed, aircraft.Position.TrueAirSpeed, Angle.FromDegrees(25), AngularVelocity.FromDegreesPerSecond(3))
                );

            // Calculate required radius of turn
            Length r = CalculateMinRadiusOfTurn(turnAmt, _trueCourse, outboundCourse, aircraft.Position.WindDirection, aircraft.Position.WindSpeed, aircraft.Position.TrueAirSpeed);

            // Find Points
            _outStartPoint = new RoutePointPbd(_routePoint.PointPosition, chordHdg, chordDist, $"{_routePoint.PointName}H_OS");
            GeoPoint tempOsPoint = (GeoPoint) _routePoint.PointPosition.Clone();
            tempOsPoint.MoveBy(outboundCourse, outboundLegLength);
            Bearing finOsBearing = GeoPoint.FinalBearing(_routePoint.PointPosition, tempOsPoint);
            GeoPoint intersection = GeoPoint.Intersection(tempOsPoint, finOsBearing + (turnAmt / 2), _outStartPoint.PointPosition, outboundCourse);

            if (intersection == null)
            {
                intersection = tempOsPoint;
            }

            _outEndPoint = new RouteWaypoint(intersection);
            Bearing oeCourse = GeoPoint.FinalBearing(_outStartPoint.PointPosition, _outEndPoint.PointPosition);
            _inStartPoint = new RoutePointPbd(_outEndPoint.PointPosition, oeCourse - (turnAmt / 2), r * 2, $"{_routePoint.PointName}H_IS1");

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
            Angle turnAmt = GetTurnAmount();
            Bearing outboundCourse = (_trueCourse + turnAmt);
            Length outboundLegLength = GetOutboundDistance(aircraft.Position);

            // Calculate required radius of turn
            Length r = CalculateMinRadiusOfTurn(turnAmt, _trueCourse, outboundCourse, aircraft.Position.WindDirection, aircraft.Position.WindSpeed, aircraft.Position.TrueAirSpeed);

            // Find Points
            _inStartPoint = new RoutePointPbd(_routePoint.PointPosition, outboundCourse, outboundLegLength, $"{_routePoint.PointName}H_IS");
            Bearing bearingToOutStart = (GeoPoint.FinalBearing(_routePoint.PointPosition, _inStartPoint.PointPosition) - (turnAmt / 2));
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
            double bearingToOutStart = GeoUtil.NormalizeHeading(GeoPoint.FinalBearing(_routePoint.PointPosition, _inStartPoint.PointPosition) - (turnAmt / 2));
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

        internal void CalculateHold(SimAircraft aircraft)
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

        private Length GetOutboundDistance(AircraftPosition position)
        {
            if (_legLengthType == HoldLegLengthTypeEnum.DISTANCE)
            {
                return Length.FromNauticalMiles(_legLength);
            }

            double legLengthMs;

            if (_legLengthType == HoldLegLengthTypeEnum.TIME)
            {
                legLengthMs = _legLength * 60000;
            } else
            {
                if (position.IndicatedAltitude.Feet < 14000)
                {
                    legLengthMs = 60000;
                } else
                {
                    legLengthMs = 90000;
                }
            }

            double inbdGs = GeoUtil.HeadwindComponent(position.WindSpeed, position.WindDirection, _trueCourse) + position.TrueAirSpeed;
            return GeoUtil.CalculateDistanceTravelledNMi(inbdGs, legLengthMs);
        }

        private void HandleOutboundTurn(SimAircraft aircraft, int posCalcIntvl)
        {
            if (_outboundTurnLeg.HasLegTerminated(aircraft))
            {
                _holdPhase = HoldPhaseEnum.OUTBOUND;
            }

            _outboundTurnLeg.ProcessLeg(aircraft, posCalcIntvl);
        }

        private void HandleOutboundLeg(SimAircraft aircraft, int posCalcIntvl)
        {
            if (_outboundLeg.HasLegTerminated(aircraft))
            {
                _holdPhase = HoldPhaseEnum.TURN_INBOUND;
            }
        }

        private void HandleInboundTurn(SimAircraft aircraft, int posCalcIntvl)
        {
            if (_inboundTurnLeg.HasLegTerminated(aircraft))
            {
                _holdPhase = HoldPhaseEnum.INBOUND;
            }

            _inboundTurnLeg.ProcessLeg(aircraft, posCalcIntvl);
        }

        private void HandleInboundLeg(SimAircraft aircraft, int posCalcIntvl)
        {
            if (_inboundLeg.HasLegTerminated(aircraft))
            {
                if (!ExitArmed)
                {
                    // Recalculate Hold dimensions
                    CalculateHold(aircraft);

                    _holdPhase = HoldPhaseEnum.TURN_OUTBOUND;
                }
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
                } else if (turnAmt <= 180 && turnAmt > 110)
                {
                    _holdEntry = HoldEntryEnum.TEARDROP;
                } else
                {
                    _holdEntry = HoldEntryEnum.DIRECT;
                }
            } else
            {
                if (turnAmt > 70 && turnAmt < 180)
                {
                    _holdEntry = HoldEntryEnum.PARALLEL;
                } else if (turnAmt >= -180 && turnAmt < -110)
                {
                    _holdEntry = HoldEntryEnum.TEARDROP;
                } else
                {
                    _holdEntry = HoldEntryEnum.DIRECT;
                }
            }
        }

        public (Bearing requiredTrueCourse, Length crossTrackError, Length alongTrackDistance, Length turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            switch (_holdPhase)
            {
                case HoldPhaseEnum.TURN_OUTBOUND:
                    return _outboundTurnLeg.GetCourseInterceptInfo(aircraft);
                case HoldPhaseEnum.OUTBOUND:
                    return _outboundLeg.GetCourseInterceptInfo(aircraft);
                case HoldPhaseEnum.TURN_INBOUND:
                    return _inboundTurnLeg.GetCourseInterceptInfo(aircraft);
                case HoldPhaseEnum.INBOUND:
                    return _inboundLeg.GetCourseInterceptInfo(aircraft);
            }


            double crossTrackError = GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _routePoint.PointPosition, _trueCourse,
out Length requiredTrueCourse, out double alongTrackDistance);

            return (requiredTrueCourse, crossTrackError, alongTrackDistance, 0);
        }

        public List<NdLine> GetUiLines()
        {
            var retList = new List<NdLine>();
            if (_outboundTurnLeg != null)
            {
                retList.AddRange(_outboundTurnLeg.UiLines);
            }
            if (_outboundLeg != null)
            {
                retList.AddRange(_outboundLeg.UiLines);
            }
            if (_inboundTurnLeg != null)
            {
                retList.AddRange(_inboundTurnLeg.UiLines);
            }
            if (_inboundLeg != null)
            {
                retList.AddRange(_inboundLeg.UiLines);
            }
            return retList;
        }
    }
}

