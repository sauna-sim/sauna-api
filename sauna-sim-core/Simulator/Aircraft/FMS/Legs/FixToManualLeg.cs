using System;
using System.Collections.Generic;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.GeoTools.MagneticTools;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class FixToManualLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private double _magneticCourse;
        private double _trueCourse;

        public FixToManualLeg(FmsPoint startPoint, BearingTypeEnum courseType, double course)
        {
            _startPoint = startPoint;

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

        public double LegLength => 0;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.FIX_TO_MANUAL;

        public bool HasLegTerminated(SimAircraft aircraft)
        {
            // Manual termination
            return false;
        }

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance, double turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            // Otherwise calculate cross track error for this leg
            double crossTrackError = GeoUtil.CalculateCrossTrackErrorM(aircraft.Position.PositionGeoPoint, _startPoint.Point.PointPosition, _trueCourse,
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
            double demandedTrack = AutopilotUtil.CalculateDemandedTrackOnCurrentTrack(crossTrackError, aircraft.Position.Track_True, requiredTrueCourse, aircraft.Position.Bank,
                aircraft.Position.GroundSpeed, intervalMs).demandedTrack;

            double requestedTurnDelta = GeoUtil.CalculateTurnAmount(demandedTrack, aircraft.Position.Track_True);
            return (trackDelta > 0 && requestedTurnDelta > 0 || trackDelta < 0 && requestedTurnDelta < 0);
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
