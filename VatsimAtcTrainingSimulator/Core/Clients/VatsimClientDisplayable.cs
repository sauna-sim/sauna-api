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

        public VatsimClientDisplayable(IVatsimClient client)
        {
            this.client = client;
        }
    }
}
