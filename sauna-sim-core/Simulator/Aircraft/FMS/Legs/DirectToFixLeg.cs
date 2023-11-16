using System;
using System.Collections.Generic;
using AviationCalcUtilNet.GeoTools;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class DirectToFixLeg : IRouteLeg
    {
        private FmsPoint _endPoint;
        private FmsPoint _startPoint;
        private double _trueCourse;

        public DirectToFixLeg(FmsPoint point)
        {
            _endPoint = point;
            _trueCourse = -1;
        }

        public FmsPoint StartPoint => _startPoint;

        public FmsPoint EndPoint => _endPoint;

        public double InitialTrueCourse => -1;

        public double FinalTrueCourse => _trueCourse;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.DIRECT_TO_FIX;
        
        public bool HasLegTerminated(SimAircraft aircraft)
        {
            // Leg terminates when aircraft passes abeam/over terminating point
            GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, _trueCourse,
                out _, out double alongTrackDistance);

            return alongTrackDistance <= 0;
        }

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance, double turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            // Check if track has been set
            if (_trueCourse < 0)
            {
                ProcessLeg(aircraft, 1000);
            }
            
            // Otherwise calculate cross track error for this leg
            double crossTrackError = GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, _trueCourse,
                out double requiredTrueCourse, out double alongTrackDistance);

            return (requiredTrueCourse, crossTrackError, alongTrackDistance, -1);
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
            // Check if track has been set
            if (_trueCourse < 0)
            {
                _trueCourse = GeoUtil.CalculateDirectBearingAfterTurn(
                    aircraft.Position.PositionGeoPoint,
                    _endPoint.Point.PointPosition,
                    GeoUtil.CalculateRadiusOfTurn(GeoUtil.CalculateMaxBankAngle(aircraft.Position.GroundSpeed, 25, 3), aircraft.Position.GroundSpeed),
                    aircraft.Position.Track_True);

                if (_trueCourse < 0)
                {
                    _trueCourse = GeoPoint.FinalBearing(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition);
                }

                // Set start point
                _startPoint = new FmsPoint(new RouteWaypoint("*PPOS", aircraft.Position.PositionGeoPoint), RoutePointTypeEnum.FLY_OVER);
            }
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
