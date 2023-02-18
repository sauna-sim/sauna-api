using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Data
{
    public class AppSettings
    {
        // TODO: Change to whatever format you want @Caspian
        public double CommandFrequency { get; set; } = 199.998;
        // TODO: Remove Update Rate
        public int UpdateRate { get; set; } = 5000;
        public int PosCalcRate { get; set; } = 1000;
    }
    public static class AppSettingsManager
    {
        private static AppSettings _settings = new AppSettings();

        public static AppSettings Settings { get => _settings; set => _settings = value; }
        // TODO: Change to whatever format you want @Caspian
        public static double CommandFrequency { get => _settings.CommandFrequency; set => _settings.CommandFrequency = value; }
        // TODO: Remove Update Rate
        public static int UpdateRate { get => _settings.UpdateRate; set => _settings.UpdateRate = value; }
        public static int PosCalcRate { get => _settings.PosCalcRate; set => _settings.PosCalcRate = value; }
    }
}
