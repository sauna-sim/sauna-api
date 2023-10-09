using AviationCalcUtilNet.GeoTools;
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
                // Calculate tangential circle to parallel legs
            } else
            {
                // Calculate tangential circle to crossing legs
                GeoPoint bisectorIntersection = GeoUtil.FindIntersection(StartPoint.Point.PointPosition, EndPoint.Point.PointPosition, InitialTrueCourse, FinalTrueCourse);
                
                double firstLegPerperndicularCourse = GeoUtil.NormalizeHeading(InitialTrueCourse + 90);
                double secondLegPerpendicularCourse = GeoUtil.NormalizeHeading(FinalTrueCourse + 90);

                double bisectorCourse = GeoUtil.NormalizeHeading((InitialTrueCourse + FinalTrueCourse) / 2);

                GeoPoint tangentPoint;

                

                if ()

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
            throw new NotImplementedException();
        }
    }

    
}
