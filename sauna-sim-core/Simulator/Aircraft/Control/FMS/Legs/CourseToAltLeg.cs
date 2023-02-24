using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.GeoTools.MagneticTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaunaSim.Core.Simulator.Aircraft.Control.FMS.Legs
{
    public class CourseToAltLeg : IRouteLeg
    {
        private double _magneticCourse;
        private double _trueCourse;
        private double _endAlt;
        private double _beginAlt;
        private TrackHoldInstruction _instr;

        public CourseToAltLeg(double endAlt, BearingTypeEnum courseType, double course)
        {
            _endAlt = endAlt;
            _beginAlt = -1;
            if (courseType == BearingTypeEnum.TRUE)
            {
                _trueCourse = course;
                _magneticCourse = -1;
            } else
            {
                _magneticCourse = course;
                _trueCourse = -1;
            }
        }

        public FmsPoint StartPoint => null;

        public FmsPoint EndPoint => null;

        public double InitialTrueCourse => _trueCourse;

        public double FinalTrueCourse => _trueCourse;

        public ILateralControlInstruction Instruction => _instr;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.COURSE_TO_ALT;

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
            // Never preempt turn
            return false;
        }

        public void UpdateLateralPosition(ref AircraftPosition pos, ref AircraftFms fms, int posCalcIntvl)
        {
            if (_beginAlt < 0 || _instr == null)
            {
                if (_trueCourse < 0)
                {
                    _trueCourse = MagneticUtil.ConvertMagneticToTrueTile(_magneticCourse, pos.Position);
                } else if (_magneticCourse < 0)
                {
                    _magneticCourse = MagneticUtil.ConvertTrueToMagneticTile(_trueCourse, pos.Position);
                }
                _beginAlt = pos.IndicatedAltitude;

                _instr = new TrackHoldInstruction(_trueCourse);
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
            return $"{_magneticCourse:000} =(CA)=> {_endAlt}";
        }
    }
}
