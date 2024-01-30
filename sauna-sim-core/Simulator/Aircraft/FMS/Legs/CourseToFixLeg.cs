using System;
using System.Collections.Generic;
using AviationCalcUtilNet.Aviation;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Magnetic;
using AviationCalcUtilNet.Units;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class CourseToFixLeg : IRouteLeg
    {
        private FmsPoint _endPoint;
        private Bearing _magneticCourse;
        private Bearing _trueCourse;

        public CourseToFixLeg(FmsPoint endPoint, BearingTypeEnum courseType, Bearing course, MagneticTileManager magTileMgr)
        {
            _endPoint = endPoint;
            if (courseType == BearingTypeEnum.TRUE)
            {
                _trueCourse = course;
                _magneticCourse = magTileMgr.TrueToMagnetic(endPoint.Point.PointPosition, DateTime.UtcNow, _trueCourse);
            }
            else
            {
                _magneticCourse = course;
                _trueCourse = magTileMgr.MagneticToTrue(endPoint.Point.PointPosition, DateTime.UtcNow, _magneticCourse);
            }
        }

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.COURSE_TO_FIX;
        
        public bool HasLegTerminated(SimAircraft aircraft)
        {
            // Leg terminates when aircraft passes abeam/over terminating point
            (_, Length alongTrackDistance, _) = AviationUtil.CalculateLinearCourseIntercept(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, _trueCourse);

            return alongTrackDistance.Meters <= 0;
        }

        public (Bearing requiredTrueCourse, Length crossTrackError, Length alongTrackDistance, Length turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            // Otherwise calculate cross track error for this leg
            (Bearing requiredTrueCourse, Length alongTrackDistance, Length crossTrackError) = AviationUtil.CalculateLinearCourseIntercept(aircraft.Position.PositionGeoPoint, _endPoint.Point.PointPosition, _trueCourse);

            return (requiredTrueCourse, crossTrackError, alongTrackDistance, (Length)0);
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            (Bearing requiredTrueCourse, Length crossTrackError, _, _) = GetCourseInterceptInfo(aircraft);

            // If there's no error
            Angle trackDelta = aircraft.Position.Track_True - requiredTrueCourse;
            if (Math.Abs(trackDelta.Radians) < double.Epsilon)
            {
                return false;
            }

            // Find cross track error to start turn (distance from intersection)
            //double demandedTrack = AutopilotUtil.CalculateDemandedTrackOnCurrentTrack(crossTrackError, aircraft.Position.Track_True, requiredTrueCourse, aircraft.Position.Bank,
            //aircraft.Position.GroundSpeed, intervalMs).demandedTrack;

            (Length turnLeadDist, _, GeoPoint intersection) = AviationUtil.CalculateTurnLeadDistance(
                aircraft.Position.PositionGeoPoint,
                _endPoint.Point.PointPosition,
                aircraft.Position.Track_True,
                aircraft.Position.TrueAirSpeed,
                _trueCourse,
                aircraft.Position.WindDirection,
                aircraft.Position.WindSpeed,
                AutopilotUtil.ROLL_LIMIT,
                AngularVelocity.FromDegreesPerSecond(3)
                ).GetValueOrDefault(((Length) 0, (Length) (-1), null));

            turnLeadDist *= 1.2;

            return (GeoPoint.FlatDistance(aircraft.Position.PositionGeoPoint, intersection) < turnLeadDist);
            //double requestedTurnDelta = GeoUtil.CalculateTurnAmount(demandedTrack, aircraft.Position.Track_True);
            //return (trackDelta > 0 && requestedTurnDelta > 0 || trackDelta < 0 && requestedTurnDelta < 0);
        }

        public FmsPoint StartPoint => null;

        public FmsPoint EndPoint => _endPoint;

        public Bearing InitialTrueCourse => Bearing.FromDegrees(-1);

        public Bearing FinalTrueCourse => _trueCourse;

        public Length LegLength => (Length) 0;

        public override string ToString()
        {
            return $"{_magneticCourse.Degrees:000.0} =(CF)=> {_endPoint}";
        }

        public void ProcessLeg(SimAircraft aircraft, int intervalMs)
        {
        }

        public void InitializeLeg(SimAircraft aircraft)
        {
        }

        public List<NdLine> UiLines => new List<NdLine>();

        public List<(Length, int)> DecelPoints => new List<(Length, int)>();
    }
}
