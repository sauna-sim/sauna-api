using System;
using System.Collections.Generic;
using System.Text;

namespace AselAtcTrainingSim.AselSimCore.Data
{
    public static class AppSettings
    {
        public static double CommandFrequency { get; set; } = 199.998;
        public static int Protocol { get; set; } = 9;
        public static string Server { get; set; } = "";
        public static string Cid { get; set; } = "";
        public static string Password { get; set; } = "";
        public static bool VatsimServer { get; set; } = false;
        public static int Port { get; set; } = 6809;
        public static int UpdateRate { get; set; } = 5000;
        public static bool SendIas { get; set; } = false;
        public static int PosCalcRate { get; set; } = 1000;
    }
}
