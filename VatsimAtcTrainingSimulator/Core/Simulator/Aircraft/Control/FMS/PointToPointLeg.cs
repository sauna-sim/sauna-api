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
    public class PointToPointLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private FmsPoint _endPoint;
        private double _finalBearing;
        private InterceptCourseInstruction _instr;

        public event EventHandler<WaypointPassedEventArgs> WaypointPassed;

        public PointToPointLeg(FmsPoint startPoint, FmsPoint endPoint)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _finalBearing = GeoPoint.FinalBearing(_startPoint.Point.PointPosition, _endPoint.Point.PointPosition);
            _instr = new InterceptCourseInstruction(_endPoint.Point)
            {
                TrueCourse = _finalBearing
            };
            _instr.WaypointCrossed += OnWaypointPassed;
        }

        public ILateralControlInstruction Instruction => _instr;

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.POINT_TO_POINT;

        public FmsPoint StartPoint => _startPoint;

        public FmsPoint EndPoint => _endPoint;

        public bool ShouldActivateLeg(AircraftPosition pos, AircraftFms fms, int posCalcIntvl)
        {
            return _instr.ShouldActivateInstruction(pos, fms, posCalcIntvl);
        }

        public void UpdatePosition(ref AircraftPosition pos, ref AircraftFms fms, int posCalcIntvl)
        {
            _instr.UpdatePosition(ref pos, ref fms, posCalcIntvl);
        }

        private void OnWaypointPassed(object sender, WaypointPassedEventArgs e)
        {
            WaypointPassed?.Invoke(sender, e);
        }

        public override string ToString()
        {
            return $"{_startPoint} => {_endPoint}";
        }
    }
}
