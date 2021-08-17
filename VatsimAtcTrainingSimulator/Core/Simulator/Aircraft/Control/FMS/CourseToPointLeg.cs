using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.GeoTools;
using VatsimAtcTrainingSimulator.Core.GeoTools.Helpers;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS
{
    public class CourseToPointLeg : IRouteLeg
    {
        private FmsPoint _endPoint;
        private double _magneticCourse;
        private double _trueCourse;
        private InterceptCourseInstruction _instr;

        public event EventHandler<WaypointPassedEventArgs> WaypointPassed;

        public CourseToPointLeg(FmsPoint endPoint, InterceptTypeEnum interceptType, double course)
        {
            _endPoint = endPoint;
            if (interceptType == InterceptTypeEnum.TRUE_TRACK)
            {
                _trueCourse = course;
                _instr = new InterceptCourseInstruction(_endPoint.Point)
                {
                    TrueCourse = course
                };
                _instr.WaypointCrossed += OnWaypointPassed;
                _magneticCourse = _instr.MagneticCourse;
            } else
            {
                _magneticCourse = course;
                _instr = new InterceptCourseInstruction(_endPoint.Point, course);
                _instr.WaypointCrossed += OnWaypointPassed;
                _trueCourse = _instr.TrueCourse;
            }
        }

        private void OnWaypointPassed(object sender, WaypointPassedEventArgs e)
        {
            WaypointPassed.Invoke(sender, e);
        }

        public ILateralControlInstruction Instruction => _instr;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.COURSE_TO_POINT;

        public FmsPoint StartPoint => null;

        public FmsPoint EndPoint => _endPoint;

        public bool ShouldActivateLeg(AircraftPosition pos, AircraftFms fms, int posCalcIntvl)
        {
            return _instr.ShouldActivateInstruction(pos, fms, posCalcIntvl);
        }

        public void UpdatePosition(ref AircraftPosition pos, ref AircraftFms fms, int posCalcIntvl)
        {
            _instr.UpdatePosition(ref pos, ref fms, posCalcIntvl);
        }

        public override string ToString()
        {
            return $"Intercept Course {_magneticCourse:000.0} => {_endPoint}";
        }
    }
}
