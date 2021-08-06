using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.GeoTools;

namespace VatsimAtcTrainingSimulator.Core
{
    public enum XpdrMode
    {
        STANDBY = 'S',
        MODE_C = 'C',
        IDENT = 'Y'
    }

    class VatsimClientPilot : IVatsimClient
    {
        public VatsimConnectionHandler ConnHandler { get; private set; }

        private string NetworkId { get; set; }
        private Thread posUpdThread;

        public string Callsign { get; private set; }

        public Action<string> Logger { get; set; }

        public bool Paused { get; set; }
        public XpdrMode XpdrMode { get; private set; }
        public int Squawk { get; private set; }
        public int Rating { get; private set; }
        public AcftData Position { get; private set; }
        public double Pitch { get; private set; }
        public double Bank { get; private set; }
        private bool _onGround = false;
        public bool OnGround
        {
            get => _onGround;
            private set
            {
                _onGround = value;
                JObject obj = new JObject(new JProperty("on_ground", _onGround));
                ConnHandler.SendData($"$CQ{Callsign}:@94836:ACC:{obj}");
            }
        }
        public int PresAltDiff { get; private set; }

        public async Task<bool> Connect(string hostname, int port, string callsign, string cid, string password, string fullname, bool vatsim)
        {
            Callsign = callsign;
            NetworkId = cid;

            // Establish Connection
            ConnHandler = new VatsimConnectionHandler()
            {
                Logger = Logger,
                RequestCommand = HandleRequest
            };

            await ConnHandler.Connect(hostname, port);

            if (ConnHandler.Status == CONN_STATUS.DISCONNECTED)
            {
                return false;
            }

            // Connect client
            await ConnHandler.AddClient(CLIENT_TYPE.PILOT, Callsign, fullname, cid, password);

            // Prefill data
            Paused = true;

            return true;
        }

        private void CalculateNextPosition(int nextUpdateTimeMs)
        {
            if (!Paused)
            {
                AcftGeoUtil.CalculateNextLatLon(Position, 0, nextUpdateTimeMs);
            }
        }

        private void AircraftPositionWorker()
        {
            try
            {
                while (ConnHandler.Status == CONN_STATUS.CONNECTED)
                {
                    // Construct position data
                    int posdata = 0;
                    if (OnGround)
                    {
                        posdata += 2;
                    }
                    posdata += Convert.ToInt32(Position.Heading_Mag * 1024.0 / 360.0) << 2;
                    posdata += Convert.ToInt32(Bank * 512.0 / 180.0) << 12;
                    posdata += Convert.ToInt32(Pitch * 256.0 / 90.0) << 22;

                    // Send Position
                    string posStr = $"@{(char)XpdrMode}:{Callsign}:{Squawk}:{Rating}:{Position.Latitude}:{Position.Longitude}:{Position.Altitude}:{Position.GroundSpeed}:{posdata}:{PresAltDiff}";
                    ConnHandler.SendData(posStr);

                    // Calculate next position
                    CalculateNextPosition(5000);

                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException)
                {
                    return;
                }

                throw ex;
            }
        }

        public void HandleRequest(string command, string requestee, string requester)
        {
            if (requestee.Equals(Callsign))
            {
                switch (command)
                {
                    case "ACC":
                        AccConfig config = new AccConfig()
                        {
                            is_full_data = true,
                            on_ground = OnGround
                        };
                        string jsonOut = JsonConvert.SerializeObject(config);

                        ConnHandler.SendData($"$CQ{Callsign}:{requester}:{command}:{jsonOut}");
                        break;
                }
            }
        }

        public void SetInitialData(XpdrMode xpdrMode, int squawk, int rating, double lat, double lon, double alt, double gs, int posdata, int presAltDiff)
        {
            XpdrMode = xpdrMode;
            Squawk = squawk;
            Rating = rating;
            Position = new AcftData();

            PresAltDiff = presAltDiff;

            // Read position data
            posdata >>= 1;
            OnGround = (posdata & 0x1) != 0;
            posdata >>= 1;
            double hdg = posdata & 0x3FF;
            hdg = (hdg * 360.0) / 1024.0;
            posdata >>= 10;
            Bank = posdata & 0x3FF;
            Bank = (Bank * 180.0) / 512;
            posdata >>= 10;
            Pitch = posdata & 0x3FF;
            Pitch = (Pitch * 90.0) / 256.0;

            // Set initial position
            Position.UpdatePosition(lat, lon, alt, hdg, 250);

            // Send initial configuration
            HandleRequest("ACC", Callsign, "@94836");

            // Start Position Update Thread
            posUpdThread = new Thread(new ThreadStart(AircraftPositionWorker));
            posUpdThread.Start();
        }

        public async Task Disconnect()
        {
            // Send Disconnect Message
            await ConnHandler.RemoveClient(CLIENT_TYPE.PILOT, Callsign, NetworkId);
            await ConnHandler.Disconnect();
        }

        ~VatsimClientPilot()
        {
            Disconnect().ConfigureAwait(false);
        }
    }
}
