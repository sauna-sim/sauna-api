using System;
using System.Collections.Generic;
using System.Text;

namespace AselAtcTrainingSim.AselSimCore.Data
{
    public class AppSettings
    {
        public double CommandFrequency { get; set; } = 199.998;
        public int Protocol { get; set; } = 9;
        public string Server { get; set; } = "";
        public string Cid { get; set; } = "";
        public string Password { get; set; } = "";
        public bool VatsimServer { get; set; } = false;
        public int Port { get; set; } = 6809;
        public int UpdateRate { get; set; } = 5000;
        public bool SendIas { get; set; } = false;
        public int PosCalcRate { get; set; } = 1000;
    }
    public static class AppSettingsManager
    {
        private static AppSettings _settings = new AppSettings();

        public static AppSettings Settings => _settings;
        
        public static double CommandFrequency { get => _settings.CommandFrequency; set => _settings.CommandFrequency = value; }
        public static int Protocol { get => _settings.Protocol; set => _settings.Protocol = value; }
        public static string Server { get => _settings.Server; set => _settings.Server = value; }
        public static string Cid { get => _settings.Cid; set => _settings.Cid = value; }
        public static string Password { get => _settings.Password; set => _settings.Password = value; }
        public static bool VatsimServer { get => _settings.VatsimServer; set => _settings.VatsimServer = value; }
        public static int Port { get => _settings.Port; set => _settings.Port = value; }
        public static int UpdateRate { get => _settings.UpdateRate; set => _settings.UpdateRate = value; }
        public static bool SendIas { get => _settings.SendIas; set => _settings.SendIas = value; }
        public static int PosCalcRate { get => _settings.PosCalcRate; set => _settings.PosCalcRate = value; }
    }
}
