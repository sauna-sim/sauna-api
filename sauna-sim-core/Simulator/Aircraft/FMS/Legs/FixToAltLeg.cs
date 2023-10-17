using System;
using System.Net;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.GeoTools.MagneticTools;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class FixToAltLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private double _magneticCourse;
        private double _trueCourse;
        private double _endAlt;
        private double _beginAlt;

        public FixToAltLeg(FmsPoint startPoint, BearingTypeEnum courseType, double course, double endAlt)
        {
            _startPoint = startPoint;
            _endAlt = endAlt;
            _beginAlt = -1;

            if (courseType == BearingTypeEnum.TRUE)
            {
                _trueCourse = course;
                _magneticCourse = MagneticUtil.ConvertTrueToMagneticTile(_trueCourse, startPoint.Point.PointPosition);
            }
            else
            {
                _magneticCourse = course;
                _trueCourse = MagneticUtil.ConvertMagneticToTrueTile(_magneticCourse, startPoint.Point.PointPosition);
            }
        }

        public FmsPoint StartPoint => _startPoint;

        public FmsPoint EndPoint => null;

        public double InitialTrueCourse => _trueCourse;

        public double FinalTrueCourse => -1;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.FIX_TO_ALT;

        public override string ToString()
        {
            return $"{_startPoint.Point.PointName}-{_magneticCourse:000} =(FA)=> {_endAlt}";
        }

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

        public (double requiredTrueCourse, double crossTrackError, double turnRadius) UpdateForLnav(SimAircraft aircraft, int intervalMs)
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
            }

            // Update CrossTrackError, etc
            (double requiredTrueCourse, double crossTrackError, _) = GetCourseInterceptInfo(aircraft);

            return (requiredTrueCourse, crossTrackError, -1);
        }

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            // Otherwise calculate cross track error for this leg
            double crossTrackError = GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _startPoint.Point.PointPosition, _trueCourse,
                out double requiredTrueCourse, out double alongTrackDistance);

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
    }
}
