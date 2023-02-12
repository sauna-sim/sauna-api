namespace AselAtcTrainingSim.AselApi.ApiObjects.Commands
{
    public class DegTurnCommandRequest
    {
        public string Callsign { get; set; }
        public int DegreesToTurn { get; set; }
    }
}