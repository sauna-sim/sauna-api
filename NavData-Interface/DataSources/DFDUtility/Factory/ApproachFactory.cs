using System;
using Microsoft.Data.Sqlite;
using NavData_Interface.Objects.LegCollections.Procedures;

namespace NavData_Interface.DataSources.DFDUtility.Factory
{
    internal class ApproachFactory : TerminalProcedureFactory
    {
        private ApproachFactory(SqliteDataReader reader, SqliteConnection connection) : base(reader, connection)
        {
        }
        
        public static Approach Factory(SqliteDataReader reader, SqliteConnection connection)
        {
            var factory = new ApproachFactory(reader, connection);
            try
            {
                var data = factory.GatherData();
                return new Approach(data.airportIdentifier, data.routeIdentifier, data.commonLegs, data.firstTransitions, data.transitionAltitude);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}