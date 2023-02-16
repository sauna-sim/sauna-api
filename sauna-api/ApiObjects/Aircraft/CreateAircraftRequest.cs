using SaunaSim.Core;
using System.Collections.Generic;

namespace SaunaSim.Api.ApiObjects.Aircraft
{
    public class CreateAircraftRequest
    {
        public string Cid { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public bool VatsimServer { get; set; }
        public string Protocol { get; set; }

        public string FullName { get; set; } = "Simulator Pilot";
        public int PilotRating { get; set; }

        public string Callsign { get; set; }
        public string FlightPlan { get; set; }

        public List<FmsWaypointRequest> FmsWaypointList { get; set; }

        public XpdrMode TransponderMode { get; set; }
        public int Squawk { get; set; }

        public AircraftPositionRequest Position { get; set; }

        public bool Paused { get; set; } = true;
    }
}