using CoordinateSharp;
using Grib.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core
{
    public class AcftPosition
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
    }

    public static class GeoTools
    {
        public static AcftPosition CalculateNextLatLon(AcftPosition pos, double trueBearingDeg, double gsKts, double vsFtMin, double timeMs)
        {
            Coordinate start = new Coordinate(pos.Latitude, pos.Longitude);
            double distanceNMi = (timeMs / 1000.0) * (gsKts / 60.0 / 60.0);
            double distanceM = distanceNMi * 1852;
            start.Move(distanceM, trueBearingDeg, Shape.Ellipsoid);
            pos.Latitude = start.Latitude.ToDouble();
            pos.Longitude = start.Longitude.ToDouble();
            pos.Altitude = pos.Altitude + ((timeMs / 1000.0) * (vsFtMin / 60.0));

            return pos;
        }

        public async static Task GetWindsAloft(string callsign, AcftPosition pos)
        {
            using (WebClient wc = new WebClient())
            {
                // Create 0.5deg x 0.5deg square.
                double topLat = Math.Min(pos.Latitude + 0.25, 90);
                double botLat = Math.Max(pos.Latitude - 0.25, -90);
                double leftLon = Math.Max(pos.Longitude - 0.25, -180);
                double rightLon = Math.Min(pos.Longitude + 0.25, 180);

                // Calculate cycle and offset
                DateTime now = DateTime.UtcNow;
                now = now.AddHours(-6);

                string datenow = now.ToString("yyyyMMdd");

                string cycle = "00";
                string offset = "000";

                if (now.Hour < 6)
                {
                    cycle = "00";
                    offset = (now.Hour + 6).ToString("000");
                } else if (now.Hour < 12)
                {
                    cycle = "06";
                    offset = (now.Hour).ToString("000");
                } else if (now.Hour < 18)
                {
                    cycle = "12";
                    offset = (now.Hour - 6).ToString("000");
                } else
                {
                    cycle = "18";
                    offset = (now.Hour - 12).ToString("000");
                }


                string filename = $"{callsign}_t{cycle}z_f{offset}.grb";
                string url = $"https://nomads.ncep.noaa.gov/cgi-bin/filter_gfs_0p25.pl?file=gfs.t{cycle}z.pgrb2.0p25.f{offset}&lev_1000_mb=on&lev_100_mb=on&lev_150_mb=on&lev_200_mb=on&lev_250_mb=on&lev_300_mb=on&lev_350_mb=on&lev_400_mb=on&lev_450_mb=on&lev_500_mb=on&lev_550_mb=on&lev_600_mb=on&lev_650_mb=on&lev_700_mb=on&lev_750_mb=on&lev_800_mb=on&lev_850_mb=on&lev_900_mb=on&lev_925_mb=on&lev_950_mb=on&lev_975_mb=on&lev_mean_sea_level=on&lev_surface=on&var_HGT=on&var_MSLET=on&var_PRES=on&var_PRMSL=on&var_TMP=on&var_UFLX=on&var_UGRD=on&var_VFLX=on&var_VGRD=on&subregion=&leftlon={leftLon}&rightlon={rightLon}&toplat={topLat}&bottomlat={botLat}&dir=%2Fgfs.{datenow}%2F{cycle}%2Fatmos";

                try
                {
                    await wc.DownloadFileTaskAsync(
                        // Param1 = Link of file
                        new System.Uri(url),
                        // Param2 = Path to save
                        filename
                    );

                    //using (GribFile file = new GribFile(filename))
                    //{
                    //    foreach (GribMessage msg in file)
                    //    {
                    //        foreach (GribValue key in msg)
                    //        {
                    //            Console.WriteLine("Key: {0}, Value: {1}", key.Key, key.AsString());
                    //        }
                    //    }
                    //}
                } catch (Exception) { }
            }
        }
    }
}
