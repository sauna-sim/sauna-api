using System;
using System.Collections.Generic;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.MathTools;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class TrackToFixLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private FmsPoint _endPoint;
        private double _initialBearing;
        private double _finalBearing;
        private double _legLength;

        public TrackToFixLeg(FmsPoint startPoint, FmsPoint endPoint)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _initialBearing = GeoPoint.InitialBearing(_startPoint.Point.PointPosition, _endPoint.Point.PointPosition);
            _finalBearing = GeoPoint.FinalBearing(_startPoint.Point.PointPosition, _endPoint.Point.PointPosition);
            _legLength = GeoPoint.DistanceM(_startPoint.Point.PointPosition, _endPoint.Point.PointPosition);
        }

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.TRACK_TO_FIX;

        public double InitialTrueCourse => _initialBearing;

        public double FinalTrueCourse => _finalBearing;

        public FmsPoint EndPoint => _endPoint;

        public FmsPoint StartPoint => _startPoint;

        public double LegLength => _legLength;

        public bool HasLegTerminated(SimAircraft aircraft)
        {
            // Leg terminates when aircraft passes abeam/over terminating point
            GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, _finalBearing,
                out _, out double alongTrackDistance);

            return alongTrackDistance <= 0;
        }

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance, double turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            // Otherwise calculate cross track error for this leg
            double crossTrackError = GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, _finalBearing,
                out double requiredTrueCourse, out double alongTrackDistance);

            return (requiredTrueCourse, crossTrackError, alongTrackDistance, 0);
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            (double requiredTrueCourse, double crossTrackError, _, _) = GetCourseInterceptInfo(aircraft);

            // If there's no error
            double trackDelta = GeoUtil.CalculateTurnAmount(requiredTrueCourse, aircraft.Position.Track_True);
            if (Math.Abs(trackDelta) < double.Epsilon)
            {
                return false;
            }

            // Find cross track error to start turn (distance from intersection)
            //double demandedTrack = AutopilotUtil.CalculateDemandedTrackOnCurrentTrack(crossTrackError, aircraft.Position.Track_True, requiredTrueCourse, aircraft.Position.Bank,
            //aircraft.Position.GroundSpeed, intervalMs).demandedTrack;

            double turnLeadDist = GeoUtil.CalculateTurnLeadDistance(aircraft.Position.PositionGeoPoint, _startPoint.Point.PointPosition, aircraft.Position.Track_True, aircraft.Position.TrueAirSpeed, _initialBearing, aircraft.Position.WindDirection, aircraft.Position.WindSpeed, out _, out GeoPoint intersection);

            turnLeadDist *= 1.2;

            return (GeoPoint.FlatDistanceM(aircraft.Position.PositionGeoPoint, intersection) < MathUtil.ConvertNauticalMilesToMeters(turnLeadDist));
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
