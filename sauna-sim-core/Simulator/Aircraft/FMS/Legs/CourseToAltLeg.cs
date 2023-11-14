using System;
using System.Collections.Generic;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.GeoTools.MagneticTools;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class CourseToAltLeg : IRouteLeg
    {
        private double _magneticCourse;
        private double _trueCourse;
        private double _endAlt;
        private double _beginAlt;

        public CourseToAltLeg(double endAlt, BearingTypeEnum courseType, double course)
        {
            _endAlt = endAlt;
            _beginAlt = -1;
            if (courseType == BearingTypeEnum.TRUE)
            {
                _trueCourse = course;
                _magneticCourse = -1;
            } else
            {
                _magneticCourse = course;
                _trueCourse = -1;
            }
        }

        public FmsPoint StartPoint => null;

        public FmsPoint EndPoint => null;

        public double InitialTrueCourse => _trueCourse;

        public double FinalTrueCourse => _trueCourse;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.COURSE_TO_ALT;

        public bool HasLegTerminated(SimAircraft aircraft)
        {
            if (_beginAlt < 0)
            {
                _beginAlt = aircraft.Position.IndicatedAltitude;
            }

            if (_beginAlt <= _endAlt)
            {
                return aircraft.Position.IndicatedAltitude >= _endAlt;
            }
            return aircraft.Position.IndicatedAltitude <= _endAlt;
        }

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance, double turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            if (_beginAlt < 0)
            {
                if (_trueCourse < 0)
                {
                    _trueCourse = MagneticUtil.ConvertMagneticToTrueTile(_magneticCourse, aircraft.Position.PositionGeoPoint);
                } else if (_magneticCourse < 0)
                {
                    _magneticCourse = MagneticUtil.ConvertTrueToMagneticTile(_trueCourse, aircraft.Position.PositionGeoPoint);
                }
                _beginAlt = aircraft.Position.IndicatedAltitude;
            }

            return (_trueCourse, 0, 0, -1);
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            // Never preempt turn
            return false;
        }

        public override string ToString()
        {
            return $"{_magneticCourse:000} =(CA)=> {_endAlt}";
        }

        public void ProcessLeg(SimAircraft aircraft, int intervalMs)
        {
        }

        public List<NdLine> UiLines => new List<NdLine>();
    }
}
