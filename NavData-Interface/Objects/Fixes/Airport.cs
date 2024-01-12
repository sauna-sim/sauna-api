using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Units;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.Fixes
{
    public enum RunwaySurfaceCode
    {
        Undefined,
        Hard,
        Soft,
        Water
    }

    public class Airport : Fix
    {
        public string Area_code { get; }

        public string Icao_code { get; }

        // This is the three-letter identifier used for some US and Canada airports.
        public string Three_letter_id { get; }
        
        public bool IsIfr { get; }

        public RunwaySurfaceCode Longest_runway_surface { get; }

        public Length Elevation { get; }

        public Length Transition_altitude { get; }

        public Length Transition_level { get; }

        public Velocity Speed_limit { get; }

        public Length Speed_limit_altitude { get; }

        public string Iata_ata_designator { get; }

        public Airport(
            string identifier,
            GeoPoint location,
            string area_code,
            string icao_code,
            string three_letter_id,
            string name,
            bool isIfr,
            RunwaySurfaceCode longest_runway_surface,
            Length elevation,
            Length transition_altitude,
            Length transition_level,
            Velocity speed_limit,
            Length speed_limit_altitude,
            string iata_ata_designator
            ) : base(identifier, name, location)
        {
            Area_code = area_code;
            Icao_code = icao_code;
            Three_letter_id = three_letter_id;
            IsIfr = isIfr;
            Longest_runway_surface = longest_runway_surface;
            Elevation = elevation;
            Transition_altitude = transition_altitude;
            Transition_level = transition_level;
            Speed_limit = speed_limit;
            Speed_limit_altitude = speed_limit_altitude;
            Iata_ata_designator = iata_ata_designator;
        }
    }
}
