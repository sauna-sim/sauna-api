using System;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core
{
    class VatsimClientPilot : IVatsimClient
    {
        public VatsimConnectionHandler ConnHandler { get; private set; }

        private string NetworkId { get; set; }

        public string Callsign { get; private set; }

        public Action<string> Logger { get; set; }

        public async Task<bool> Connect(string hostname, int port, string callsign, string cid, string password, string fullname, bool vatsim)
        {            
            Callsign = callsign;
            NetworkId = cid;

            // Establish Connection
            ConnHandler = new VatsimConnectionHandler()
            {
                Logger = Logger
            };

            await ConnHandler.Connect(hostname, port);

            if (ConnHandler.Status == CONN_STATUS.DISCONNECTED)
            {
                return false;
            }

            // Connect client
            await ConnHandler.AddClient(CLIENT_TYPE.PILOT, Callsign, fullname, cid, password);

            return true;
        }

        public async Task Disconnect()
        {
            // Send Disconnect Message
            await ConnHandler.RemoveClient(CLIENT_TYPE.PILOT, Callsign, NetworkId);
            await ConnHandler.Disconnect();
        }

        ~VatsimClientPilot()
        {
            Disconnect();
        }
    }
}
