using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS
{
    public class PointToAltLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private double _endAlt;
        private double _magneticCourse;
        private double _trueCourse;
        private InterceptCourseInstruction _instr;
        private double _lastAlt = -1;

        public PointToAltLeg(FmsPoint startPoint, InterceptTypeEnum interceptType, double course, double endAlt)
        {
            _startPoint = startPoint;
            if (interceptType == InterceptTypeEnum.TRUE_TRACK)
            {
                _trueCourse = course;
                _instr = new InterceptCourseInstruction(_startPoint.Point)
                {
                    TrueCourse = course
                };
                _magneticCourse = _instr.MagneticCourse;
            } else
            {
                _magneticCourse = course;
                _instr = new InterceptCourseInstruction(_startPoint.Point, course);
                _trueCourse = _instr.TrueCourse;
            }
        }

        public FmsPoint StartPoint => _startPoint;

        public FmsPoint EndPoint => null;

        public ILateralControlInstruction Instruction => _instr;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.POINT_TO_ALTITUDE;

        public double InitialTrueCourse => _trueCourse;

        public double FinalTrueCourse => -1;

        public event EventHandler<WaypointPassedEventArgs> WaypointPassed;

        public bool ShouldActivateLeg(AircraftPosition pos, AircraftFms fms, int posCalcIntvl)
        {
            return _instr.ShouldActivateInstruction(pos, fms, posCalcIntvl);
        }

        public void UpdatePosition(ref AircraftPosition pos, ref AircraftFms fms, int posCalcIntvl)
        {
            if (_lastAlt <=_endAlt && _endAlt <= pos.IndicatedAltitude)
            {
                fms.ActivateNextLeg();
            } else
            {
                _instr.UpdatePosition(ref pos, ref fms, posCalcIntvl);
            }
        }
    }
}
