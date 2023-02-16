using SaunaSim.Core;

namespace SaunaSim.Api.ApiObjects.Commands
{
    public class InterceptCourseCommandRequest
    {
        public string Callsign { get; set; }
        public string Waypoint { get; set; }
        public int Course { get; set; }
    }
}