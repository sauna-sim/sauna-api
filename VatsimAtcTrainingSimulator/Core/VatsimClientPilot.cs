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

    public enum TurnDirection
    {
        LEFT = -1,
        RIGHT = 1,
        SHORTEST = 0
    }

    public enum AssignedIASType
    {
        FREE = -2,
        LESS = -1,
        EXACT = 0,
        MORE = 1
    }

    public class VatsimClientPilot : IVatsimClient
    {
        // Constants
        private const int POS_SEND_INTERVAL = 5000;
        private const int POS_CALC_INVERVAL = 500;

        // Properties
        public VatsimConnectionHandler ConnHandler { get; private set; }

        private string NetworkId { get; set; }
        private Thread posUpdThread;
        private Thread posSendThread;

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
                _ = ConnHandler.SendData($"$CQ{Callsign}:@94836:ACC:{obj}");
            }
        }
        public int PresAltDiff => (int) (((Position.AltimeterSetting_hPa == 0 ? AcftGeoUtil.STD_PRES_HPA : Position.AltimeterSetting_hPa) - AcftGeoUtil.STD_PRES_HPA) * AcftGeoUtil.STD_PRES_DROP);

        // Assigned values
        private int _assignedHeading = 0;
        public int Assigned_Heading { get => _assignedHeading; set => _assignedHeading = (value >= 360) ? value - 360 : value; }
        public TurnDirection Assigned_TurnDirection { get; set; } = TurnDirection.SHORTEST;
        public int Assigned_IAS { get; set; } = -1;
        public AssignedIASType Assigned_IAS_Type { get; set; } = AssignedIASType.FREE;
        public int Assigned_Altitude { get; set; }

        public async Task<bool> Connect(string hostname, int port, string callsign, string cid, string password, string fullname, bool vatsim)
        {
            Callsign = callsign;
            NetworkId = cid;

            // Establish Connection
            ConnHandler = new VatsimConnectionHandler(Callsign)
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
                    posdata += Convert.ToInt32(Position.Heading_Mag * 1024.0 / 360.0) << 2;
                    posdata += Convert.ToInt32(Bank * 512.0 / 180.0) << 12;
                    posdata += Convert.ToInt32(Pitch * 256.0 / 90.0) << 22;

                    // Send Position
                    string posStr = $"@{(char)XpdrMode}:{Callsign}:{Squawk}:{Rating}:{Position.Latitude}:{Position.Longitude}:{Position.IndicatedAltitude}:{Position.GroundSpeed}:{posdata}:{PresAltDiff}";
                    _ = ConnHandler.SendData(posStr);

                    Thread.Sleep(POS_SEND_INTERVAL);
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

                        // Calculate next altitude
                        double nextAlt = Position.IndicatedAltitude + ((POS_CALC_INVERVAL / 1000.0) * (0 / 60.0));

                        // Calculate next position and heading
                        double turnAmount = AcftGeoUtil.CalculateTurnAmount(Position.Heading_Mag, Assigned_Heading);
                        double distanceTravelledNMi = AcftGeoUtil.CalculateDistanceTravelledNMi(Position.GroundSpeed, POS_CALC_INVERVAL);

                        if (Math.Abs(turnAmount) > 1)
                        {
                            // Calculate bank angle
                            double bankAngle = AcftGeoUtil.CalculateBankAngle(Position.GroundSpeed, 25, 3);

                            // Calculate radius of turn
                            double radiusOfTurn = AcftGeoUtil.CalculateRadiusOfTurn(bankAngle, Position.GroundSpeed);

                            // Calculate degrees to turn
                            double degreesToTurn = Math.Min(Math.Abs(turnAmount), AcftGeoUtil.CalculateDegreesTurned(distanceTravelledNMi, radiusOfTurn));

                            // Figure out turn direction
                            bool isRightTurn = (Assigned_TurnDirection == TurnDirection.SHORTEST) ? 
                                (isRightTurn = turnAmount > 0) : 
                                (isRightTurn = Assigned_TurnDirection == TurnDirection.RIGHT);
                            
                            // Calculate end heading
                            double endHeading = AcftGeoUtil.CalculateEndHeading(Position.Heading_Mag, degreesToTurn, isRightTurn);

                            // Calculate chord line data
                            Tuple<double, double> chordLine = AcftGeoUtil.CalculateChordHeadingAndDistance(Position.Heading_Mag, degreesToTurn, radiusOfTurn, isRightTurn);

                            // Calculate new position
                            Position.Heading_Mag = chordLine.Item1;
                            AcftGeoUtil.CalculateNextLatLon(Position, nextAlt, chordLine.Item2);
                            Position.Heading_Mag = endHeading;
                        } else
                        {
                            Position.Heading_Mag = Assigned_Heading;
                            // Calculate new position
                            AcftGeoUtil.CalculateNextLatLon(Position, nextAlt, distanceTravelledNMi);
                        }
                    }

                    Thread.Sleep(POS_CALC_INVERVAL);
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
            Position = new AcftData();

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
            Position.Heading_Mag = hdg;
            Position.IndicatedAirSpeed = 250;
            Position.UpdatePosition(lat, lon, alt);

            // Set initial assignments
            Assigned_Heading = Convert.ToInt32(hdg);
            Assigned_Altitude = Convert.ToInt32(alt);
            Assigned_TurnDirection = TurnDirection.SHORTEST;


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
            await ConnHandler.RemoveClient(CLIENT_TYPE.PILOT, Callsign, NetworkId);
            await ConnHandler.Disconnect();
        }

        ~VatsimClientPilot()
        {
            Disconnect().ConfigureAwait(false);
        }
    }
}
