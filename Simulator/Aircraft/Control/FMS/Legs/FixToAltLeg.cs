using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AselAtcTrainingSim.AselSimCore.Simulator.Aircraft.Control.FMS.Legs
{
    public class FixToAltLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private double _magneticCourse;
        private double _trueCourse;
        private double _endAlt;
        private double _beginAlt;
        private InterceptCourseInstruction _instr;

        public FixToAltLeg(FmsPoint startPoint, BearingTypeEnum courseType, double course, double endAlt)
        {
            _startPoint = startPoint;
            _endAlt = endAlt;
            _beginAlt = -1;

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

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.FIX_TO_ALT;

        public bool HasLegTerminated(AircraftPosition pos, ref AircraftFms fms)
        {
            if (_beginAlt < 0)
            {
                _beginAlt = pos.IndicatedAltitude;
            }

            if (_beginAlt <= _endAlt)
            {
                return pos.IndicatedAltitude >= _endAlt;
            }
            return pos.IndicatedAltitude <= _endAlt;
        }

        public bool ShouldBeginTurn(AircraftPosition pos, AircraftFms fms, int posCalcIntvl)
        {
            return _instr.ShouldActivateInstruction(pos, fms, posCalcIntvl);
        }

        public void UpdateLateralPosition(ref AircraftPosition pos, ref AircraftFms fms, int posCalcIntvl)
        {
            if (_beginAlt < 0)
            {
                _beginAlt = pos.IndicatedAltitude;
            }

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
            return $"{_startPoint.Point.PointName}-{_magneticCourse:000} =(FA)=> {_endAlt}";
        }
    }
}
