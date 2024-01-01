using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.LegCollections.Airways
{

    public enum AirwayType
    {
        AirlineSpecific,
        DirectRoute,
        Helicopter,
        OfficialNonRnav,
        Rnav,
        UndesignatedRoute
    }

    public enum AirwayLevel
    {
        High,
        Low,
        Both
    }

    public enum AirwayDirection
    {
        Forward,
        Backward,
        Both
    }

    public class Airway : LegCollection
    {
        // public LegSet Legs

        public AirwayLevel Level { get; }

        public AirwayDirection Direction { get; }

        // private string _cruiseLevelTable;
        // public CruiseLevelTable ApplicableCruiseLevelTable(DataSource source) => source.GetCruiseLevelTable(_cruiseLevelTable);


        public Airway(string areaCode, string identifier, string icaoCode, AirwayLevel level, AirwayDirection direction) : base(areaCode, identifier, icaoCode)
        {
            Level = level;
            Direction = direction;
        }
    }
}
