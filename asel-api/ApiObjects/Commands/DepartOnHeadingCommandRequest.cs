namespace AselAtcTrainingSim.AselApi.ApiObjects.Commands
{
    public class DepartOnHeadingCommandRequest
    {
        public string Callsign { get; set; }
        public string Waypoint { get; set; }
        public int Heading { get; set; }
    }
}