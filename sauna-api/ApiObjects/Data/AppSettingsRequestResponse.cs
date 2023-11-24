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
            ushort mHz = Convert.ToUInt16(split[0]);
            ushort kHz = Convert.ToUInt16(split[1]);
            if (kHz < 10)
            {
                kHz *= 100;
            } else if (kHz < 100)
            {
                kHz *= 10;
            }

            return new AppSettings {
                CommandFrequency = (mHz, kHz),
                PosCalcRate = this.PosCalcRate,
            };
        }
    }
}
