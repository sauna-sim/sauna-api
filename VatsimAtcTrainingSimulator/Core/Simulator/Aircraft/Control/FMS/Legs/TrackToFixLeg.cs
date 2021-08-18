using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.GeoTools;
using VatsimAtcTrainingSimulator.Core.GeoTools.Helpers;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS.Legs
{
    public class TrackToFixLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private FmsPoint _endPoint;
        private double _initialBearing;
        private double _finalBearing;
        private InterceptCourseInstruction _instr;

        public TrackToFixLeg(FmsPoint startPoint, FmsPoint endPoint)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _initialBearing = GeoPoint.InitialBearing(_startPoint.Point.PointPosition, _endPoint.Point.PointPosition);
            _finalBearing = GeoPoint.FinalBearing(_startPoint.Point.PointPosition, _endPoint.Point.PointPosition);
            _instr = new InterceptCourseInstruction(_endPoint.Point)
            {
                TrueCourse = _finalBearing
            };
        }

        public ILateralControlInstruction Instruction => _instr;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.TRACK_TO_FIX;

        public double InitialTrueCourse => _initialBearing;

        public double FinalTrueCourse => _finalBearing;

        public FmsPoint EndPoint => _endPoint;

        public FmsPoint StartPoint => _startPoint;

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
            return $"{_startPoint} =(TF)=> {_endPoint}";
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
