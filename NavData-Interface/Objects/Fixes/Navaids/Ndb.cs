using AviationCalcUtilNet.GeoTools;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.Fixes.Navaids
{
    public class Ndb : Navaid
    {
        public bool IsTerminal { get; }

        public string Region_code { get; }

        public Ndb(
            string identifier, 
            GeoPoint location,
            string areaCode, 
            string icaoCode, 
            string name, 
            double frequency,
            int range) :
            this(identifier, location, areaCode, icaoCode, name, frequency, range, "") { }

        public Ndb(string identifier,
            GeoPoint location,
            string areaCode,
            string icaoCode,
            string name,
            double frequency,
            int range,
            string region_code) :
            base(areaCode, identifier, icaoCode, location, name, frequency, range)
        {
            if (region_code == "")
            {
                IsTerminal = false;
            } else
            {
                IsTerminal = true;
                Region_code = region_code;
            }
        }
    }
}
