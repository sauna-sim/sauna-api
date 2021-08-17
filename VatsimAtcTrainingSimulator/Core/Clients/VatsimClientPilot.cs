using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.GeoTools;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control;

namespace VatsimAtcTrainingSimulator.Core
{
    public enum XpdrMode
    {
        STANDBY = 'S',
        MODE_C = 'C',
        IDENT = 'Y'
    }

    public enum ConstraintType
    {
        FREE = -2,
        LESS = -1,
        EXACT = 0,
        MORE = 1
    }

    public class VatsimClientPilot : IVatsimClient
    {
        // Constants
        private const int POS_CALC_INVERVAL = 1000;

        // Properties
        public VatsimClientConnectionHandler ConnHandler { get; private set; }

        private string NetworkId { get; set; }
        private Thread posUpdThread;
        private Thread posSendThread;

        public string Callsign { get; private set; }

        public Action<string> Logger { get; set; }
        public Action<CONN_STATUS> StatusChangeAction { get; set; }

        public bool Paused { get; set; }
        public XpdrMode XpdrMode { get; private set; }
        public int Squawk { get; private set; }
        public int Rating { get; private set; }
        private AircraftPosition _position;
        public AircraftPosition Position { get => _position; private set => _position = value; }
        private bool _onGround = false;
        public bool OnGround
        {
            get => _onGround;
            private set
            {
                _onGround = value;
                JObject obj = new JObject(new JProperty("on_ground", _onGround));
                _ = ConnHandler.SendData($"$CQ{Callsign}:@94836:ACC:{obj}");
            }
        }

        // Assigned values
        public AircraftControl Control { get; private set; }
        public int Assigned_IAS { get; set; } = -1;
        public ConstraintType Assigned_IAS_Type { get; set; } = ConstraintType.FREE;

        public CONN_STATUS ConnectionStatus => ConnHandler == null ? CONN_STATUS.DISCONNECTED : ConnHandler.Status;

        public VatsimClientPilot()
        {
            Position = new AircraftPosition();
            Control = new AircraftControl();
            ConnHandler = new VatsimClientConnectionHandler(this);
        }

        public async Task<bool> Connect(string hostname, int port, string callsign, string cid, string password, string fullname, bool vatsim)
        {
            Callsign = callsign;
            NetworkId = cid;

            // Establish Connection
            ConnHandler = new VatsimClientConnectionHandler(this)
            {
                Logger = Logger,
                RequestCommand = HandleRequest,
                StatusChangeAction = StatusChangeAction
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

        private void AircraftPositionSender()
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
                    posdata += Convert.ToInt32(Position.Heading_True * 1024.0 / 360.0) << 2;
                    posdata += Convert.ToInt32(Position.Bank * 512.0 / 180.0) << 12;
                    posdata += Convert.ToInt32(Position.Pitch * 256.0 / 90.0) << 22;

                    // Send Position
                    string posStr = $"@{(char)XpdrMode}:{Callsign}:{Squawk}:{Rating}:{Position.Latitude}:{Position.Longitude}:{Position.AbsoluteAltitude}:{Position.GroundSpeed}:{posdata}:{Position.PresAltDiff}";
                    if (Properties.Settings.Default.sendIas)
                    {
                        posStr += $":{Position.IndicatedAirSpeed}";
                    }
                    _ = ConnHandler.SendData(posStr);

                    Thread.Sleep(Properties.Settings.Default.updateRate);
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

        private void AircraftPositionWorker()
        {
            try
            {
                while (ConnHandler.Status == CONN_STATUS.CONNECTED)
                {
                    // Calculate position
                    if (!Paused)
                    {
                        int slowDownKts = -2;
                        int speedUpKts = 5;

                        // Calculate Speed Change
                        if (Assigned_IAS != -1)
                        {
                            if (Assigned_IAS <= Position.IndicatedAirSpeed)
                            {
                                Position.IndicatedAirSpeed = Math.Max(Assigned_IAS, Position.IndicatedAirSpeed + (slowDownKts * POS_CALC_INVERVAL / 1000.0));
                            } else
                            {
                                Position.IndicatedAirSpeed = Math.Min(Assigned_IAS, Position.IndicatedAirSpeed + (speedUpKts * POS_CALC_INVERVAL / 1000.0));
                            }
                        }

                        Control.UpdatePosition(ref _position, POS_CALC_INVERVAL);
                    }

                    Thread.Sleep(POS_CALC_INVERVAL);
                }
            }
            catch (ThreadAbortException ex)
            {
                return;
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

                        _ = ConnHandler.SendData($"$CQ{Callsign}:{requester}:{command}:{jsonOut}");
                        break;
                }
            }
        }

        public void SetInitialData(XpdrMode xpdrMode, int squawk, int rating, double lat, double lon, double alt, double gs, int posdata, int presAltDiff)
        {
            XpdrMode = xpdrMode;
            Squawk = squawk;
            Rating = rating;
            Position = new AircraftPosition();

            // Read position data
            posdata >>= 1;
            OnGround = (posdata & 0x1) != 0;
            posdata >>= 1;
            double hdg = posdata & 0x3FF;
            hdg = (hdg * 360.0) / 1024.0;
            posdata >>= 10;
            double bank = posdata & 0x3FF;
            Position.Bank = (bank * 180.0) / 512;
            posdata >>= 10;
            double pitch = posdata & 0x3FF;
            Position.Pitch = (pitch * 90.0) / 256.0;

            // Set initial position
            Position.Heading_Mag = hdg;
            Position.IndicatedAirSpeed = 250;
            Position.Latitude = lat;
            Position.Longitude = lon;
            Position.IndicatedAltitude = alt;
            Position.UpdatePosition();

            // Set initial assignments
            Control = new AircraftControl(new HeadingHoldInstruction(Convert.ToInt32(hdg)), new AltitudeHoldInstruction(Convert.ToInt32(alt)));

            // Send initial configuration
            HandleRequest("ACC", Callsign, "@94836");

            // Start Position Update Thread
            posUpdThread = new Thread(new ThreadStart(AircraftPositionWorker));
            posUpdThread.Name = $"{Callsign} Position Worker";
            posUpdThread.Start();

            // Start Position Send Thread
            posSendThread = new Thread(new ThreadStart(AircraftPositionSender));
            posSendThread.Name = $"{Callsign} Position Sender";
            posSendThread.Start();
        }

        public async Task Disconnect()
        {
            // Send Disconnect Message
            if (ConnHandler != null)
            {
                await ConnHandler.RemoveClient(CLIENT_TYPE.PILOT, Callsign, NetworkId);
                ConnHandler.Disconnect();
            }
        }

        ~VatsimClientPilot()
        {
            Disconnect().ConfigureAwait(false);
        }
    }
}
