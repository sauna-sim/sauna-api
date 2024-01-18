using System;
using System.Collections.Generic;
using AviationCalcUtilNet.Aviation;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Units;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class DirectToFixLeg : IRouteLeg
    {
        private FmsPoint _endPoint;
        private FmsPoint _startPoint;
        private Bearing _trueCourse;
        private Bearing _initTrueCourse;
        private Length _legLength;

        public DirectToFixLeg(FmsPoint point, GeoPoint ppos, Bearing curTrueTrack, Velocity gs)
        {
            _endPoint = point;
            _trueCourse = AviationUtil.CalculateDirectBearingAfterTurn(
                    ppos,
                    _endPoint.Point.PointPosition,
                    AviationUtil.CalculateRadiusOfTurn(gs, AviationUtil.CalculateMaxBankAngle(gs, Angle.FromDegrees(25), AngularVelocity.FromDegreesPerSecond(3))),
                    curTrueTrack);

            if (_trueCourse == null)
            {
                _trueCourse = GeoPoint.FinalBearing(ppos, _endPoint.Point.PointPosition);
            }

            // Set start point
            GeoPoint startPt = (GeoPoint)_endPoint.Point.PointPosition.Clone();
            startPt.MoveBy(_trueCourse + Angle.FromDegrees(180), GeoPoint.FlatDistance(_endPoint.Point.PointPosition, ppos));
            _startPoint = new FmsPoint(new RouteWaypoint("*DIRECT", startPt), RoutePointTypeEnum.FLY_OVER);

            _initTrueCourse = GeoPoint.InitialBearing(_startPoint.Point.PointPosition, _endPoint.Point.PointPosition);
            _legLength = GeoPoint.Distance(_startPoint.Point.PointPosition, _endPoint.Point.PointPosition);
        }

        public FmsPoint StartPoint => _startPoint;

        public FmsPoint EndPoint => _endPoint;

        public Bearing InitialTrueCourse => _initTrueCourse;

        public Bearing FinalTrueCourse => _trueCourse;

        public Length LegLength => _legLength;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.DIRECT_TO_FIX;
        
        public bool HasLegTerminated(SimAircraft aircraft)
        {
            // Leg terminates when aircraft passes abeam/over terminating point
            (_, Length alongTrackDistance, _) = AviationUtil.CalculateLinearCourseIntercept(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, _trueCourse);

            return alongTrackDistance <= (Length) 0;
        }

        public (Bearing requiredTrueCourse, Length crossTrackError, Length alongTrackDistance, Length turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {            
            // Otherwise calculate cross track error for this leg
            (Bearing requiredTrueCourse, Length alongTrackDistance, Length crossTrackError) = AviationUtil.CalculateLinearCourseIntercept(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, _trueCourse);

            return (requiredTrueCourse, crossTrackError, alongTrackDistance, (Length)0);
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            // Always activates
            return true;
        }

        public override string ToString()
        {
            return $"PPOS =(DF)=> {_endPoint}";
        }

        public void ProcessLeg(SimAircraft aircraft, int intervalMs)
        {
        }

        public void InitializeLeg(SimAircraft aircraft)
        {
        }

        public List<NdLine> UiLines
        {
            get
            {
                var lines = new List<NdLine>();
                if (StartPoint != null)
                {
                    lines.Add(new NdLine(StartPoint.Point.PointPosition, EndPoint.Point.PointPosition));
                }

                return lines;
            }
        }
    }
}
