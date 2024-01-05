using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NavData_Interface.Objects.Fixes;
using SaunaSim.Core.Simulator.Aircraft;

namespace SaunaSim.Core.Data
{
    public enum HoldLegLengthTypeEnum
    {
        DEFAULT,
        DISTANCE,
        TIME
    }

    public enum HoldTurnDirectionEnum
    {
        LEFT = -1,
        RIGHT = 1
    }

    public class PublishedHold
    {
        private Fix _wp;
        private double _inboundCourse;
        private HoldTurnDirectionEnum _turnDirection;
        private HoldLegLengthTypeEnum _lengthType;
        private double _legLength;

        public PublishedHold(Fix wp, double inboundCourse, HoldTurnDirectionEnum turnDirection, HoldLegLengthTypeEnum legLengthType, double legLength)
        {
            _wp = wp;
            _inboundCourse = inboundCourse;
            _turnDirection = turnDirection;
            _lengthType = legLengthType;
            _legLength = legLength;
        }

        public PublishedHold(Fix wp, double inboundCourse, HoldTurnDirectionEnum turnDirection) :
            this(wp, inboundCourse, turnDirection, HoldLegLengthTypeEnum.DEFAULT, -1)
        { }

        public PublishedHold(Fix wp, double inboundCourse, HoldLegLengthTypeEnum legLengthType, double legLength) :
            this(wp, inboundCourse, HoldTurnDirectionEnum.RIGHT, legLengthType, legLength)
        { }

        public PublishedHold(Fix wp, double inboundCourse) :
            this(wp, inboundCourse, HoldTurnDirectionEnum.RIGHT, HoldLegLengthTypeEnum.DEFAULT, -1)
        { }

        public Fix Waypoint => _wp;

        public double InboundCourse => _inboundCourse;

        public HoldTurnDirectionEnum TurnDirection => _turnDirection;

        public HoldLegLengthTypeEnum LegLengthType => _lengthType;

        public double LegLength => _legLength;
    }
}
