using SaunaSim.Core;
using SaunaSim.Core.Simulator.Aircraft;

namespace SaunaSim.Api.ApiObjects.Commands
{
    public class SpeedCommandRequest
    {
        public string Callsign { get; set; }
        public ConstraintType ConstraintType { get; set; }
        public int Speed { get; set; }
    }
}