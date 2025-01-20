using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Units;
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

        public VhfNavaid(GeoPoint location, 
            string areaCode, 
            string airportIdentifier, 
            string icaoCode,
            string vorIdentifier,
            string name, 
            double frequency,
            string dmeIdent, 
            GeoPoint dmeLocation, 
            Length range) : base(areaCode, vorIdentifier, icaoCode, location, name, frequency, range)
        {
            AirportIdentifier = airportIdentifier;
            DmeIdent = dmeIdent;
            DmeLocation = dmeLocation;
        }
    }
}
