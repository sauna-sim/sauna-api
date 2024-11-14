using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Data
{
    public class AppSettings
    {
        public (ushort, ushort) CommandFrequency { get; set; } = (199, 998);

        public int PosCalcRate => AppSettingsManager.PosCalcRate;
    }
    public static class AppSettingsManager
    {
        private static int _posCalcRate = 100;

        public static AppSettings Settings { get; set; } = new AppSettings();
        public static (ushort, ushort) CommandFrequency { get => Settings.CommandFrequency; set => Settings.CommandFrequency = value; }
        public static int PosCalcRate => _posCalcRate;
    }
}
