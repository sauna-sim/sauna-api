using System;
using System.Collections.Generic;
using System.Net;
using AviationCalcUtilNet.Aviation;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.Magnetic;
using AviationCalcUtilNet.Units;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class FixToAltLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private Bearing _magneticCourse;
        private Bearing _trueCourse;
        private Length _endAlt;
        private Length _beginAlt;

        public FixToAltLeg(FmsPoint startPoint, BearingTypeEnum courseType, Bearing course, Length endAlt, MagneticTileManager magTileManager)
        {
            _startPoint = startPoint;
            _endAlt = endAlt;
            _beginAlt = (Length)(-1);

            if (courseType == BearingTypeEnum.TRUE)
            {
                _trueCourse = course;
                _magneticCourse = magTileManager.TrueToMagnetic(startPoint.Point.PointPosition, DateTime.UtcNow, _trueCourse);
            }
            else
            {
                _magneticCourse = course;
                _trueCourse = magTileManager.MagneticToTrue(startPoint.Point.PointPosition, DateTime.UtcNow, _magneticCourse);
            }
        }

        public FmsPoint StartPoint => _startPoint;

        public FmsPoint EndPoint => null;

        public Bearing InitialTrueCourse => _trueCourse;

        public Bearing FinalTrueCourse => null;

        public Length LegLength => (Length) 0;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.FIX_TO_ALT;

        public override string ToString()
        {
            return $"{_startPoint.Point.PointName}-{_magneticCourse:000} =(FA)=> {_endAlt}";
        }

        public bool HasLegTerminated(SimAircraft aircraft)
        {
            if (_beginAlt.Feet < 0)
            {
                _beginAlt = aircraft.Position.IndicatedAltitude;
            }

            if (_beginAlt <= _endAlt)
            {
                return aircraft.Position.IndicatedAltitude >= _endAlt;
            }
            return aircraft.Position.IndicatedAltitude <= _endAlt;
        }

        public (Bearing requiredTrueCourse, Length crossTrackError, Length alongTrackDistance, Length turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            // Otherwise calculate cross track error for this leg
            (Bearing requiredTrueCourse, Length alongTrackDistance, Length crossTrackError) = AviationUtil.CalculateLinearCourseIntercept(aircraft.Position.PositionGeoPoint, _startPoint.Point.PointPosition, _trueCourse);

            return (requiredTrueCourse, crossTrackError, alongTrackDistance, (Length) 0);
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            (Bearing requiredTrueCourse, Length crossTrackError, _, _) = GetCourseInterceptInfo(aircraft);

            // If there's no error
            Angle trackDelta = aircraft.Position.Track_True - requiredTrueCourse;
            if (Math.Abs(trackDelta.Degrees) < double.Epsilon)
            {
                return false;
            }

            // Find cross track error to start turn (distance from intersection)
            Bearing demandedTrack = AutopilotUtil.CalculateDemandedTrackOnCurrentTrack(crossTrackError, aircraft.Position.Track_True, requiredTrueCourse, aircraft.Position.Bank,
                aircraft.Position.GroundSpeed, intervalMs).demandedTrack;

            Angle requestedTurnDelta = aircraft.Position.Track_True - demandedTrack;
            return (trackDelta.Radians > 0 && requestedTurnDelta.Radians > 0 || trackDelta.Radians < 0 && requestedTurnDelta.Radians < 0);
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
