using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Data
{
    public class AppSettings
    {
        public double CommandFrequency { get; set; } = 199.998;
        public int UpdateRate { get; set; } = 5000;
        public bool SendIas { get; set; } = false;
        public int PosCalcRate { get; set; } = 1000;
    }
    public static class AppSettingsManager
    {
        private static AppSettings _settings = new AppSettings();

        public static AppSettings Settings { get => _settings; set => _settings = value; }        
        public static double CommandFrequency { get => _settings.CommandFrequency; set => _settings.CommandFrequency = value; }
        public static int UpdateRate { get => _settings.UpdateRate; set => _settings.UpdateRate = value; }
        public static bool SendIas { get => _settings.SendIas; set => _settings.SendIas = value; }
        public static int PosCalcRate { get => _settings.PosCalcRate; set => _settings.PosCalcRate = value; }
    }
}
