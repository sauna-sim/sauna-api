using SaunaSim.Core.Data;

namespace SaunaSim.Api.ApiObjects.Commands
{
    public class HoldCommandRequest
    {
        public string Callsign { get; set; }
        public string Waypoint { get; set; }
        public bool PublishedHold { get; set; } = true;
        public int InboundCourse { get; set; }
        public HoldTurnDirectionEnum TurnDirection { get; set; } = HoldTurnDirectionEnum.RIGHT;
        public HoldLegLengthTypeEnum LegLengthType { get; set; } = HoldLegLengthTypeEnum.DEFAULT;
        public double LegLength { get; set; }
    }
}