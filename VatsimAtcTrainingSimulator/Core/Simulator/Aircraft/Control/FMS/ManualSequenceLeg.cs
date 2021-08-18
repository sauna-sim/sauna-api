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
    public enum InterceptTypeEnum
    {
        MAGNETIC_TRACK,
        HEADING,
        TRUE_TRACK
    }

    public class ManualSequenceLeg : IRouteLeg
    {
        private FmsPoint _startPoint;
        private double _headingTrack;
        private double _trueTrack;
        private InterceptTypeEnum _sequenceType;
        private ILateralControlInstruction _instr;

        public event EventHandler<WaypointPassedEventArgs> WaypointPassed;

        public ManualSequenceLeg(InterceptTypeEnum sequenceType, FmsPoint startPoint, double headingTrack)
        {
            _startPoint = startPoint;
            _headingTrack = AcftGeoUtil.NormalizeHeading(headingTrack);
            _sequenceType = sequenceType;

            if (sequenceType == InterceptTypeEnum.TRUE_TRACK)
            {
                _trueTrack = _headingTrack;
                _instr = new InterceptCourseInstruction(_startPoint.Point)
                {
                    TrueCourse = _trueTrack
                };
            }
            else if (sequenceType == InterceptTypeEnum.MAGNETIC_TRACK)
            {
                _instr = new InterceptCourseInstruction(_startPoint.Point, _headingTrack);
                _trueTrack = ((InterceptCourseInstruction)_instr).TrueCourse;
            }
            else if (sequenceType == InterceptTypeEnum.HEADING)
            {
                _instr = new HeadingHoldInstruction((int)Math.Round(_headingTrack, 1, MidpointRounding.AwayFromZero));
            }
        }

        public FmsPoint StartPoint { get => _startPoint; }

        public double HeadingTrack { get => _headingTrack; }

        public InterceptTypeEnum SequenceType { get => _sequenceType; }

        public RouteLegTypeEnum LegType => RouteLegTypeEnum.MANUAL_SEQUENCE;

        public ILateralControlInstruction Instruction => _instr;

        public FmsPoint EndPoint => null;

        public double InitialTrueCourse => _trueTrack;

        public double FinalTrueCourse => -1;

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
            return $"{_startPoint} => MANSEQ";
        }
    }
}
