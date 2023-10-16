using AviationCalcUtilNet.GeoTools;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
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

        private class TurnCircle
        {
            public GeoPoint Center { get; set; }

            public GeoPoint TangentialPointA { get; set; }
            public double PointARadial => GeoPoint.InitialBearing(Center, TangentialPointA);

            public GeoPoint TangentialPointB { get; set; }
            public double PointBRadial => GeoPoint.InitialBearing(Center, TangentialPointB);

            public double RadiusM { get; set; }

            public override string ToString()
            {
                return $"Center: ({Center.Lat}, {Center.Lon}) Radius (nm): {RadiusM}";
            }

            public TurnCircle(GeoPoint center, GeoPoint tangentialPointA, GeoPoint tangentialPointB, double radiusM)
            {
                Center = center;
                TangentialPointA = tangentialPointA;
                TangentialPointB = tangentialPointB;
                RadiusM = radiusM;
            }
        }

        private enum RfState
        {
            TRACK_TO_RF,
            IN_RF,
            TRACK_FROM_RF
        }

        private RfState _legState;

        public RadiusToFixLeg(FmsPoint startPoint, FmsPoint endPoint, double initialTrueCourse, double finalTrueCourse)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _initialTrueCourse = initialTrueCourse;
            _finalTrueCourse = finalTrueCourse;

            CalculateTurnCircle();
        }

        public void CalculateTurnCircle()
        {
            if (Math.Abs(InitialTrueCourse - FinalTrueCourse) < 5) // TODO: Figure out the margin of error. Probably less than 5.
            {
                // Calculate tangential circle to parallel legs:
                double diameterLineCourse = GeoPoint.InitialBearing(StartPoint.Point.PointPosition, EndPoint.Point.PointPosition);
                double turnCircleRadiusM = GeoPoint.DistanceM(StartPoint.Point.PointPosition, EndPoint.Point.PointPosition);

                GeoPoint turnCircleCenter = new GeoPoint(StartPoint.Point.PointPosition);
                turnCircleCenter.MoveByM(diameterLineCourse, turnCircleRadiusM);

                _turnCircle = new TurnCircle(turnCircleCenter, StartPoint.Point.PointPosition, EndPoint.Point.PointPosition, turnCircleRadiusM);
            } else
            {
                // Calculate tangential circle to crossing legs:

                // Calculate bisector of both legs
                GeoPoint bisectorIntersection = GeoUtil.FindIntersection(StartPoint.Point.PointPosition, EndPoint.Point.PointPosition, InitialTrueCourse, FinalTrueCourse);

                // Calculate the courses again because great circle
                double bisectorStartRadial = GeoPoint.InitialBearing(bisectorIntersection, StartPoint.Point.PointPosition);
                double bisectorEndRadial = GeoPoint.InitialBearing(bisectorIntersection, EndPoint.Point.PointPosition);

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

                        turnCircleCenter = GeoUtil.FindIntersection(referenceTangentPoint, bisectorIntersection, referenceTangentPointPerpendicularCourse, bisectorRadial);

                        // Calculate the other tangent point
                        double tangentPointBPerpendicularCourse = GeoUtil.NormalizeHeading(FinalTrueCourse + 90);
                        tangentPointB = GeoUtil.FindIntersection(EndPoint.Point.PointPosition, turnCircleCenter, FinalTrueCourse, tangentPointBPerpendicularCourse);
                    } else
                    {
                        referenceTangentPointPerpendicularCourse = GeoUtil.NormalizeHeading(FinalTrueCourse + 90); // perpendicular to EndPoint
                        referenceTangentPoint = EndPoint.Point.PointPosition;
                        tangentPointB = EndPoint.Point.PointPosition;

                        turnCircleCenter = GeoUtil.FindIntersection(referenceTangentPoint, bisectorIntersection, referenceTangentPointPerpendicularCourse, bisectorRadial);

                        // Calculate the other tangent point
                        double tangentPointAPerpendicularCourse = GeoUtil.NormalizeHeading(InitialTrueCourse + 90);
                        tangentPointA = GeoUtil.FindIntersection(StartPoint.Point.PointPosition, turnCircleCenter, InitialTrueCourse, tangentPointAPerpendicularCourse);
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

                        turnCircleCenter = GeoUtil.FindIntersection(referenceTangentPoint, bisectorIntersection, referenceTangentPointPerpendicularCourse, bisectorRadial);

                        // Calculate the other tangent point
                        double tangentPointBPerpendicularCourse = GeoUtil.NormalizeHeading(FinalTrueCourse + 90);
                        tangentPointB = GeoUtil.FindIntersection(EndPoint.Point.PointPosition, turnCircleCenter, FinalTrueCourse, tangentPointBPerpendicularCourse);
                    }
                    else
                    {
                        referenceTangentPointPerpendicularCourse = GeoUtil.NormalizeHeading(FinalTrueCourse + 90); // perpendicular to EndPoint
                        referenceTangentPoint = EndPoint.Point.PointPosition;
                        tangentPointB = EndPoint.Point.PointPosition;

                        turnCircleCenter = GeoUtil.FindIntersection(referenceTangentPoint, bisectorIntersection, referenceTangentPointPerpendicularCourse, bisectorRadial);

                        // Calculate the other tangent point
                        double tangentPointAPerpendicularCourse = GeoUtil.NormalizeHeading(InitialTrueCourse + 90);
                        tangentPointA = GeoUtil.FindIntersection(StartPoint.Point.PointPosition, turnCircleCenter, InitialTrueCourse, tangentPointAPerpendicularCourse);
                    }
                }

                double turnCircleRadius = GeoPoint.DistanceM(referenceTangentPoint, turnCircleCenter);

                _turnCircle = new TurnCircle(turnCircleCenter, tangentPointA, tangentPointB, turnCircleRadius);
            }
        }

        public bool HasLegTerminated(SimAircraft aircraft)
        {
            return false; // FOR DEAR GOD CHANGE THIS
        }

        private bool isClockwise()
        {
            // Calculate the shortest turn from the initial leg to a leg inbound the turnCircle Center
            double turnAmount = GeoUtil.CalculateTurnAmount(InitialTrueCourse, GeoPoint.InitialBearing(_turnCircle.TangentialPointA, _turnCircle.Center));
            // If the turnAmount is greater than 0, it is a right-hand turn, thus, a clockwise turn
            return turnAmount > 0;
        }

        public (double requiredTrueCourse, double crossTrackError) UpdateForLnav(SimAircraft aircraft, int intervalMs)
        {
            // TODO: Add states (a->Ta, Tb->b). For now this should work for holds though.
            
            double requiredTrueCourse;
            double crossTrackError = GeoUtil.CalculateArcCourseInfo(aircraft.Position.PositionGeoPoint, _turnCircle.Center, _turnCircle.PointARadial, _turnCircle.PointBRadial, _turnCircle.RadiusM, isClockwise(), out requiredTrueCourse, out _);

            return (requiredTrueCourse, crossTrackError);
        }

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            // TODO: Add states (a->Ta, Tb->b). For now this should work for holds though.

            double requiredTrueCourse;
            double alongTrackDistance;
            double crossTrackError = GeoUtil.CalculateArcCourseInfo(aircraft.Position.PositionGeoPoint, _turnCircle.Center, _turnCircle.PointARadial, _turnCircle.PointBRadial, _turnCircle.RadiusM, isClockwise(), out requiredTrueCourse, out alongTrackDistance);

            return (requiredTrueCourse, crossTrackError, alongTrackDistance);
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            return GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, StartPoint.Point.PointPosition, InitialTrueCourse, out _, out _) < 0;
        }

        public override string ToString()
        {
            return $"({StartPoint}) (RF) => ({EndPoint}) TurnCircle: {_turnCircle}";
        }
    }
}
