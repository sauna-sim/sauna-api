using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Units;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects
{
    public enum IlsCategory
    {
        LocalizerOnly,
        CATI,
        CATII,
        CATIII,
        IGS,
        LDA,
        SDF
    }

    public class Localizer
    {
        public string Area_code { get; }

        public string Icao_code { get; }

        public string Airport_identifier { get; }

        public string Runway_identifier { get; }

        public string Loc_identifier { get; }

        public GeoPoint Loc_location { get; }

        public double Loc_frequency { get; }

        public Bearing Loc_bearing { get; }

        public Angle Loc_width { get; }

        public IlsCategory Loc_category { get; }

        public Glideslope Glideslope { get; }

        public double Station_declination { get; }

        public Localizer(
            string area_code,
            string icao_code,
            string airport_identifier,
            string runway_identifier,
            string loc_identifier,
            GeoPoint loc_location,
            double loc_frequency,
            Bearing loc_bearing,
            Angle loc_width,
            IlsCategory loc_category,
            double station_declination) :
            this(
                area_code,
                icao_code, airport_identifier,
                runway_identifier,
                loc_identifier,
                loc_location,
                loc_frequency,
                loc_bearing,
                loc_width,
                loc_category,
                null,
                station_declination)
        { }

        public Localizer(string area_code,
            string icao_code,
            string airport_identifier,
            string runway_identifier,
            string loc_identifier,
            GeoPoint loc_location,
            double loc_frequency,
            Bearing loc_bearing,
            Angle loc_width,
            IlsCategory loc_category,
            Glideslope glideslope,
            double station_declination) 
        { 
            Area_code = area_code;
            Icao_code = icao_code;
            Airport_identifier = airport_identifier;
            Runway_identifier = runway_identifier;
            Loc_identifier = loc_identifier;
            Loc_location = loc_location;
            Loc_frequency = loc_frequency;
            Loc_bearing = loc_bearing;
            Loc_width = loc_width;

            Station_declination = station_declination;

            // Handle invalid loc_category/glideslope pairings
            if (loc_category == IlsCategory.LocalizerOnly)
            { // if we are localizer only, there is no glideslope, no matter what was passed
                Glideslope = null;
            } else
            { // if we are CATI/II/III, but no glideslope was passed, our category should be LocalizerOnly instead
                if (glideslope == null)
                {
                    if (loc_category == IlsCategory.CATI || loc_category == IlsCategory.CATII || loc_category == IlsCategory.CATIII)
                    {
                        Loc_category = IlsCategory.LocalizerOnly;
                    }
                    if (loc_category == IlsCategory.IGS)
                    {
                        Loc_category = IlsCategory.LDA;
                    }
                }
                else
                { // the pair is valid. just set it.
                    Loc_category = loc_category;
                    Glideslope = glideslope;
                }
            }
        }

        public override string ToString()
        {
            string glideslope_string;

            if (Glideslope != null)
            {
                glideslope_string = Glideslope.ToString();
            } else
            {
                glideslope_string = "None";
            }

            return $"Localizer: {this.Airport_identifier} {this.Runway_identifier} localizer is {this.Loc_identifier}.\n" +
                $"Located at ({this.Loc_location.Lat}, {this.Loc_location.Lon}).\n" +
                $"Frequency: {this.Loc_frequency} Bearing: {this.Loc_bearing} Width: {this.Loc_width}\n" +
                $"Category: {this.Loc_category}\n" +
                $"Glideslope: {glideslope_string}\n" +
                $"Station declination: {this.Station_declination}";
        }
    }
}
