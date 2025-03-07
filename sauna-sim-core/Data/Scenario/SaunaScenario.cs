using AviationCalcUtilNet.GeoTools;
using FsdConnectorNet;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SaunaSim.Core.Data.Scenario
{
    public class SaunaScenario
    {
        public List<SaunaScenarioAircraft> Aircraft { get; set; }
    }
    public class SaunaScenarioAircraft
    {
        public string Callsign { get; set; }
        public AircraftPos Pos { get; set; }
        public int Alt { get; set; }
        public string AcftType { get; set; }
        public string Squawk { get; set; }
        public FlightPlan Fp { get; set; }
    }
    public class AircraftPos
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }
}
