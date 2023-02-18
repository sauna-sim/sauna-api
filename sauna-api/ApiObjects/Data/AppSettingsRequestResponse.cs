using SaunaSim.Core.Data;
using System;

namespace SaunaSim.Api.ApiObjects.Data
{
    public class AppSettingsRequestResponse
    {
        public string CommandFrequency { get; set; }
        public int PosCalcRate { get; set; }

        public AppSettingsRequestResponse() { }

        public AppSettingsRequestResponse(AppSettings settings)
        {
            CommandFrequency = settings.CommandFrequency.ToString("000.000");
            PosCalcRate = settings.PosCalcRate;
        }

        public AppSettings ToAppSettings()
        {
            return new AppSettings {
                CommandFrequency = Convert.ToDouble(this.CommandFrequency),
                PosCalcRate = this.PosCalcRate
            };
        }
    }
}
