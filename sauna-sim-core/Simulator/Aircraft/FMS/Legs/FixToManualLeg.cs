using System;
using System.Collections.Generic;
using AviationCalcUtilNet.Aviation;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.Magnetic;
using AviationCalcUtilNet.Units;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class FixToManualLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private Bearing _magneticCourse;
        private Bearing _trueCourse;

        public FixToManualLeg(FmsPoint startPoint, BearingTypeEnum courseType, Bearing course, MagneticTileManager magTileMgr)
        {
            _startPoint = startPoint;

            if (courseType == BearingTypeEnum.TRUE)
            {
                _trueCourse = course;
                _magneticCourse = magTileMgr.TrueToMagnetic(startPoint.Point.PointPosition, DateTime.UtcNow, _trueCourse);
            }
            else
            {
                _magneticCourse = course;
                _trueCourse = magTileMgr.MagneticToTrue(startPoint.Point.PointPosition, DateTime.UtcNow, _magneticCourse);
            }
        }

        public FmsPoint StartPoint => _startPoint;

        public FmsPoint EndPoint => null;

        public Bearing InitialTrueCourse => _trueCourse;

        public Bearing FinalTrueCourse => null;

        public Length LegLength => (Length) 0;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.FIX_TO_MANUAL;

        public bool HasLegTerminated(SimAircraft aircraft)
        {
            // Manual termination
            return false;
        }

        public (Bearing requiredTrueCourse, Length crossTrackError, Length alongTrackDistance, Length turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            // Otherwise calculate cross track error for this leg
            (Bearing requiredTrueCourse, Length alongTrackDistance, Length crossTrackError) = AviationUtil.CalculateLinearCourseIntercept(aircraft.Position.PositionGeoPoint, _startPoint.Point.PointPosition, _trueCourse);

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
            Bearing demandedTrack = AutopilotUtil.CalculateDemandedTrackOnCurrentTrack(crossTrackError, aircraft.Position.Track_True, requiredTrueCourse, aircraft.Position.Bank,
                aircraft.Position.GroundSpeed, intervalMs).demandedTrack;

            Angle requestedTurnDelta = aircraft.Position.Track_True - demandedTrack;
            return (trackDelta.Radians > 0 && requestedTurnDelta.Radians > 0 || trackDelta.Radians < 0 && requestedTurnDelta.Radians < 0);
        }

        public override string ToString()
        {
            return $"{_startPoint.Point.PointName}-{_magneticCourse:000} =(FM)=> MANUAL";
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
