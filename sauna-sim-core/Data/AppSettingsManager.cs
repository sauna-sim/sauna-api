using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Data
{
    public class AppSettings
    {
        public (ushort, ushort) CommandFrequency { get; set; } = (199, 998);
        public int PosCalcRate { get; set; } = 1000;
    }
    public static class AppSettingsManager
    {
        private static AppSettings _settings = new AppSettings();

        public static AppSettings Settings { get => _settings; set => _settings = value; }
        public static (ushort, ushort) CommandFrequency { get => _settings.CommandFrequency; set => _settings.CommandFrequency = value; }
        public static int PosCalcRate { get => _settings.PosCalcRate; set => _settings.PosCalcRate = value; }
    }
}
