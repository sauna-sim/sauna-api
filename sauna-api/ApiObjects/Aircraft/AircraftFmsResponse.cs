using NavData_Interface.Objects.Fix;
using SaunaSim.Core.Data;
using System.Collections.Generic;
using System.Text;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using AviationCalcUtilNet.GeoTools;
using SaunaSim.Core.Simulator.Aircraft.FMS.NavDisplay;

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
        public List<object> FmsLines { get; set; }

        public double AlongTrackDistance_m { get; set; }

        public double CrossTrackDistance_m { get; set; }

        public double RequiredTrueCourse { get; set; }

        public double TurnRadius_m { get; set; }

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
            FmsLines = new List<object>();
            AlongTrackDistance_m = fms.AlongTrackDistance;
            CrossTrackDistance_m = fms.CrossTrackDistance;
            RequiredTrueCourse = fms.RequiredTrueCourse;
            TurnRadius_m = fms.TurnRadius;

            StringBuilder sb = new StringBuilder();
            if (fms.ActiveLeg != null)
            {
                sb.Append(fms.ActiveLeg.ToString());
                sb.Append("; ");
                foreach (NdLine line in fms.ActiveLeg.UiLines)
                {
                    FmsLines.Add(line);
                }
            }

            foreach (var leg in fms.GetRouteLegs())
            {
                sb.Append(leg.ToString());
                sb.Append("; ");
                foreach (NdLine line in leg.UiLines)
                {
                    FmsLines.Add(line);
                }
                RouteLegs.Add(leg);
            }
            AsString = sb.ToString();
        }
    }
}