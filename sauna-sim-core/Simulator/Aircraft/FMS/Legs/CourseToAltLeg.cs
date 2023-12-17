using System;
using System.Collections.Generic;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.Magnetic;
using AviationCalcUtilNet.Units;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class CourseToAltLeg : IRouteLeg
    {
        private Bearing _magneticCourse;
        private Bearing _trueCourse;
        private Length _endAlt;
        private Length _beginAlt;
        private MagneticTileManager _magTileMgr;

        public CourseToAltLeg(Length endAlt, BearingTypeEnum courseType, Bearing course, MagneticTileManager magTileManager)
        {
            _magTileMgr = magTileManager;
            _endAlt = endAlt;
            _beginAlt = Length.FromFeet(-1);
            if (courseType == BearingTypeEnum.TRUE)
            {
                _trueCourse = course;
                _magneticCourse = Bearing.FromDegrees(-1);
            } else
            {
                _magneticCourse = course;
                _trueCourse = Bearing.FromDegrees(-1);
            }
        }

        public FmsPoint StartPoint => null;

        public FmsPoint EndPoint => null;

        public Bearing InitialTrueCourse => _trueCourse;

        public Bearing FinalTrueCourse => _trueCourse;

        public Length LegLength => Length.FromFeet(0);

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.COURSE_TO_ALT;

        public bool HasLegTerminated(SimAircraft aircraft)
        {
            if (_beginAlt < Length.FromFeet(0))
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
            if (_beginAlt.Feet < 0)
            {
                if (_trueCourse.Degrees < 0)
                {
                    _trueCourse = _magTileMgr.MagneticToTrue(aircraft.Position.PositionGeoPoint, DateTime.UtcNow, _magneticCourse);
                } else if (_magneticCourse.Degrees < 0)
                {
                    _magneticCourse = _magTileMgr.TrueToMagnetic(aircraft.Position.PositionGeoPoint, DateTime.UtcNow, _trueCourse);
                }
                _beginAlt = aircraft.Position.IndicatedAltitude;
            }

            return (_trueCourse, (Length) 0, (Length)0, (Length)0);
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            // Never preempt turn
            return false;
        }

        public override string ToString()
        {
            return $"{_magneticCourse.Degrees:000} =(CA)=> {_endAlt.Feet}";
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
