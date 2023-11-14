using NavData_Interface.Objects.Fix;
using SaunaSim.Core.Data;
using System.Collections.Generic;
using System.Text;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using AviationCalcUtilNet.GeoTools;

namespace SaunaSim.Api.ApiObjects.Aircraft
{
    public class FmsLine
    {
        public double StartLat { get; set; }
        public double StartLon { get; set; }
        public double EndLat { get; set; }
        public double EndLon { get; set; }
    }

    public class AircraftFmsResponse
    {
        public bool Suspended { get; set; }
        public int CruiseAltitude { get; set; }
        public Fix DepartureAirport { get; set; }
        public Fix ArrivalAirport { get; set; }
        public object ActiveLeg { get; set; }
        public List<object> RouteLegs { get; set; }
        public string AsString { get; set; }
        public List<FmsLine> FmsLines { get; set; }

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
            FmsLines = new List<FmsLine>();

            StringBuilder sb = new StringBuilder();
            if (fms.ActiveLeg != null)
            {
                sb.Append(fms.ActiveLeg.ToString());
                sb.Append("; ");
                foreach ((GeoPoint start, GeoPoint end) in fms.ActiveLeg.UiLines)
                {
                    FmsLines.Add(new FmsLine()
                    {
                        StartLat = start.Lat,
                        StartLon = start.Lon,
                        EndLat = end.Lat,
                        EndLon = end.Lon
                    });
                }
            }

            foreach (var leg in fms.GetRouteLegs())
            {
                sb.Append(leg.ToString());
                sb.Append("; ");
                foreach ((GeoPoint start, GeoPoint end) in leg.UiLines)
                {
                    FmsLines.Add(new FmsLine()
                    {
                        StartLat = start.Lat,
                        StartLon = start.Lon,
                        EndLat = end.Lat,
                        EndLon = end.Lon
                    });
                }
                RouteLegs.Add(leg);
            }
            AsString = sb.ToString();
        }
    }
}