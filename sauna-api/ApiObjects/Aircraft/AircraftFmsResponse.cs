using NavData_Interface.Objects.Fix;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS.Legs;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Api.ApiObjects.Aircraft
{
    public class AircraftFmsResponse
    {
        public bool Suspended { get; set; }
        public int CruiseAltitude { get; set; }
        public Fix DepartureAirport { get; set; }
        public Fix ArrivalAirport { get; set; }
        public object ActiveLeg { get; set; }
        public List<object> RouteLegs { get; set; }
        public string AsString { get; set; }

        public AircraftFmsResponse()
        {

        }

        public AircraftFmsResponse(AircraftFms fms)
        {
            Suspended = fms.Suspended;
            CruiseAltitude = fms.CruiseAltitude;
            DepartureAirport = fms.DepartureAirport;
            ArrivalAirport = fms.ArrivalAirport;
            ActiveLeg = fms.ActiveLeg;
            RouteLegs = new List<object>();
            StringBuilder sb = new StringBuilder();
            foreach (var leg in fms.GetRouteLegs())
            {
                sb.Append(leg.ToString());
                sb.Append("; ");
                RouteLegs.Add(leg);
            }
            AsString = sb.ToString();
        }
    }
}