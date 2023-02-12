using AselAtcTrainingSim.AselSimCore;
using AselAtcTrainingSim.AselSimCore.Simulator.Aircraft;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AselAtcTrainingSim.AselApi.RestObjects
{
    public class AircraftResponse
    {
        public string Callsign { get; set; }
        public bool ShouldSpawn { get; set; }
        public int DelayMs { get; set; }
        public bool Paused { get; set; }
        public string FlightPlan { get; set; }
        public XpdrMode XpdrMode { get; set; }
        public int Squawk { get; set; }
        public int Rating { get; set; }
        public AircraftPosition Position { get; set; }
        public bool OnGround { get; set; }
        public int Assigned_IAS { get; set; }
        public ConstraintType Assigned_IAS_Type { get; set; }
        public CONN_STATUS ConnectionStatus { get; set; }

        public AircraftResponse()
        {

        }

        public AircraftResponse(VatsimClientPilot pilot)
        {
            Callsign = pilot.Callsign;
            ShouldSpawn = pilot.ShouldSpawn;
            DelayMs = pilot.DelayMs;
            Paused = pilot.Paused;
            FlightPlan = pilot.FlightPlan;
            XpdrMode = pilot.XpdrMode;
            Squawk = pilot.Squawk;
            Rating = pilot.Rating;
            Position = pilot.Position;
            OnGround = pilot.OnGround;
            Assigned_IAS = pilot.Assigned_IAS;
            Assigned_IAS_Type = pilot.Assigned_IAS_Type;
            ConnectionStatus = pilot.ConnectionStatus;
        }
    }
}