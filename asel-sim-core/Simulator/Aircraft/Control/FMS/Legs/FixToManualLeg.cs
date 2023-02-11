using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AselAtcTrainingSim.AselSimCore.Simulator.Aircraft.Control.FMS.Legs
{
    public class FixToManualLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private double _magneticCourse;
        private double _trueCourse;
        private InterceptCourseInstruction _instr;

        public FixToManualLeg(FmsPoint startPoint, BearingTypeEnum courseType, double course)
        {
            _startPoint = startPoint;

            if (courseType == BearingTypeEnum.TRUE)
            {
                _instr = new InterceptCourseInstruction(_startPoint.Point)
                {
                    TrueCourse = course
                };
                _magneticCourse = _instr.MagneticCourse;
            }
            else
            {
                _instr = new InterceptCourseInstruction(_startPoint.Point, course);
                _trueCourse = _instr.TrueCourse;
            }
        }

        public FmsPoint StartPoint => _startPoint;

        public FmsPoint EndPoint => null;

        public double InitialTrueCourse => _trueCourse;

        public double FinalTrueCourse => -1;

        public ILateralControlInstruction Instruction => _instr;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.FIX_TO_MANUAL;

        public bool HasLegTerminated(AircraftPosition pos, ref AircraftFms fms)
        {
            // Manual termination
            return false;
        }

        public bool ShouldBeginTurn(AircraftPosition pos, AircraftFms fms, int posCalcIntvl)
        {
            return _instr.ShouldActivateInstruction(pos, fms, posCalcIntvl);
        }

        public void UpdateLateralPosition(ref AircraftPosition pos, ref AircraftFms fms, int posCalcIntvl)
        {
            // Will not auto sequence
            _instr.UpdatePosition(ref pos, ref fms, posCalcIntvl);
        }

        public void UpdateVerticalPosition(ref AircraftPosition pos, ref AircraftFms fms, int posCalcIntvl)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"{_startPoint.Point.PointName}-{_magneticCourse:000} =(FM)=> MANUAL";
        }
    }
}
