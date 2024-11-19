using AviationCalcUtilNet.Units;
using NavData_Interface.Objects.LegCollections.Procedures;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace NavData_Interface.DataSources.DFDUtility.Factory
{
    internal class SidFactory : TerminalProcedureFactory
    {
        public static Sid Factory(SQLiteDataReader reader, SQLiteConnection connection)
        {
            var factory = new SidFactory(reader, connection);

            var data = factory.GatherData();
            return new Sid(data.airportIdentifier, data.routeIdentifier, data.firstTransitions, data.commonLegs, data.secondTransitions, data.transitionAltitude);
        }

        private SidFactory(SQLiteDataReader reader, SQLiteConnection connection) : base(reader, connection) { }
    }
}
