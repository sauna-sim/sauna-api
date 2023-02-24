using AviationCalcUtilNet.GeoTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaunaSim.Core.Simulator.Aircraft.Control.FMS.Legs
{
    public class DirectToFixLeg : IRouteLeg
    {
        private FmsPoint _endPoint;
        private double _trueCourse;
        private InterceptCourseInstruction _instr;

        public DirectToFixLeg(FmsPoint point)
        {
            _endPoint = point;
            _trueCourse = -1;
        }

        public FmsPoint StartPoint => null;

        public FmsPoint EndPoint => _endPoint;

        public double InitialTrueCourse => -1;

        public double FinalTrueCourse => _trueCourse;

        public ILateralControlInstruction Instruction => _instr;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.DIRECT_TO_FIX;

        public bool HasLegTerminated(AircraftPosition pos, ref AircraftFms fms)
        {
            if (_instr == null)
            {
                return false;
            }

            // Leg terminates when aircraft passes abeam/over terminating point
            _instr.UpdateInfo(pos, ref fms);
            return _instr.AlongTrackM <= 0;
        }

        public bool ShouldBeginTurn(AircraftPosition pos, AircraftFms fms, int posCalcIntvl)
        {
            if (_instr == null)
            {
                return false;
            }

            return _instr.ShouldActivateInstruction(pos, fms, posCalcIntvl);
        }

        public void UpdateLateralPosition(ref AircraftPosition pos, ref AircraftFms fms, int posCalcIntvl)
        {
            // Check if track has been set
            if (_trueCourse < 0 || _instr == null)
            {
                _trueCourse = GeoUtil.CalculateDirectBearingAfterTurn(
                        pos.Position,
                        _endPoint.Point.PointPosition,
                        GeoUtil.CalculateRadiusOfTurn(GeoUtil.CalculateMaxBankAngle(pos.GroundSpeed, 25, 3), pos.GroundSpeed),
                        pos.Track_True);

                if (_trueCourse < 0)
                {
                    _trueCourse = GeoPoint.FinalBearing(pos.Position, _endPoint.Point.PointPosition);
                }

                _instr = new InterceptCourseInstruction(_endPoint.Point)
                {
                    TrueCourse = _trueCourse
                };
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

        public void UpdateVerticalPosition(ref AircraftPosition pos, ref AircraftFms fms, int posCalcIntvl)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"PPOS =(DF)=> {_endPoint}";
        }
    }
}
