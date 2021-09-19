using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS.Legs
{
    public class CourseToFixLeg : IRouteLeg
    {
        private FmsPoint _endPoint;
        private double _magneticCourse;
        private double _trueCourse;
        private InterceptCourseInstruction _instr;

        public CourseToFixLeg(FmsPoint endPoint, BearingTypeEnum courseType, double course)
        {
            _endPoint = endPoint;
            if (courseType == BearingTypeEnum.TRUE)
            {
                _trueCourse = course;
                _instr = new InterceptCourseInstruction(_endPoint.Point)
                {
                    TrueCourse = course
                };
                _magneticCourse = _instr.MagneticCourse;
            }
            else
            {
                _magneticCourse = course;
                _instr = new InterceptCourseInstruction(_endPoint.Point, course);
                _trueCourse = _instr.TrueCourse;
            }
        }

        public ILateralControlInstruction Instruction => _instr;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.COURSE_TO_FIX;

        public FmsPoint StartPoint => null;

        public FmsPoint EndPoint => _endPoint;

        public double InitialTrueCourse => -1;

        public double FinalTrueCourse => _trueCourse;

        public bool ShouldBeginTurn(AircraftPosition pos, AircraftFms fms, int posCalcIntvl)
        {
            return _instr.ShouldActivateInstruction(pos, fms, posCalcIntvl);
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
                else if (_endPoint.PointType == RoutePointTypeEnum.FLY_BY &&
                    nextLeg.ShouldBeginTurn(pos, fms, posCalcIntvl) &&
                    nextLeg.InitialTrueCourse >= 0 &&
                    Math.Abs(FinalTrueCourse - nextLeg.InitialTrueCourse) > 0.5)
                {
                    // Begin turn to next leg, but do not activate
                    nextLeg.Instruction.UpdatePosition(ref pos, ref fms, posCalcIntvl);

                    return;
                }
            }

            // Otherwise update position as normal
            _instr.UpdatePosition(ref pos, ref fms, posCalcIntvl);
        }

        public override string ToString()
        {
            return $"{_magneticCourse:000.0} =(CF)=> {_endPoint}";
        }

        public void UpdateVerticalPosition(ref AircraftPosition pos, ref AircraftFms fms, int posCalcIntvl)
        {
            throw new NotImplementedException();
        }

        public bool HasLegTerminated(AircraftPosition pos, ref AircraftFms fms)
        {
            // Leg terminates when aircraft passes abeam/over terminating point
            _instr.UpdateInfo(pos, ref fms);
            return _instr.AlongTrackM <= 0;
        }
    }
}
