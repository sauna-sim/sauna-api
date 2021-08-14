using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.GeoTools.Helpers;
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
        public double WDir_rads => Math.Atan2(-U_mpers, -V_mpers);
        public double WDir_deg => AcftGeoUtil.NormalizeHeading(AcftGeoUtil.RadiansToDegrees(WDir_rads));
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
            double absHeightM = pos.AbsoluteAltitude / AcftGeoUtil.CONV_FACTOR_M_FT;
            double geoPotHeightM = AcftGeoUtil.EARTH_RADIUS_M * absHeightM / (AcftGeoUtil.EARTH_RADIUS_M + absHeightM);
            double flatDistNMi = GeoPoint.FlatDistanceNMi(new GeoPoint(pos.Latitude, pos.Longitude), new GeoPoint(Latitude, Longitude_Norm));
            double altDistNMi = Math.Abs(geoPotHeightM - GeoPotentialHeight_M) / AcftGeoUtil.CONV_FACTOR_NMI_M;
            return Math.Sqrt(Math.Pow(flatDistNMi, 2) + Math.Pow(altDistNMi, 2));
        }

        public override string ToString()
        {
            return $"Lat: {Latitude} Lon: {Longitude_Norm} Level: {Level_hPa}hPa Height: {GeoPotentialHeight_Ft}ft Temp: {Temp_C}C Wind: {WDir_deg}@{WSpeed_kts}KT RH: {RelativeHumidity}";
        }
    }
}
