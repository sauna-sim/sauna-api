using NavData_Interface.Objects.Fixes.Navaids;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace NavData_Interface.DataSources.DFDUtility.Factory
{
    internal class VhfNavaidFactory
    {
        internal static VhfNavaid Factory(SQLiteDataReader reader)
        {
            // these may be null. In that case, set to 0.
            var ilsdme_bias_string = reader["ilsdme_bias"].ToString();
            var ilsdme_bias = ilsdme_bias_string == "" ? 0 : Double.Parse(ilsdme_bias_string);

            var station_declination_string = reader["station_declination"].ToString();
            var station_declination = station_declination_string == "" ? 0 : Double.Parse(station_declination_string);

            var navaid = new VhfNavaid
                (
                SQLHelper.locationFromColumns(reader, "vor_latitude", "vor_longitude"),
                reader["area_code"].ToString(),
                reader["airport_identifier"].ToString(),
                reader["icao_code"].ToString(),
                reader["vor_identifier"].ToString(),
                reader["vor_name"].ToString(),
                Double.Parse(reader["vor_frequency"].ToString()),
                reader["dme_ident"].ToString(),
                SQLHelper.locationFromColumns(reader, "dme_latitude", "dme_longitude"),
                Int32.Parse(reader["dme_elevation"].ToString()),
                ilsdme_bias,
                Int32.Parse(reader["range"].ToString()),
                station_declination
                );

            return navaid;
        }
    }
}
