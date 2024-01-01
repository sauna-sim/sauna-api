using AviationCalcUtilNet.GeoTools;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.Fixes.Navaids
{
    public class VhfNavaid : Navaid
    {
        public string AirportIdentifier { get; }
        public string VorIdentifier => Identifier;
        public string DmeIdent { get; }
        public GeoPoint DmeLocation { get; }
        public int DmeElevation { get; }
        public double IlsDmeBias { get; }

        public double StationDeclination { get; }

        public VhfNavaid(GeoPoint location, 
            string areaCode, 
            string airportIdentifier, 
            string icaoCode,
            string vorIdentifier,
            string name, 
            double frequency,
            string dmeIdent, 
            GeoPoint dmeLocation, 
            int dmeElevation, 
            double ilsDmeBias, 
            int range, 
            double stationDeclination) : base(areaCode, vorIdentifier, icaoCode, location, name, frequency, range)
        {
            AirportIdentifier = airportIdentifier;
            DmeIdent = dmeIdent;
            DmeLocation = dmeLocation;
            DmeElevation = dmeElevation;
            IlsDmeBias = ilsDmeBias;
            StationDeclination = stationDeclination;
        }
    }
}
