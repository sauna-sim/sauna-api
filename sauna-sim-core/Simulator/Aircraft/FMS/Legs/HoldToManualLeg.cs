using System;
using System.Collections.Generic;
using AviationCalcUtilNet.GeoTools;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

namespace SaunaSim.Core.Simulator.Aircraft.FMS.Legs
{
    public class HoldToManualLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private FmsPoint _endPoint;
        private ApFmsHoldController _instr;

        public ApFmsHoldController Instr => _instr;

        public HoldToManualLeg(FmsPoint startPoint, BearingTypeEnum courseType, double inboundCourse, HoldTurnDirectionEnum turnDir, HoldLegLengthTypeEnum legLengthType, double legLength)
        {
            _startPoint = startPoint;
            _endPoint = new FmsPoint(startPoint.Point, RoutePointTypeEnum.FLY_OVER);
            _instr = new ApFmsHoldController(startPoint.Point, courseType, inboundCourse, turnDir, legLengthType, legLength);
        }

        public FmsPoint StartPoint => _startPoint;

        public FmsPoint EndPoint => _endPoint;

        public double InitialTrueCourse => _instr.TrueCourse;

        public double FinalTrueCourse => _instr.TrueCourse;

        public double LegLength => 0;

        public bool ExitArmed
        {
            get => _instr.ExitArmed;
            set => _instr.ExitArmed = value;
        }

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
            if (_instr.ExitArmed && _instr.HoldPhase == HoldPhaseEnum.INBOUND)
            {
                (_, _, double alongTrackM, _) = _instr.GetCourseInterceptInfo(aircraft);
                return alongTrackM <= 0;
            }
            return false;
        }

        public void ProcessLeg(SimAircraft aircraft, int intervalMs)
        {
            _instr.ProcessLeg(aircraft, intervalMs);
        }

        public (double requiredTrueCourse, double crossTrackError, double alongTrackDistance, double turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            return _instr.GetCourseInterceptInfo(aircraft);
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            // All holds are fly-over waypoints, so we never activate this leg early
            return false;
        }

        public List<NdLine> UiLines => _instr.GetUiLines();
    }
}
