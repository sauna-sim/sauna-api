using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.GeoTools
{
    public class GribTile
    {
        // Static Helpers
        private static List<GribTile> GribTileList { get; set; }
        private static readonly object GribTileListLock = new object();

        static GribTile()
        {
            GribTileList = new List<GribTile>();
        }

        public static GribTile FindOrCreateGribTile(AcftPosition pos, DateTime dateTime)
        {
            DateTime now = DateTime.UtcNow;

            GribTile foundTile = null;
            lock (GribTileListLock)
            {
                // Look for tile
                foundTile = GribTileList.Find(t => t.IsAcftInside(pos) && t.IsValid(now));
            }

            // Create if not found
            if (foundTile == null)
            {
                foundTile = new GribTile(pos.Latitude, pos.Longitude, now);

                lock (GribTileListLock)
                {
                    GribTileList.Add(foundTile);
                }
            }

            return foundTile;
        }

        // Instance Properties
        public short TopLatitude { get; private set; }
        public short BottomLatitude { get; private set; }
        public short LeftLongitude { get; private set; }
        public short RightLongitude { get; private set; }
        public DateTime ForecastDateUtc { get; private set; }
        public string GribFileName => $"GribTile_{GribDateString}_t{CycleString}z_f{ForecastHourString}_l{LeftLongitude}_t{TopLatitude}_r{RightLongitude}_b{BottomLatitude}.grb";
        public bool Downloaded { get; private set; }

        // Helper Properties
        private DateTime OffsetDateUtc => ForecastDateUtc.AddHours(-6);
        private short Cycle => (short)((OffsetDateUtc.Hour / 6) * 6);
        private short ForecastHour => (short)(OffsetDateUtc.Hour - Cycle + 6);
        private string GribDateString => OffsetDateUtc.ToString("yyyyMMdd");
        private string CycleString => Cycle.ToString("00");
        private string ForecastHourString => ForecastHour.ToString("000");
        private bool Downloading { get; set; }
        private TaskCompletionSource<bool> WaitForDownload { get; set; }

        public GribTile(double latitude, double longitude, DateTime dateTime)
        {
            Downloaded = false;
            Downloading = false;

            // Create Tile Bounds
            LeftLongitude = Math.Max(Convert.ToInt16(longitude), (short)-180);
            RightLongitude = Math.Min(Convert.ToInt16(longitude + 1), (short)180);
            BottomLatitude = Math.Max(Convert.ToInt16(latitude), (short)-90);
            TopLatitude = Math.Min(Convert.ToInt16(latitude + 1), (short)90);

            // Convert to UTC
            ForecastDateUtc = dateTime.ToUniversalTime();
        }

        public async Task DownloadTile()
        {
            if (!Downloaded)
            {
                // If another thread is already downloading, just wait for that to finish
                if (Downloading)
                {
                    await WaitForDownload?.Task;
                    return;
                }

                Downloading = true;
                WaitForDownload = new TaskCompletionSource<bool>();
                using (WebClient wc = new WebClient())
                {
                    // Generate URL
                    string url = $"https://nomads.ncep.noaa.gov/cgi-bin/filter_gfs_0p25.pl?file=gfs.t{CycleString}z.pgrb2.0p25.f{ForecastHourString}&lev_1000_mb=on&lev_100_mb=on&lev_150_mb=on&lev_200_mb=on&lev_250_mb=on&lev_300_mb=on&lev_350_mb=on&lev_400_mb=on&lev_450_mb=on&lev_500_mb=on&lev_550_mb=on&lev_600_mb=on&lev_650_mb=on&lev_700_mb=on&lev_750_mb=on&lev_800_mb=on&lev_850_mb=on&lev_900_mb=on&lev_925_mb=on&lev_950_mb=on&lev_975_mb=on&lev_mean_sea_level=on&lev_surface=on&var_HGT=on&var_MSLET=on&var_PRES=on&var_PRMSL=on&var_TMP=on&var_UFLX=on&var_UGRD=on&var_VFLX=on&var_VGRD=on&subregion=&leftlon={LeftLongitude}&rightlon={RightLongitude}&toplat={TopLatitude}&bottomlat={BottomLatitude}&dir=%2Fgfs.{GribDateString}%2F{CycleString}%2Fatmos";

                    // Download File
                    try
                    {
                        await wc.DownloadFileTaskAsync(new System.Uri(url), GribFileName);
                    }
                    catch (WebException)
                    {
                        // Set Downloaded Flag
                        Downloaded = false;
                        Downloading = false;
                        WaitForDownload?.TrySetResult(true);
                        return;
                    }

                    // Set Downloaded Flag
                    Downloaded = true;
                    Downloading = false;
                    WaitForDownload?.TrySetResult(true);
                }
            }
        }

        public bool IsValid(DateTime dateTime)
        {
            return (ForecastDateUtc - dateTime.ToUniversalTime()).TotalHours == 0;
        }

        public bool IsAcftInside(AcftPosition pos)
        {
            return pos.Latitude >= BottomLatitude && pos.Latitude <= TopLatitude
                && pos.Longitude >= LeftLongitude && pos.Longitude <= RightLongitude;
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            GribTile o = (GribTile)obj;

            return LeftLongitude == o.LeftLongitude && RightLongitude == o.RightLongitude
                && BottomLatitude == o.BottomLatitude && TopLatitude == o.TopLatitude
                && IsValid(o.ForecastDateUtc);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            // TODO: write your implementation of GetHashCode() here
            return base.GetHashCode();
        }

        ~GribTile()
        {
            WaitForDownload?.TrySetResult(true);
            if (Downloaded)
            {
                try
                {
                    File.Delete(GribFileName);
                }
                catch (Exception)
                {
                    return;
                }
            }
        }
    }
}
