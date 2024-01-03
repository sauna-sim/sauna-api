using NavData_Interface.Objects.LegCollections.Airways;
using NavData_Interface.Objects.LegCollections.Legs;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace NavData_Interface.DataSources.DFDUtility.Factory
{
    public class AirwayFactory
    {
        internal static Airway Factory(SQLiteDataReader reader)
        {
            List<Leg> legs = new List<Leg>();

            reader.Read();


        }
    }
}
