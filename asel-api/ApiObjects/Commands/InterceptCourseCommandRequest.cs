using AselAtcTrainingSim.AselSimCore;

namespace AselAtcTrainingSim.AselApi.ApiObjects.Commands
{
    public class InterceptCourseCommandRequest
    {
        public string Callsign { get; set; }
        public string Waypoint { get; set; }
        public int Course { get; set; }
    }
}