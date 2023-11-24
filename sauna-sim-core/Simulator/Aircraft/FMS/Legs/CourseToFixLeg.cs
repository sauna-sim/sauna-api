using System;
using System.Collections.Generic;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.GeoTools.MagneticTools;
using AviationCalcUtilNet.MathTools;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class CourseToFixLeg : IRouteLeg
    {
        private FmsPoint _endPoint;
        private double _magneticCourse;
        private double _trueCourse;

        public CourseToFixLeg(FmsPoint endPoint, BearingTypeEnum courseType, double course)
        {
            _endPoint = endPoint;
            if (courseType == BearingTypeEnum.TRUE)
            {
                _trueCourse = course;
                _magneticCourse = MagneticUtil.ConvertTrueToMagneticTile(_trueCourse, endPoint.Point.PointPosition);
            }
            else
            {
                _magneticCourse = course;
                _trueCourse = MagneticUtil.ConvertMagneticToTrueTile(_magneticCourse, endPoint.Point.PointPosition);
            }
        }

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.COURSE_TO_FIX;
        
        public bool HasLegTerminated(SimAircraft aircraft)
        {
            // Leg terminates when aircraft passes abeam/over terminating point
            GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, _trueCourse,
                out _, out double alongTrackDistance);

            return alongTrackDistance <= 0;
        }

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance, double turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            // Otherwise calculate cross track error for this leg
            double crossTrackError = GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, _trueCourse,
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

            double turnLeadDist = GeoUtil.CalculateTurnLeadDistance(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, aircraft.Position.Track_True, aircraft.Position.TrueAirSpeed, _trueCourse, aircraft.Position.WindDirection, aircraft.Position.WindSpeed, out _, out GeoPoint intersection);

            turnLeadDist *= 1.2;

            return (GeoPoint.FlatDistanceM(aircraft.Position.PositionGeoPoint, intersection) < MathUtil.ConvertNauticalMilesToMeters(turnLeadDist));
            //double requestedTurnDelta = GeoUtil.CalculateTurnAmount(demandedTrack, aircraft.Position.Track_True);
            //return (trackDelta > 0 && requestedTurnDelta > 0 || trackDelta < 0 && requestedTurnDelta < 0);
        }

        public FmsPoint StartPoint => null;

        public FmsPoint EndPoint => _endPoint;

        public double InitialTrueCourse => -1;

        public double FinalTrueCourse => _trueCourse;

        public double LegLength => 0;

        public override string ToString()
        {
            return $"{_magneticCourse:000.0} =(CF)=> {_endPoint}";
        }

        public void ProcessLeg(SimAircraft aircraft, int intervalMs)
        {
        }

        public void InitializeLeg(SimAircraft aircraft)
        {
        }

        public List<NdLine> UiLines => new List<NdLine>();
    }
}
