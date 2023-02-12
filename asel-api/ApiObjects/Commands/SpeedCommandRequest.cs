using AselAtcTrainingSim.AselSimCore;

namespace AselAtcTrainingSim.AselApi.ApiObjects.Commands
{
    public class SpeedCommandRequest
    {
        public string Callsign { get; set; }
        public ConstraintType ConstraintType { get; set; }
        public int Speed { get; set; }
    }
}