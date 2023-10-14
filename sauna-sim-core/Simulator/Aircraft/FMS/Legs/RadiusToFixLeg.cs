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

        public FmsPoint StartPoint => throw new NotImplementedException();

        public FmsPoint EndPoint => throw new NotImplementedException();

        private double _initialTrueCourse;
        private double _finalTrueCourse;

        public double InitialTrueCourse => _initialTrueCourse;

        public double FinalTrueCourse => _finalTrueCourse;

        public RouteLegTypeEnum LegType => throw new NotImplementedException();

        private TurnCircle _turnCircle;

        private class TurnCircle
        {
            public GeoPoint Center { get; set; }
            public double RadiusNm { get; set; }
        }

        private enum RfState
        {
            TRACK_TO_RF,
            IN_RF,
            TRACK_FROM_RF
        }

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
            if (Math.Abs(InitialTrueCourse - FinalTrueCourse) < 5)
            {
                // Calculate tangential circle to parallel legs:
            } else
            {
                // Calculate tangential circle to crossing legs:

                // Calculate bisector of both legs
                GeoPoint bisectorIntersection = GeoUtil.FindIntersection(StartPoint.Point.PointPosition, EndPoint.Point.PointPosition, InitialTrueCourse, FinalTrueCourse);

                double firstLegCourse = GeoPoint.InitialBearing(bisectorIntersection, StartPoint.Point.PointPosition);
                double secondLegCourse = GeoPoint.InitialBearing(bisectorIntersection, EndPoint.Point.PointPosition);

                double bisectorCourse = firstLegCourse + (GeoUtil.CalculateTurnAmount(firstLegCourse, secondLegCourse) / 2);
                // The bisector is now defined by bisectorIntersection and bisectorCourse

                // Figure out which of the two posible turn circles we want:
                double intersectionToStartPointAlongTrackDistance = GeoUtil.CalculateCrossTrackErrorM(bisectorIntersection, StartPoint.Point.PointPosition, firstLegCourse, out _, out _);

                // either A or B, whichever point we'll be crossing *while in the turn*
                GeoPoint referenceTangentPoint;

                // the course for the line perpendicular to the leg of the referenceTangentPoint that goes through referenceTangentPoint
                double perpendicularCourse;

                if (intersectionToStartPointAlongTrackDistance < 0)
                {
                    // This means we're heading into the intersection.
                    // The reference tangent point should be the closest one to the intersection
                    if (GeoPoint.DistanceNMi(StartPoint.Point.PointPosition, bisectorIntersection) < GeoPoint.DistanceNMi(EndPoint.Point.PointPosition, bisectorIntersection) {
                        perpendicularCourse = GeoUtil.NormalizeHeading(InitialTrueCourse + 90); // perpendicular to StartPoint
                        referenceTangentPoint = StartPoint.Point.PointPosition;
                    } else
                    {
                        perpendicularCourse = GeoUtil.NormalizeHeading(FinalTrueCourse + 90); // perpendicular to EndPoint
                        referenceTangentPoint = EndPoint.Point.PointPosition;
                    }
                }
                else
                {
                    // This means we're heading away from the intersection.
                    // The reference tangent point should be the furthest one to the intersection
                    if (GeoPoint.DistanceNMi(StartPoint.Point.PointPosition, bisectorIntersection) > GeoPoint.DistanceNMi(EndPoint.Point.PointPosition, bisectorIntersection) {
                        perpendicularCourse = GeoUtil.NormalizeHeading(InitialTrueCourse + 90); // perpendicular to StartPoint
                        referenceTangentPoint = StartPoint.Point.PointPosition;
                    }
                    else
                    {
                        perpendicularCourse = GeoUtil.NormalizeHeading(FinalTrueCourse + 90); // perpendicular to EndPoint
                        referenceTangentPoint = EndPoint.Point.PointPosition;
                    }
                }

                // The center of the circle is the intersection between the bisector and the perpendicular line

                GeoPoint turnCircleCenter = GeoUtil.FindIntersection(referenceTangentPoint, bisectorIntersection, perpendicularCourse, bisectorCourse);

                double turnCircleRadius = GeoPoint.DistanceNMi(referenceTangentPoint, turnCircleCenter);

                _turnCircle = new TurnCircle() {
                    Center = turnCircleCenter,
                    RadiusNm = turnCircleRadius
                };
            }
        }

        public bool HasLegTerminated(SimAircraft aircraft)
        {
            throw new NotImplementedException();
        }

        public (double requiredTrueCourse, double crossTrackError) UpdateForLnav(SimAircraft aircraft, int intervalMs)
        {
            throw new NotImplementedException();
        }

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            throw new NotImplementedException();
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            return GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, StartPoint.Point.PointPosition, InitialTrueCourse, out _, out _) < 0;
        }
    }
}
