using AviationCalcUtilNet.GeoTools;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class RadiusToFixLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private FmsPoint _endPoint;

        public FmsPoint StartPoint => _startPoint;

        public FmsPoint EndPoint => _endPoint;

        private double _initialTrueCourse;
        private double _finalTrueCourse;

        public double InitialTrueCourse => _initialTrueCourse;

        public double FinalTrueCourse => _finalTrueCourse;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.RADIUS_TO_FIX;

        private TurnCircle _turnCircle;

        public TurnCircle TurnCircley => _turnCircle;

        private TrackToFixLeg _trackToRFLeg;
        private TrackToFixLeg _trackFromRFLeg;

        public class TurnCircle
        {
            public GeoPoint Center { get; set; }

            public GeoPoint TangentialPointA { get; set; }
            public double PointARadial => GeoPoint.InitialBearing(Center, TangentialPointA);

            public GeoPoint TangentialPointB { get; set; }
            public double PointBRadial => GeoPoint.InitialBearing(Center, TangentialPointB);

            public double RadiusM { get; set; }

            public override string ToString()
            {
                return $"Center: ({Center.Lat}, {Center.Lon}) Radius (m): {RadiusM}";
            }

            public TurnCircle(GeoPoint center, GeoPoint tangentialPointA, GeoPoint tangentialPointB, double radiusM)
            {
                Center = center;
                TangentialPointA = tangentialPointA;
                TangentialPointB = tangentialPointB;
                RadiusM = radiusM;
            }

            public GeoPoint BisectorIntersection { get; set; }
        }

        private enum RfState
        {
            TRACK_TO_RF,
            IN_RF,
            TRACK_FROM_RF
        }

        private RfState _legState = RfState.TRACK_TO_RF;

        public RadiusToFixLeg(FmsPoint startPoint, FmsPoint endPoint, double initialTrueCourse, double finalTrueCourse)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _initialTrueCourse = initialTrueCourse;
            _finalTrueCourse = finalTrueCourse;

            CalculateTurnCircle();

            // Initial subleg is Track To RF

            if (!StartPoint.Point.PointPosition.Equals(_turnCircle.TangentialPointA))
            {
                _trackToRFLeg = new TrackToFixLeg(StartPoint, new FmsPoint(new RouteWaypoint(_turnCircle.TangentialPointA), RoutePointTypeEnum.FLY_OVER));
                _legState = RfState.TRACK_TO_RF;
            } else
            {
                _legState = RfState.IN_RF;
            }

            if (!EndPoint.Point.PointPosition.Equals(_turnCircle.TangentialPointB))
            {
                _trackFromRFLeg = new TrackToFixLeg(new FmsPoint(new RouteWaypoint(_turnCircle.TangentialPointB), RoutePointTypeEnum.FLY_OVER), EndPoint);
            }
        }

        public void CalculateTurnCircle()
        {
            double turnAmount = GeoUtil.CalculateTurnAmount(InitialTrueCourse, FinalTrueCourse);

            GeoPoint bisectorIntersection;

            if (Math.Abs(turnAmount) < 3 || Math.Abs(turnAmount) > 177) // TODO: Figure out the margin of error. Probably less than 3
            {
                GeoPoint turnCircleCenter;
                double turnCircleRadiusM;
                
                // Calculate tangential circle to parallel legs:
                GeoUtil.CalculateCrossTrackErrorM(StartPoint.Point.PointPosition, EndPoint.Point.PointPosition, FinalTrueCourse, out _, out double startToEndAlongTrackDistance);
                if (startToEndAlongTrackDistance > 0) 
                {
                    // if we turn at StartPoint, we'll have some distance to go to EndPoint
                    // thus, we'll turn at StartPoint

                    double diameterCourse = GeoUtil.NormalizeHeading(InitialTrueCourse + 90);
                    GeoPoint tangentialPointA = StartPoint.Point.PointPosition;
                    GeoPoint tangentialPointB = GeoPoint.FindClosestIntersection(StartPoint.Point.PointPosition, diameterCourse, EndPoint.Point.PointPosition, FinalTrueCourse);

                    turnCircleRadiusM = GeoPoint.DistanceM(tangentialPointA, tangentialPointB) / 2;

                    // diameterCourse is the correct direction but not necessarily going the correct way
                    // we'll recalculate it now that we know both tangential points, so it goes the right way
                    diameterCourse = GeoPoint.InitialBearing(tangentialPointA, tangentialPointB);

                    turnCircleCenter = new GeoPoint(tangentialPointA);
                    turnCircleCenter.MoveByM(diameterCourse, turnCircleRadiusM);
                } else
                {
                    // if we turn at StartPoint, we'll be past EndPoint.
                    // we'll turn when we're abeam EndPoint

                    double diameterCourse = GeoUtil.NormalizeHeading(FinalTrueCourse + 90);
                    GeoPoint tangentialPointA = GeoPoint.FindClosestIntersection(EndPoint.Point.PointPosition, diameterCourse, StartPoint.Point.PointPosition, InitialTrueCourse);
                    GeoPoint tangentialPointB = EndPoint.Point.PointPosition;

                    turnCircleRadiusM = GeoPoint.DistanceM(tangentialPointB, tangentialPointA) / 2;

                    // diameterCourse is the correct direction but not necessarily going the correct way
                    // we'll recalculate it now that we know both tangential points, so it goes the right way
                    diameterCourse = GeoPoint.InitialBearing(tangentialPointB, tangentialPointA);

                    turnCircleCenter = new GeoPoint(tangentialPointB);
                    turnCircleCenter.MoveByM(diameterCourse, turnCircleRadiusM);
                }
                _turnCircle = new TurnCircle(turnCircleCenter, StartPoint.Point.PointPosition, EndPoint.Point.PointPosition, turnCircleRadiusM);
            } else
            {
                // Calculate tangential circle to crossing legs:

                // Calculate bisector of both legs
                bisectorIntersection = GeoPoint.FindClosestIntersection(StartPoint.Point.PointPosition, InitialTrueCourse, EndPoint.Point.PointPosition, FinalTrueCourse);

                // Calculate the courses again because great circle
                double bisectorStartRadial = GeoPoint.InitialBearing(bisectorIntersection, StartPoint.Point.PointPosition);
                double bisectorEndRadial = GeoPoint.InitialBearing(bisectorIntersection, EndPoint.Point.PointPosition);

                // Handle cases where we turn > 180 degrees. In these cases the bisectorRadials are not reciprocals of the courses for either start or end
                if (Math.Abs(bisectorStartRadial - InitialTrueCourse) < 2 && Math.Abs(bisectorEndRadial - FinalTrueCourse) < 2)
                {
                    bisectorEndRadial = GeoUtil.NormalizeHeading(bisectorEndRadial + 180);
                }

                double bisectorRadial = GeoUtil.NormalizeHeading(bisectorStartRadial + (GeoUtil.CalculateTurnAmount(bisectorStartRadial, bisectorEndRadial) / 2));
                // The bisector is now defined by bisectorIntersection and bisectorCourse

                // Figure out which of the two posible turn circles we want:
                GeoUtil.CalculateCrossTrackErrorM(bisectorIntersection, StartPoint.Point.PointPosition, InitialTrueCourse, out _, out double intersectionToStartPointAlongTrackDistance);

                // either A or B, whichever point we'll be crossing *while in the turn*
                GeoPoint referenceTangentPoint;

                // the two tangent points of the circle. One of them is A or B, whichever one referenceTangentPoint is, but we don't care.
                GeoPoint tangentPointA;
                GeoPoint tangentPointB;

                // the course for the line perpendicular to the leg of the referenceTangentPoint that goes through referenceTangentPoint
                double referenceTangentPointPerpendicularCourse;

                // The center of the circle is the intersection between the bisector and the perpendicular line
                GeoPoint turnCircleCenter;

                if (intersectionToStartPointAlongTrackDistance < 0)
                {
                    // This means we're heading into the intersection.
                    // The reference tangent point should be the closest one to the intersection
                    if (GeoPoint.DistanceM(StartPoint.Point.PointPosition, bisectorIntersection) < GeoPoint.DistanceM(EndPoint.Point.PointPosition, bisectorIntersection)) {
                        referenceTangentPointPerpendicularCourse = GeoUtil.NormalizeHeading(InitialTrueCourse + 90); // perpendicular to StartPoint
                        referenceTangentPoint = StartPoint.Point.PointPosition;
                        tangentPointA = StartPoint.Point.PointPosition;

                        turnCircleCenter = GeoPoint.FindClosestIntersection(bisectorIntersection, bisectorRadial, referenceTangentPoint, referenceTangentPointPerpendicularCourse);

                        // Calculate the other tangent point
                        double tangentPointBPerpendicularCourse = GeoUtil.NormalizeHeading(FinalTrueCourse + 90);
                        tangentPointB = GeoPoint.FindClosestIntersection(EndPoint.Point.PointPosition, GeoUtil.NormalizeHeading(FinalTrueCourse + 180), turnCircleCenter, tangentPointBPerpendicularCourse);
                    } else
                    {
                        referenceTangentPointPerpendicularCourse = GeoUtil.NormalizeHeading(FinalTrueCourse + 90); // perpendicular to EndPoint
                        referenceTangentPoint = EndPoint.Point.PointPosition;
                        tangentPointB = EndPoint.Point.PointPosition;

                        turnCircleCenter = GeoPoint.FindClosestIntersection(bisectorIntersection, bisectorRadial, referenceTangentPoint, referenceTangentPointPerpendicularCourse);
                        // Calculate the other tangent point
                        double tangentPointAPerpendicularCourse = GeoUtil.NormalizeHeading(InitialTrueCourse + 90);
                        tangentPointA = GeoPoint.FindClosestIntersection(StartPoint.Point.PointPosition, InitialTrueCourse, turnCircleCenter, tangentPointAPerpendicularCourse);
                    }
                }
                else
                {
                    // This means we're heading away from the intersection.
                    // The reference tangent point should be the furthest one to the intersection
                    if (GeoPoint.DistanceM(StartPoint.Point.PointPosition, bisectorIntersection) > GeoPoint.DistanceM(EndPoint.Point.PointPosition, bisectorIntersection)) {
                        referenceTangentPointPerpendicularCourse = GeoUtil.NormalizeHeading(InitialTrueCourse + 90); // perpendicular to StartPoint
                        referenceTangentPoint = StartPoint.Point.PointPosition;
                        tangentPointA = StartPoint.Point.PointPosition;

                        turnCircleCenter = GeoPoint.FindClosestIntersection(bisectorIntersection, bisectorRadial, referenceTangentPoint, referenceTangentPointPerpendicularCourse);
                        // Calculate the other tangent point
                        double tangentPointBPerpendicularCourse = GeoUtil.NormalizeHeading(FinalTrueCourse + 90);
                        tangentPointB = GeoPoint.FindClosestIntersection(EndPoint.Point.PointPosition, GeoUtil.NormalizeHeading(FinalTrueCourse + 180), turnCircleCenter, tangentPointBPerpendicularCourse);
                    }
                    else
                    {
                        referenceTangentPointPerpendicularCourse = GeoUtil.NormalizeHeading(FinalTrueCourse + 90); // perpendicular to EndPoint
                        referenceTangentPoint = EndPoint.Point.PointPosition;
                        tangentPointB = EndPoint.Point.PointPosition;

                        turnCircleCenter = GeoPoint.FindClosestIntersection(bisectorIntersection, bisectorRadial, referenceTangentPoint, referenceTangentPointPerpendicularCourse);

                        // Calculate the other tangent point
                        double tangentPointAPerpendicularCourse = GeoUtil.NormalizeHeading(InitialTrueCourse + 90);
                        tangentPointA = GeoPoint.FindClosestIntersection(StartPoint.Point.PointPosition, InitialTrueCourse, turnCircleCenter, tangentPointAPerpendicularCourse);
                    }
                }

                double turnCircleRadius = GeoPoint.DistanceM(referenceTangentPoint, turnCircleCenter);

                _turnCircle = new TurnCircle(turnCircleCenter, tangentPointA, tangentPointB, turnCircleRadius);
                _turnCircle.BisectorIntersection = bisectorIntersection;
            }
        }

        public bool HasLegTerminated(SimAircraft aircraft)
        {
            (_, _, double alongTrackDistance, _) = GetCourseInterceptInfo(aircraft);
            return alongTrackDistance < 0;
        }

        private bool isClockwise()
        {
            // Calculate the shortest turn from the initial leg to a leg inbound the turnCircle Center
            double turnAmount = GeoUtil.CalculateTurnAmount(InitialTrueCourse, GeoPoint.InitialBearing(_turnCircle.TangentialPointA, _turnCircle.Center));
            // If the turnAmount is greater than 0, it is a right-hand turn, thus, a clockwise turn
            return turnAmount > 0;
        }

        public void ProcessLeg(SimAircraft aircraft, int intervalMs)
        {
            switch (_legState)
            {
                case RfState.TRACK_TO_RF:
                    HandleTrackToRF(aircraft, intervalMs);
                    break;
                case RfState.IN_RF:
                    HandleRFTurn(aircraft, intervalMs);
                    break;
                case RfState.TRACK_FROM_RF:
                    break;
            }
        }

        public void HandleRFTurn(SimAircraft aircraft, int intervalMs)
        {
            GeoUtil.CalculateArcCourseInfo(aircraft.Position.PositionGeoPoint, _turnCircle.Center, _turnCircle.PointARadial, _turnCircle.PointBRadial, _turnCircle.RadiusM, isClockwise(), out double requiredTrueCourse, out double alongTrackDistanceM);

            if (alongTrackDistanceM < 0 && _trackFromRFLeg != null)
            {
                _legState = RfState.TRACK_FROM_RF;
            }
        }

        public void HandleTrackToRF(SimAircraft aircraft, int intervalMs)
        {
            if (_trackToRFLeg.HasLegTerminated(aircraft))
            {
                _legState = RfState.IN_RF;
                HandleRFTurn(aircraft, intervalMs);
            }
        }

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance, double turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            // Depending on how far along the leg we are, we'll add the along track distances of the relevant internal legs.

            double alongTrackDistance = 0;
            double crossTrackError = 0;
            double requiredTrueCourse = 0;
            double turnRadius = -1;

            switch (_legState)
            {
                case RfState.TRACK_TO_RF:

                    double toRFAlongTrackDistance;

                    (requiredTrueCourse, crossTrackError, toRFAlongTrackDistance, _) = _trackToRFLeg.GetCourseInterceptInfo(aircraft);
                    GeoUtil.CalculateArcCourseInfo(_turnCircle.TangentialPointA, _turnCircle.Center, InitialTrueCourse, FinalTrueCourse, _turnCircle.RadiusM, isClockwise(), out _, out double rfTurnLength);

                    double fromRFAlongTrackDistance = GeoPoint.DistanceM(_turnCircle.TangentialPointB, EndPoint.Point.PointPosition);

                    alongTrackDistance = toRFAlongTrackDistance + rfTurnLength + fromRFAlongTrackDistance;

                    break;
                case RfState.IN_RF:
                    crossTrackError = GeoUtil.CalculateArcCourseInfo(aircraft.Position.PositionGeoPoint, _turnCircle.Center, _turnCircle.PointARadial, _turnCircle.PointBRadial, _turnCircle.RadiusM, isClockwise(), out requiredTrueCourse, out rfTurnLength);
                    turnRadius = _turnCircle.RadiusM * (isClockwise() ? 1 : -1);

                    if (_trackFromRFLeg != null)
                    {
                        fromRFAlongTrackDistance = GeoPoint.DistanceM(_turnCircle.TangentialPointB, EndPoint.Point.PointPosition);
                    } else
                    {
                        fromRFAlongTrackDistance = 0;
                    }
                    
                    alongTrackDistance = rfTurnLength + fromRFAlongTrackDistance;

                    break;
                case RfState.TRACK_FROM_RF:
                    if(_trackFromRFLeg != null)
                    {
                        (requiredTrueCourse, crossTrackError, fromRFAlongTrackDistance, _) = _trackFromRFLeg.GetCourseInterceptInfo(aircraft);
                        alongTrackDistance = fromRFAlongTrackDistance;
                    } else
                    {
                        goto case RfState.IN_RF;
                    }
                    
                    break;
            }

            return (requiredTrueCourse, crossTrackError, alongTrackDistance, turnRadius);
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            return GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, StartPoint.Point.PointPosition, InitialTrueCourse, out _, out _) < 0;
        }

        public override string ToString()
        {
            return $"({StartPoint}) (RF) => ({EndPoint}) TurnCircle: {_turnCircle}\n" +
                $"Legs: \n" +
                $"S->Ta: ({_trackToRFLeg})\n" +
                $"Tb->E: ({_trackFromRFLeg})";
        }

        public List<NdLine> UiLines
        {
            get
            {
                var retList = new List<NdLine>();
                if (!StartPoint.Point.PointPosition.Equals(_turnCircle.TangentialPointA))
                {
                    retList.Add(new NdLine(StartPoint.Point.PointPosition, _turnCircle.TangentialPointA));
                }

                retList.Add(new NdArc(_turnCircle.TangentialPointA, _turnCircle.TangentialPointB, _turnCircle.Center, _turnCircle.RadiusM, _turnCircle.PointARadial, _turnCircle.PointBRadial, isClockwise()));
                if (!EndPoint.Point.PointPosition.Equals(_turnCircle.TangentialPointA))
                {
                    retList.Add(new NdLine(_turnCircle.TangentialPointB, EndPoint.Point.PointPosition));
                }

                return retList;
            }
        }
    }
}
