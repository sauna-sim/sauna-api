namespace SaunaSim.Api.ApiObjects.Commands
{
    public class DegTurnCommandRequest
    {
        public string Callsign { get; set; }
        public int DegreesToTurn { get; set; }
    }
}