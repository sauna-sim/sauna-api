using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS.Legs;
using System.Collections.Generic;

namespace SaunaSim.Api.ApiObjects.Aircraft
{
    public class AircraftFmsResponse
    {
        public bool Suspended { get; set; }
        public int CruiseAltitude { get; set; }
        public Waypoint DepartureAirport { get; set; }
        public Waypoint ArrivalAirport { get; set; }
        public object ActiveLeg { get; set; }
        public List<object> RouteLegs { get; set; }

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
            foreach (var leg in fms.GetRouteLegs())
            {
                RouteLegs.Add(leg);
            }
        }
    }
}