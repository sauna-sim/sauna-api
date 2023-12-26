using System;
using System.Collections.Generic;
using AviationCalcUtilNet.Aviation;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.MathTools;
using AviationCalcUtilNet.Units;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class TrackToFixLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private FmsPoint _endPoint;
        private Bearing _initialBearing;
        private Bearing _finalBearing;
        private Length _legLength;

        public TrackToFixLeg(FmsPoint startPoint, FmsPoint endPoint)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _initialBearing = GeoPoint.InitialBearing(_startPoint.Point.PointPosition, _endPoint.Point.PointPosition);
            _finalBearing = GeoPoint.FinalBearing(_startPoint.Point.PointPosition, _endPoint.Point.PointPosition);
            _legLength = GeoPoint.Distance(_startPoint.Point.PointPosition, _endPoint.Point.PointPosition);
        }

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.TRACK_TO_FIX;

        public Bearing InitialTrueCourse => _initialBearing;

        public Bearing FinalTrueCourse => _finalBearing;

        public FmsPoint EndPoint => _endPoint;

        public FmsPoint StartPoint => _startPoint;

        public Length LegLength => _legLength;

        public bool HasLegTerminated(SimAircraft aircraft)
        {
            // Leg terminates when aircraft passes abeam/over terminating point
            (_, Length alongTrackDistance, _) = AviationUtil.CalculateLinearCourseIntercept(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, _finalBearing);

            return alongTrackDistance.Meters <= 0;
        }

        public (Bearing requiredTrueCourse, Length crossTrackError, Length alongTrackDistance, Length turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            // Otherwise calculate cross track error for this leg
            (Bearing requiredTrueCourse, Length alongTrackDistance, Length crossTrackError) = AviationUtil.CalculateLinearCourseIntercept(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, _finalBearing);

            return (requiredTrueCourse, crossTrackError, alongTrackDistance, new Length(0));
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            (Bearing requiredTrueCourse, Length crossTrackError, _, _) = GetCourseInterceptInfo(aircraft);

            // If there's no error
            Angle trackDelta = aircraft.Position.Track_True - requiredTrueCourse;
            if (Math.Abs(trackDelta.Value()) < double.Epsilon)
            {
                return false;
            }

            // Find cross track error to start turn (distance from intersection)
            //double demandedTrack = AutopilotUtil.CalculateDemandedTrackOnCurrentTrack(crossTrackError, aircraft.Position.Track_True, requiredTrueCourse, aircraft.Position.Bank,
            //aircraft.Position.GroundSpeed, intervalMs).demandedTrack;

            (Length turnLeadDist, _, GeoPoint intersection) = AviationUtil.CalculateTurnLeadDistance(aircraft.Position.PositionGeoPoint, _startPoint.Point.PointPosition, aircraft.Position.Track_True, aircraft.Position.TrueAirSpeed, _initialBearing, aircraft.Position.WindDirection, aircraft.Position.WindSpeed, Angle.FromDegrees(25), AngularVelocity.FromDegreesPerSecond(3))
                .GetValueOrDefault(((Length)0, (Length)(-1), null));

            turnLeadDist *= 1.2;

            return (GeoPoint.FlatDistance(aircraft.Position.PositionGeoPoint, intersection) < turnLeadDist);
            //double requestedTurnDelta = GeoUtil.CalculateTurnAmount(demandedTrack, aircraft.Position.Track_True);
            //return (trackDelta > 0 && requestedTurnDelta > 0 || trackDelta < 0 && requestedTurnDelta < 0);

        }

        public override string ToString()
        {
            return $"{_startPoint} =(TF)=> {_endPoint}";
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
                var retList = new List<NdLine>();
                retList.Add(new NdLine(StartPoint.Point.PointPosition, EndPoint.Point.PointPosition));
                return retList;
            }
        }
    }
}
