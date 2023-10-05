using System;
using AviationCalcUtilNet.GeoTools;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class HoldToManualLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private FmsPoint _endPoint;
        private ApFmsHoldController _instr;
        private bool _exitArmed;

        public HoldToManualLeg(FmsPoint startPoint, BearingTypeEnum courseType, double inboundCourse, HoldTurnDirectionEnum turnDir, HoldLegLengthTypeEnum legLengthType, double legLength)
        {
            _startPoint = startPoint;
            _endPoint = new FmsPoint(startPoint.Point, RoutePointTypeEnum.FLY_OVER);
            _instr = new ApFmsHoldController(startPoint.Point, courseType, inboundCourse, turnDir, legLengthType, legLength);
            _exitArmed = false;
        }

        public FmsPoint StartPoint => _startPoint;

        public FmsPoint EndPoint => _endPoint;

        public double InitialTrueCourse => _instr.TrueCourse;

        public double FinalTrueCourse => _instr.TrueCourse;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.HOLD_TO_MANUAL;
        public bool ShouldBeginTurn(AircraftPosition pos, AircraftFms fms, int posCalcIntvl)
        {
            return false;
        }

        public override string ToString()
        {
            return $"{_startPoint} =(HM)=> {_instr.HoldPhase}";
        }

        public bool HasLegTerminated(SimAircraft aircraft)
        {
            if (_exitArmed)
            {
                return _instr.HoldPhase == HoldPhaseEnum.INBOUND && _instr.AlongTrack_M <= 0;
            }
            return false;
        }

        public (double requiredTrueCourse, double crossTrackError) UpdateForLnav(SimAircraft aircraft, int intervalMs)
        {
            return _instr.UpdateForLnav(aircraft, intervalMs);
        }

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            return _instr.GetCourseInterceptInfo(aircraft);
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            // All holds are fly-over waypoints, so we never activate this leg early
            return false;
        }
    }
}
