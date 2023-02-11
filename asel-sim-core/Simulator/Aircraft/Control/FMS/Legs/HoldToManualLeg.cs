using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AselAtcTrainingSim.AselSimCore.Data;
using AselAtcTrainingSim.AselSimCore.Simulator.Aircraft.Control.Instructions.Lateral;

namespace AselAtcTrainingSim.AselSimCore.Simulator.Aircraft.Control.FMS.Legs
{
    public class HoldToManualLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private FmsPoint _endPoint;
        private HoldingPatternInstruction _instr;
        private bool _exitArmed;

        public HoldToManualLeg(FmsPoint startPoint, BearingTypeEnum courseType, double inboundCourse, HoldTurnDirectionEnum turnDir, HoldLegLengthTypeEnum legLengthType, double legLength)
        {
            _startPoint = startPoint;
            _endPoint = new FmsPoint(startPoint.Point, RoutePointTypeEnum.FLY_OVER);
            _instr = new HoldingPatternInstruction(startPoint.Point, courseType, inboundCourse, turnDir, legLengthType, legLength);
            _exitArmed = false;
        }

        public FmsPoint StartPoint => _startPoint;

        public FmsPoint EndPoint => _endPoint;

        public double InitialTrueCourse => _instr.TrueCourse;

        public double FinalTrueCourse => _instr.TrueCourse;

        public ILateralControlInstruction Instruction => _instr;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.HOLD_TO_MANUAL;

        public bool HasLegTerminated(AircraftPosition pos, ref AircraftFms fms)
        {
            if (_exitArmed)
            {
                return _instr.HoldPhase == HoldPhaseEnum.INBOUND && _instr.Instruction.AlongTrackM <= 0;
            }
            return false;
        }

        public bool ShouldBeginTurn(AircraftPosition pos, AircraftFms fms, int posCalcIntvl)
        {
            return false;
        }

        public void UpdateLateralPosition(ref AircraftPosition pos, ref AircraftFms fms, int posCalcIntvl)
        {
            IRouteLeg nextLeg = fms.GetFirstLeg();

            // Only sequence if next leg exists and fms is not suspended
            if (nextLeg != null && !fms.Suspended)
            {
                if (HasLegTerminated(pos, ref fms))
                {
                    // Activate next leg on termination
                    fms.ActivateNextLeg();
                }
            }

            // Otherwise update position as normal
            _instr.UpdatePosition(ref pos, ref fms, posCalcIntvl);
        }

        public void UpdateVerticalPosition(ref AircraftPosition pos, ref AircraftFms fms, int posCalcIntvl)
        {
            throw new NotImplementedException();
        }
        public override string ToString()
        {
            return $"{_startPoint} =(HM)=> {_instr.HoldPhase.ToString()}";
        }
    }
}
