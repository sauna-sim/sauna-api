using NavData_Interface.Objects.LegCollections.Procedures;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace NavData_Interface.DataSources.DFDUtility.Factory
{
    internal class StarFactory : TerminalProcedureFactory
    {
        public static Star Factory(SQLiteDataReader reader, SQLiteConnection connection)
        {
            var factory = new StarFactory(reader, connection);

            var data = factory.GatherData();
            return new Star(data.airportIdentifier, data.routeIdentifier, data.firstTransitions, data.commonLegs, data.secondTransitions, data.transitionAltitude);
        }

        private StarFactory(SQLiteDataReader reader, SQLiteConnection connection) : base(reader, connection) { }
    }
}
