using Grib.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft;

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

        public static GribTile FindOrCreateGribTile(AircraftPosition pos, DateTime dateTime)
        {
            GribTile foundTile = null;
            lock (GribTileListLock)
            {
                // Look for tile
                foundTile = null;
                foreach (GribTile tile in GribTileList)
                {
                    if (tile.IsAcftInside(pos) && tile.IsValid(dateTime))
                    {
                        foundTile = tile;
                        break;
                    }
                }

                // Create if not found
                if (foundTile == null)
                {
                    foundTile = new GribTile(pos.Latitude, pos.Longitude, dateTime);
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
        
        // Helper Properties
        private DateTime OffsetDateUtc => ForecastDateUtc.AddHours(-6);
        private short Cycle => (short)((OffsetDateUtc.Hour / 6) * 6);
        private short ForecastHour => (short)(OffsetDateUtc.Hour - Cycle + 6);
        private string GribDateString => OffsetDateUtc.ToString("yyyyMMdd");
        private string CycleString => Cycle.ToString("00");
        private string ForecastHourString => ForecastHour.ToString("000");
        private List<GribDataPoint> dataPoints;
        private readonly object GribDataListLock = new object();
        private bool Downloaded = false;

        public GribTile(double latitude, double longitude, DateTime dateTime)
        {
            lock (GribDataListLock)
            {
                dataPoints = new List<GribDataPoint>();
            }

            // Create Tile Bounds
            LeftLongitude = Math.Max(Convert.ToInt16(Math.Floor(longitude)), (short)-180);
            RightLongitude = Math.Min(Convert.ToInt16(Math.Ceiling(longitude)), (short)180);
            BottomLatitude = Math.Max(Convert.ToInt16(Math.Floor(latitude)), (short)-90);
            TopLatitude = Math.Min(Convert.ToInt16(Math.Ceiling(latitude)), (short)90);

            // Convert to UTC
            ForecastDateUtc = dateTime.ToUniversalTime();

            _ = DownloadTile();
        }

        private void ExtractData()
        {
            List<GribDataPoint> sfcValues = new List<GribDataPoint>();

            // Extract Data From Grib File
            using (GribFile file = new GribFile(GribFileName))
            {
                foreach (GribMessage msg in file)
                {
                    if (msg.TypeOfLevel.Equals("isobaricInhPa"))
                    {
                        foreach (GeoSpatialValue val in msg.GeoSpatialValues)
                        {
                            // Get GRID Point if it exists
                            GribDataPoint foundPoint = null;

                            lock (GribDataListLock)
                            {
                                foreach (GribDataPoint pt in dataPoints)
                                {
                                    if (pt.Latitude == val.Latitude && pt.Longitude == val.Longitude && pt.Level_hPa == msg.Level)
                                    {
                                        foundPoint = pt;
                                        break;
                                    }
                                }
                            }

                            // Otherwise, create new point
                            if (foundPoint == null)
                            {
                                foundPoint = new GribDataPoint(val.Latitude, val.Longitude, msg.Level);
                                lock (GribDataListLock)
                                {
                                    dataPoints.Add(foundPoint);
                                }
                            }

                            // Add data
                            switch (msg.ShortName)
                            {
                                case "u":
                                    foundPoint.U_mpers = val.Value;
                                    break;
                                case "v":
                                    foundPoint.V_mpers = val.Value;
                                    break;
                                case "t":
                                    foundPoint.Temp_K = val.Value;
                                    break;
                                case "gh":
                                    foundPoint.GeoPotentialHeight_M = val.Value;
                                    break;
                                case "rh":
                                    foundPoint.RelativeHumidity = val.Value;
                                    break;
                                default:
                                    Console.WriteLine($"{msg.ShortName}: {val.Value} {msg.Units}");
                                    break;
                            }
                        }
                    }
                    else if (msg.TypeOfLevel.Equals("meanSea"))
                    {
                        switch (msg.ShortName)
                        {
                            case "prmsl":
                                foreach (GeoSpatialValue val in msg.GeoSpatialValues)
                                {
                                    // Get GRID Point if it exists
                                    GribDataPoint foundPoint = null;

                                    foreach (GribDataPoint pt in sfcValues)
                                    {
                                        if (pt.Latitude == val.Latitude && pt.Longitude == val.Longitude && msg.Level == 0)
                                        {
                                            foundPoint = pt;
                                            break;
                                        }
                                    }

                                    // Otherwise, create new point
                                    if (foundPoint == null)
                                    {
                                        foundPoint = new GribDataPoint(val.Latitude, val.Longitude, msg.Level);
                                        sfcValues.Add(foundPoint);
                                    }

                                    foundPoint.SfcPress_hPa = val.Value / 100.0;
                                }
                                break;
                        }
                    }
                }
            }

            // Add Surface Pressures
            lock (GribDataListLock)
            {
                foreach (GribDataPoint point in dataPoints)
                {
                    foreach (GribDataPoint sfc in sfcValues)
                    {
                        if (sfc.Longitude == point.Longitude && sfc.Latitude == point.Latitude)
                        {
                            point.SfcPress_hPa = sfc.SfcPress_hPa;
                            break;
                        }
                    }
                }
            }
        }

        private async Task DownloadTile()
        {
            if (!Downloaded)
            {
                if (File.Exists(GribFileName))
                {
                    File.Delete(GribFileName);
                }

                using (WebClient wc = new WebClient())
                {
                    // Generate URL
                    string url = $"https://nomads.ncep.noaa.gov/cgi-bin/filter_gfs_0p25.pl?file=gfs.t{CycleString}z.pgrb2.0p25.f{ForecastHourString}&lev_1000_mb=on&lev_100_mb=on&lev_150_mb=on&lev_200_mb=on&lev_250_mb=on&lev_300_mb=on&lev_350_mb=on&lev_400_mb=on&lev_450_mb=on&lev_500_mb=on&lev_550_mb=on&lev_600_mb=on&lev_650_mb=on&lev_700_mb=on&lev_750_mb=on&lev_800_mb=on&lev_850_mb=on&lev_900_mb=on&lev_925_mb=on&lev_950_mb=on&lev_975_mb=on&lev_mean_sea_level=on&lev_surface=on&var_HGT=on&var_PRES=on&var_TMP=on&var_UGRD=on&var_VGRD=on&var_PRMSL=on&subregion=&leftlon={LeftLongitude}&rightlon={RightLongitude}&toplat={TopLatitude}&bottomlat={BottomLatitude}&dir=%2Fgfs.{GribDateString}%2F{CycleString}%2Fatmos";

                    // Download File
                    try
                    {
                        await wc.DownloadFileTaskAsync(new System.Uri(url), GribFileName);
                    }
                    catch (WebException we)
                    {
                        Console.WriteLine(we.Message + ": " + we.StackTrace.ToString());

                        // Set Downloaded Flag
                        Downloaded = false;
                        return;
                    }

                    // Extract Data
                    ExtractData();

                    // Set Downloaded Flag
                    Downloaded = true;
                }
            }
        }

        public GribDataPoint GetClosestPoint(AircraftPosition acftPos)
        {
            double minDist = -1;
            GribDataPoint pt = null;

            lock (GribDataListLock)
            {
                foreach (GribDataPoint point in dataPoints)
                {
                    double dist = point.GetDistanceFrom(acftPos);
                    if (pt == null || dist < minDist)
                    {
                        pt = point;
                        minDist = dist;
                    }
                }
            }

            return pt;
        }

        public bool IsValid(DateTime dateTime)
        {
            return Math.Abs((ForecastDateUtc - dateTime.ToUniversalTime()).TotalHours) < 1;
        }

        public bool IsAcftInside(AircraftPosition pos)
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
