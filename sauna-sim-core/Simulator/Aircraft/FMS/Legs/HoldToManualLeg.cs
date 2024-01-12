using System;
using System.Collections.Generic;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Magnetic;
using AviationCalcUtilNet.Units;
using NavData_Interface.Objects;
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

        public HoldToManualLeg(FmsPoint startPoint, BearingTypeEnum courseType, Bearing inboundCourse, HoldTurnDirectionEnum turnDir, HoldLegLengthTypeEnum legLengthType, double legLength, MagneticTileManager magTileMgr)
        {
            _startPoint = startPoint;
            _endPoint = new FmsPoint(startPoint.Point, RoutePointTypeEnum.FLY_OVER);
            _instr = new ApFmsHoldController(startPoint.Point, courseType, inboundCourse, turnDir, legLengthType, legLength, magTileMgr);
        }

        public FmsPoint StartPoint => _startPoint;

        public FmsPoint EndPoint => _endPoint;

        public Bearing InitialTrueCourse => _instr.TrueCourse;

        public Bearing FinalTrueCourse => _instr.TrueCourse;

        public Length LegLength => (Length)0;

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
                (_, _, Length alongTrackM, _) = _instr.GetCourseInterceptInfo(aircraft);
                return alongTrackM.Meters <= 0;
            }
            return false;
        }

        public void ProcessLeg(SimAircraft aircraft, int intervalMs)
        {
            _instr.ProcessLeg(aircraft, intervalMs);
        }

        public (Bearing requiredTrueCourse, Length crossTrackError, Length alongTrackDistance, Length turnRadius) GetCourseInterceptInfo(SimAircraft aircraft)
        {
            return _instr.GetCourseInterceptInfo(aircraft);
        }

        public bool ShouldActivateLeg(SimAircraft aircraft, int intervalMs)
        {
            // All holds are fly-over waypoints, so we never activate this leg early
            return false;
        }

        public void InitializeLeg(SimAircraft aircraft)
        {
            _instr.CalculateHold(aircraft);
        }

        public List<NdLine> UiLines => _instr.GetUiLines();
    }
}
