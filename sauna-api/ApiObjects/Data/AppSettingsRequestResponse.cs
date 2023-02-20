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
            CommandFrequency = $"{settings.CommandFrequency.Item1:000}.{settings.CommandFrequency.Item2:000}";
            PosCalcRate = settings.PosCalcRate;
        }

        public AppSettings ToAppSettings()
        {
            var split = CommandFrequency.Split('.');

            return new AppSettings {
                
                CommandFrequency = (Convert.ToUInt16(split[0]), Convert.ToUInt16(split[1])),
                PosCalcRate = this.PosCalcRate,
            };
        }
    }
}
