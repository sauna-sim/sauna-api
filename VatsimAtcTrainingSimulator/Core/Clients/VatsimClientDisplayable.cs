using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Clients
{
    public class VatsimClientDisplayable
    {
        public VatsimClientDisplayable(IVatsimClient client)
        {
            this.client = client;
        }

        [Browsable(false)]
        public IVatsimClient client { get; private set; }

        public string Callsign => client.Callsign;

        public string Type
        {
            get
            {
                if (client is VatsimClientPilot)
                {
                    return "Aircraft";
                }
                return "None";
            }
        }

        [DisplayName("Connection")]
        public CONN_STATUS ConnectionStatus => client.ConnectionStatus;

        public string Paused
        {
            get
            {
                if (client is VatsimClientPilot && ((VatsimClientPilot)client).Paused)
                {
                    return "Paused";
                }
                return "Unpaused";
            }
        }

        private int RoundDoubles(double input)
        {
            return Convert.ToInt32(Math.Round(input, MidpointRounding.AwayFromZero));
        }

        [DisplayName("Heading (Magnetic)")]
        public int Heading
        {
            get
            {
                if (client is VatsimClientPilot)
                {
                    return RoundDoubles(((VatsimClientPilot)client).Position.Heading_Mag);
                }
                return -1;
            }
        }

        [DisplayName("Airspeed (KIAS)")]
        public int Airspeed
        {
            get
            {
                if (client is VatsimClientPilot)
                {
                    return RoundDoubles(((VatsimClientPilot)client).Position.IndicatedAirSpeed);
                }
                return -1;
            }
        }

        [DisplayName("Altitude (ft)")]
        public int Altitude
        {
            get
            {
                if (client is VatsimClientPilot)
                {
                    return RoundDoubles(((VatsimClientPilot)client).Position.IndicatedAltitude);
                }
                return -1;
            }
        }

        [DisplayName("Altimeter Setting")]
        public string AltimeterSetting
        {
            get
            {
                if (client is VatsimClientPilot)
                {
                    return $"{RoundDoubles(((VatsimClientPilot)client).Position.AltimeterSetting_hPa)}hPa";
                }
                return "";
            }
        }
    }
}
