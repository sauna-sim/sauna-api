using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl;

namespace VatsimAtcTrainingSimulator.Core.GeoTools
{
    public class GribDataPoint
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double Longitude_Norm => Longitude > 180 ? Longitude - 360 : Longitude;
        public double GeoPotentialHeight_M { get; set; }
        public double GeoPotentialHeight_Ft => GeoPotentialHeight_M * 3.28084;
        public int Level_hPa { get; private set; }
        public double Temp_K { get; set; }
        public double Temp_C => Temp_K == 0 ? 56.5 : Temp_K - 273.15;
        public double V_mpers { get; set; }
        public double U_mpers { get; set; }
        public double WSpeed_mpers => Math.Sqrt(Math.Pow(U_mpers, 2) + Math.Pow(V_mpers, 2));
        public double WSpeed_kts => WSpeed_mpers * 1.943844;
        public double WDir_rads => Math.Atan2(V_mpers, U_mpers);
        public double WDir_deg => (WDir_rads * 180.0) / Math.PI;
        public double RelativeHumidity { get; set; }
        public double SfcPress_hPa { get; set; }

        public GribDataPoint(double lat, double lon, int level_hPa)
        {
            Latitude = lat;
            Longitude = lon;
            Level_hPa = level_hPa;
        }

        public double GetDistanceFrom(AcftData pos)
        {
            return Math.Sqrt(Math.Pow(pos.Latitude - Latitude, 2) + Math.Pow(pos.Longitude - Longitude_Norm, 2) + Math.Pow(pos.DensityAltitude - GeoPotentialHeight_Ft, 2));
        }

        public override string ToString()
        {
            return $"Lat: {Latitude} Lon: {Longitude_Norm} Level: {Level_hPa}hPa Height: {GeoPotentialHeight_Ft}ft Temp: {Temp_C}C Wind: {WDir_deg}@{WSpeed_kts}KT RH: {RelativeHumidity}";
        }
    }
}
