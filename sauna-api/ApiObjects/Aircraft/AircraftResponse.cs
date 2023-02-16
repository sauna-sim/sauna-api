using SaunaSim.Core;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Aircraft.Control;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS.Legs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace SaunaSim.Api.ApiObjects.Aircraft
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
        public AircraftControlResponse Control { get; set; }
        public bool OnGround { get; set; }
        public int Assigned_IAS { get; set; }
        public ConstraintType Assigned_IAS_Type { get; set; }
        public CONN_STATUS ConnectionStatus { get; set; }

        public AircraftResponse()
        {

        }

        public AircraftResponse(VatsimClientPilot pilot, bool includeFms = false)
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
            Control = new AircraftControlResponse(pilot.Control, includeFms);
            OnGround = pilot.OnGround;
            Assigned_IAS = pilot.Assigned_IAS;
            Assigned_IAS_Type = pilot.Assigned_IAS_Type;
            ConnectionStatus = pilot.ConnectionStatus;
        }

        public class AircraftControlResponse
        {
            public object CurrentLateralMode { get; set; }
            public object ArmedLateralMode { get; set; }
            public object CurrentVerticalMode { get; set; }
            public List<object> ArmedVerticalModes { get; set; }
            public AircraftFmsResponse FMS { get; set; }

            public AircraftControlResponse()
            {

            }

            public AircraftControlResponse(AircraftControl control, bool includeFms = false)
            {
                CurrentLateralMode = control.CurrentLateralInstruction;
                ArmedLateralMode = control.ArmedLateralInstruction;
                CurrentVerticalMode = control.CurrentVerticalInstruction;
                ArmedVerticalModes = new List<object>();
                foreach (var instr in control.ArmedVerticalInstructions)
                {
                    ArmedVerticalModes.Add(instr);
                }
                if (includeFms)
                {
                    FMS = new AircraftFmsResponse(control.FMS);
                }
            }
        }
    }
}