using System;
using AviationCalcUtilNet.GeoTools;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class DirectToFixLeg : IRouteLeg
    {
        private FmsPoint _endPoint;
        private double _trueCourse;
        private double _prevAlongTrackDist;

        public DirectToFixLeg(FmsPoint point)
        {
            _endPoint = point;
            _trueCourse = -1;
        }

        public FmsPoint StartPoint => null;

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

        public (double requiredTrueCourse, double crossTrackError) UpdateForLnav(SimAircraft aircraft, int intervalMs)
        {
            // Check if we should start turning towards the next leg
            IRouteLeg nextLeg = aircraft.Fms.GetFirstLeg();
            
            if (nextLeg != null && !aircraft.Fms.Suspended)
            {
                if (HasLegTerminated(aircraft))
                {
                    // Activate next leg on termination
                    aircraft.Fms.ActivateNextLeg();
                }
                else if (_endPoint.PointType == RoutePointTypeEnum.FLY_BY &&
                         nextLeg.ShouldActivateLeg(aircraft, intervalMs) &&
                         nextLeg.InitialTrueCourse >= 0 &&
                         Math.Abs(FinalTrueCourse - nextLeg.InitialTrueCourse) > 0.5)
                {
                    // Begin turn to next leg, but do not activate
                    (double nextRequiredTrueCourse, double nextCrossTrackError, _) = nextLeg.GetCourseInterceptInfo(aircraft);

                    return (nextRequiredTrueCourse, nextCrossTrackError);
                }
            }
            
            // Update CrossTrackError, etc
            (double requiredTrueCourse, double crossTrackError, _) = GetCourseInterceptInfo(aircraft);

            return (requiredTrueCourse, crossTrackError);
        }

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance) GetCourseInterceptInfo(SimAircraft aircraft)
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
            }
            
            // Otherwise calculate cross track error for this leg
            double crossTrackError = GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, _trueCourse,
                out double requiredTrueCourse, out double alongTrackDistance);
            
            if (alongTrackDistance <= AutopilotUtil.MIN_XTK_M && AutopilotUtil.MIN_XTK_M <= _prevAlongTrackDist)
            {
                aircraft.Fms.WaypointPassed?.Invoke(this, new WaypointPassedEventArgs(_endPoint.Point));
            }

            _prevAlongTrackDist = alongTrackDistance;

            return (requiredTrueCourse, crossTrackError, alongTrackDistance);
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            (double requiredTrueCourse, double crossTrackError, _) = GetCourseInterceptInfo(aircraft);

            // If there's no error
            double trackDelta = GeoUtil.CalculateTurnAmount(requiredTrueCourse, aircraft.Position.Track_True);
            if (Math.Abs(trackDelta) < double.Epsilon)
            {
                return false;
            }
            
            // Find cross track error to start turn (distance from intersection)
            double demandedTrack = AutopilotUtil.CalculateDemandedTrackOnCurrentTrack(crossTrackError, aircraft.Position.Track_True, requiredTrueCourse, aircraft.Position.Bank,
                aircraft.Position.GroundSpeed, intervalMs).demandedTrack;

            double requestedTurnDelta = GeoUtil.CalculateTurnAmount(demandedTrack, aircraft.Position.Track_True);
            return (trackDelta > 0 && requestedTurnDelta > 0 || trackDelta < 0 && requestedTurnDelta < 0);
        }

        public override string ToString()
        {
            return $"PPOS =(DF)=> {_endPoint}";
        }
    }
}
