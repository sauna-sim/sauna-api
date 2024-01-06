using FsdConnectorNet;
using FsdConnectorNet.Args;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.MathTools;
using SaunaSim.Core.Simulator.Aircraft.Autopilot;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using SaunaSim.Core.Simulator.Aircraft.Performance;
using System.Diagnostics;
using SaunaSim.Core.Simulator.Aircraft.Ground;
using SaunaSim.Core.Simulator.Aircraft.Pilot;
using AviationCalcUtilNet.Atmos.Grib;
using AviationCalcUtilNet.Magnetic;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.Units;
using NavData_Interface.Objects.Fixes;
using AviationCalcUtilNet.Physics;
using AviationCalcUtilNet.Aviation;
using AviationCalcUtilNet.Math;

namespace SaunaSim.Core.Simulator.Aircraft
{
    public enum ConstraintType
    {
        FREE = -2,
        LESS = -1,
        EXACT = 0,
        MORE = 1
    }

    public class AircraftPositionUpdateEventArgs : EventArgs
    {
        public DateTime TimeStamp { get; set; }
        public SimAircraft Aircraft { get; set; }

        public AircraftPositionUpdateEventArgs(DateTime timeStamp, SimAircraft aircraft)
        {
            TimeStamp = timeStamp;
            Aircraft = aircraft;
        }
    }

    public class AircraftConnectionStatusChangedEventArgs : EventArgs
    {
        public string Callsign { get; set; }
        public ConnectionStatusType ConnectionStatus { get; set; }

        public AircraftConnectionStatusChangedEventArgs(string callsign, ConnectionStatusType connectionStatus)
        {
            Callsign = callsign;
            ConnectionStatus = connectionStatus;
        }
    }

    public class AircraftSimStateChangedEventArgs : EventArgs
    {
        public string Callsign { get; set; }
        public bool Paused { get; set; }
        public double SimRate { get; set; }

        public AircraftSimStateChangedEventArgs(string callsign, bool paused, double simRate)
        {
            Callsign = callsign;
            Paused = paused;
            SimRate = simRate;
        }
    }

    public class SimAircraft : IDisposable
    {
        private Thread _posUpdThread;
        private PauseableTimer _delayTimer;
        private bool disposedValue;
        private bool _shouldUpdatePosition = false;
        private ClientInfo _clientInfo;
        private Stopwatch _lagTimer;
        private GribTileManager _gribTileManager;
        private MagneticTileManager _magTileManager;
        private CommandHandler _commandHandler;

        // Events
        public event EventHandler<AircraftPositionUpdateEventArgs> PositionUpdated;
        public event EventHandler<AircraftConnectionStatusChangedEventArgs> ConnectionStatusChanged;
        public event EventHandler<AircraftSimStateChangedEventArgs> SimStateChanged;

        // Connection Info
        public LoginInfo LoginInfo { get; private set; }

        public string Callsign => LoginInfo.callsign;

        public Connection Connection { get; private set; }

        private ConnectionStatusType _connectionStatus = ConnectionStatusType.WAITING;
        public ConnectionStatusType ConnectionStatus
        {
            get => _connectionStatus;
            private set
            {
                _connectionStatus = value;
                Task.Run(() => ConnectionStatusChanged?.Invoke(this, new AircraftConnectionStatusChangedEventArgs(Callsign, _connectionStatus)));
            }
        }

        // Simulator Data
        private bool _paused;
        public bool Paused
        {
            get => _paused;
            set
            {
                _paused = value;
                if (DelayMs > 0 && _delayTimer != null)
                {
                    if (!_paused)
                    {
                        _delayTimer.Start();
                    }
                    else
                    {
                        _delayTimer.Pause();
                    }
                }

                Task.Run(() => SimStateChanged?.Invoke(this, new AircraftSimStateChangedEventArgs(Callsign, _paused, SimRate / 10.0)));
            }
        }

        private int _simRate;
        public int SimRate
        {
            get => _simRate;
            set
            {
                if (value > 80)
                {
                    _simRate = 80;
                }
                else if (value < 1)
                {
                    _simRate = 1;
                }
                else
                {
                    _simRate = value;
                }

                if (DelayMs > 0 && _delayTimer != null)
                {
                    _delayTimer.RatePercent = _simRate * 10;
                }

                Task.Run(() => SimStateChanged?.Invoke(this, new AircraftSimStateChangedEventArgs(Callsign, _paused, SimRate / 10.0)));
            }
        }

        // Aircraft Info
        private AircraftPosition _position;
        public AircraftPosition Position => _position;

        private AircraftAutopilot _autopilot;
        public AircraftAutopilot Autopilot => _autopilot;

        private AircraftGroundHandler _groundhandler;
        public AircraftGroundHandler GroundHandler => _groundhandler;

        private ArtificialPilot _artificialpilot;
        public ArtificialPilot ArtificialPilot => _artificialpilot;

        private AircraftFms _fms;
        public AircraftFms Fms => _fms;

        private AircraftData _data;
        public AircraftData Data => _data;

        public PerfData PerformanceData { get; set; }

        public TransponderModeType XpdrMode { get; set; }

        public int Squawk { get; set; }

        public int DelayMs { get; set; }

        public int DelayRemainingMs => _delayTimer?.TimeRemainingMs() ?? DelayMs;

        public string AircraftType { get; private set; }

        public string AirlineCode { get; private set; }

        private FlightPlan? _flightPlan;
        public FlightPlan? FlightPlan
        {
            get => Connection.CurrentFlightPlan;
            set
            {
                if (ConnectionStatus == ConnectionStatusType.CONNECTED)
                {
                    Connection.SendFlightPlan(value);
                }
                else
                {
                    _flightPlan = value;
                }
            }
        }

        public FlightPhaseType FlightPhase { get; set; }


        // Loggers
        public Action<string> LogInfo { get; set; }

        public Action<string> LogWarn { get; set; }

        public Action<string> LogError { get; set; }

        //Airport
        public Airport RelaventAirport { get; set; }

        public SimAircraft(string callsign, string networkId, string password, string fullname, string hostname, ushort port, ProtocolRevision protocol, ClientInfo clientInfo,
              PerfData perfData, Latitude lat, Longitude lon, Length alt, Bearing hdg_mag, MagneticTileManager magTileManager, GribTileManager gribTileManager, CommandHandler commandHandler, int delayMs = 0)
        {
            LoginInfo = new LoginInfo(networkId, password, callsign, fullname, PilotRatingType.Student, hostname, protocol, AppSettingsManager.CommandFrequency, port);
            _clientInfo = clientInfo;
            _lagTimer = new Stopwatch();
            Connection = new Connection();
            Connection.Connected += OnConnectionEstablished;
            Connection.Disconnected += OnConnectionTerminated;
            Connection.FrequencyMessageReceived += OnFrequencyMessageReceived;
            Connection.PrivateMessageReceived += OnPrivateMessageReceived;

            _magTileManager = magTileManager;
            _gribTileManager = gribTileManager;
            _commandHandler = commandHandler;

            _simRate = 10;
            _paused = true;

            _position = new AircraftPosition(lat, lon, alt, this, magTileManager)
            {
                Pitch = Angle.FromDegrees(2.5),
                Bank = Angle.FromDegrees(0),
                IndicatedAirSpeed = Velocity.FromKnots(250.0),
                Heading_Mag = hdg_mag
            };

            FlightPhase = FlightPhaseType.IN_FLIGHT;

            _data = new AircraftData(this)
            {
                ThrustLeverPos = 0,
                SpeedBrakePos = 0,
                AircraftConfig = new AircraftConfig(true, false, false, true, true, false, false, 0, false, false, new AircraftEngine(true, false), new AircraftEngine(true, false)),

                // TODO: Change This To Actually Calculate Mass
                Mass_kg = (perfData.MTOW_kg + perfData.OEW_kg) / 2
            };

            _autopilot = new AircraftAutopilot(this)
            {
                SelectedAltitude = Convert.ToInt32(alt),
                SelectedHeading = Convert.ToInt32(hdg_mag),
                SelectedSpeed = Convert.ToInt32(250.0),
                CurrentLateralMode = LateralModeType.HDG,
                CurrentThrustMode = ThrustModeType.SPEED,
                CurrentVerticalMode = VerticalModeType.FLCH
            };

            _groundhandler = new AircraftGroundHandler(this) { };

            _artificialpilot = new ArtificialPilot(this) { };

            _fms = new AircraftFms(this, magTileManager);
            PerformanceData = perfData;
            DelayMs = delayMs;

            // Set Relavent airport
            RelaventAirport = DataHandler.GetAirportByIdentifier(DataHandler.FAKE_AIRPORT_NAME);

            AircraftType = "A320"; // TODO: Change This
            AirlineCode = "JBU"; // TODO: Change This
        }

        private void UpdateGribPoint()
        {
            var tile = _gribTileManager.FindOrCreateTile(Position.PositionGeoPoint, DateTime.UtcNow);
            Position.GribPoint = tile.GetClosestPoint(Position.PositionGeoPoint);
        }

        public void Start()
        {
            // Set initial assignments
            UpdateGribPoint();

            // Determine flight phase
            // Lookup dep airport

            // Connect if no delay, otherwise start timer
            if (DelayMs <= 0)
            {
                OnTimerElapsed(this, null);
            }
            else
            {
                _delayTimer = new PauseableTimer(DelayMs, _simRate * 10);
                _delayTimer.Elapsed += OnTimerElapsed;

                if (!_paused)
                {
                    _delayTimer.Start();
                }
            }
        }

        private void OnFrequencyMessageReceived(object sender, FrequencyMessageEventArgs e)
        {
            if (e.Frequency == AppSettingsManager.CommandFrequency && e.Message.StartsWith($"{Callsign}, "))
            {
                // Split message into args
                List<string> split = e.Message.Replace($"{Callsign}, ", "").Split(' ').ToList();

                // Loop through command list
                while (split.Count > 0)
                {
                    // Get command name
                    string command = split[0].ToLower();
                    split.RemoveAt(0);

                    split = _commandHandler.HandleCommand(command, this, split, (string msg) =>
                    {
                        string returnMsg = msg.Replace($"{Callsign} ", "");
                        Connection.SendFrequencyMessage(e.Frequency, returnMsg);
                    });
                }
            }
        }

        private void OnPrivateMessageReceived(object sender, PrivateMessageEventArgs e)
        {
            // Split message into args
            List<string> split = e.Message.Replace($"{Callsign}, ", "").Split(' ').ToList();

            // Loop through command list
            while (split.Count > 0)
            {
                // Get command name
                string command = split[0].ToLower();
                split.RemoveAt(0);

                split = _commandHandler.HandleCommand(command, this, split, (string msg) =>
                {
                    string returnMsg = msg.Replace($"{Callsign} ", "");
                    Connection.SendPrivateMessage(e.From, returnMsg);
                });
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            DelayMs = -1;
            _delayTimer?.Stop();

            // Connect to FSD Server
            Connection.Connect(_clientInfo, LoginInfo, GetFsdPilotPosition(), _data.AircraftConfig, new PlaneInfo(AircraftType, AirlineCode));
            ConnectionStatus = ConnectionStatusType.CONNECTING;
        }

        private void OnConnectionEstablished(object sender, EventArgs e)
        {
            ConnectionStatus = ConnectionStatusType.CONNECTED;
            // Start Position Update Thread
            _shouldUpdatePosition = true;
            _posUpdThread = new Thread(new ThreadStart(AircraftPositionWorker));
            _posUpdThread.Name = $"{Callsign} Position Worker";
            _posUpdThread.Start();

            // Send Flight Plan
            Connection.SendFlightPlan(_flightPlan);

            // Stick to Ground
            Connection.SetOnGround(_position.OnGround);

            // Set Flap, Gear and Spoiler Position
            var flapsPct = (double)_data.Config / PerformanceData.ConfigList.Count;
            Connection.SetFlapsPct((int)(flapsPct * 100.0));
            Connection.SetGearDown(PerformanceData.ConfigList[_data.Config].GearDown);
            Connection.SetSpoilersDeployed(_data.SpeedBrakePos > 0);

            // Set Aircraft Lights
            Connection.SetBeaconLight(true);
            Connection.SetNavLights(true);
            Connection.SetStrobeLight(false);
            Connection.SetLandingLights(false);
            Connection.SetTaxiLights(false);
            Connection.SetLogoLight(false);

            // Set Engines ON/OFF
            Connection.SetEnginesOn(true);
        }

        private void OnConnectionTerminated(object sender, EventArgs e)
        {
            ConnectionStatus = ConnectionStatusType.DISCONNECTED;
            _shouldUpdatePosition = false;
            _delayTimer?.Stop();
        }
        private void HandleInFlight()
        {
            // Update FMS
            _fms.OnPositionUpdate((int)(AppSettingsManager.PosCalcRate * (_simRate / 10.0)));

            // Run Config Handler
            _artificialpilot.OnPositionUpdate((int)(AppSettingsManager.PosCalcRate * (_simRate / 10.0)));

            // Run Autopilot
            _autopilot.OnPositionUpdate((int)(AppSettingsManager.PosCalcRate * (_simRate / 10.0)));

            // TODO: Update Mass

            // Move Aircraft
            MoveAircraft((int)(AppSettingsManager.PosCalcRate * (_simRate / 10.0)));
        }
        private void HandleOnGround()
        {
            // Run ground handler
            _groundhandler.OnPositionUpdate((int)(AppSettingsManager.PosCalcRate * (_simRate / 10.0)));

            // TODO: Update Mass

            // Move aircraft
            MoveAircraftOnGround((int)(AppSettingsManager.PosCalcRate * (_simRate / 10.0)));
        }
        private void AircraftPositionWorker()
        {
            while (_shouldUpdatePosition)
            {
                // Check calculation step time
                _lagTimer.Restart();

                // Calculate position
                if (!_paused)
                {
                    if (FlightPhase == FlightPhaseType.IN_FLIGHT)
                    {
                        //If we're on APCH below 50ft afe then switch flight phase to ground
                        if (_autopilot.CurrentVerticalMode == VerticalModeType.LAND &&
                            (Position.TrueAltitude < RelaventAirport.Elevation + Length.FromFeet(1)))
                        {
                            FlightPhase = FlightPhaseType.ON_GROUND;

                            _groundhandler.GroundPhase = GroundPhaseType.LAND;
                            HandleOnGround();
                        }
                        else
                        {
                            HandleInFlight();
                        }
                    }
                    else if (FlightPhase == FlightPhaseType.ON_GROUND)
                    {
                        HandleOnGround();
                    }

                    // Update Aircraft FSD Config
                    _artificialpilot.AircraftLights();

                    // Update Grib Data
                    UpdateGribPoint();

                    // Update FSD
                    Connection.UpdatePosition(GetFsdPilotPosition());

                    Task.Run(() => PositionUpdated.Invoke(this, new AircraftPositionUpdateEventArgs(DateTime.UtcNow, this)));
                }

                // Remove calculation time from position calculation rate
                int sleepTime = AppSettingsManager.PosCalcRate - (int)_lagTimer.ElapsedMilliseconds;

                // Sleep the thread
                if (sleepTime > 0)
                {
                    Thread.Sleep(sleepTime);
                }
            }
        }
        private void MoveAircraftOnGround(int intervalMs)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(intervalMs);
            Velocity vi = Position.GroundSpeed;
            Velocity vf = PhysicsUtil.KinematicsFinalVelocity(vi, Position.Forward_Acceleration, t);
            Position.GroundSpeed = vf;

            //Calculate displacement
            Length displacement = PhysicsUtil.KinematicsDisplacement2(vi, Position.Forward_Acceleration, t);

            GeoPoint point = (GeoPoint)Position.PositionGeoPoint.Clone();
            point.MoveBy(Position.Track_True, displacement);
            Position.Latitude = point.Lat;
            Position.Longitude = point.Lon;

            if (Position.VerticalSpeed.Value() > 0)
            {
                Position.TrueAltitude += PhysicsUtil.KinematicsDisplacement1(Position.VerticalSpeed, (Velocity)0, t);
            }
            else
            {
                Position.TrueAltitude = RelaventAirport.Elevation;
            }
        }

        private void MoveAircraft(int intervalMs)
        {
            double t = intervalMs / 1000.0;

            // Calculate Pitch, Bank, and Thrust Lever Position
            Position.Pitch += PhysicsUtil.KinematicsDisplacement2((double)Position.PitchRate, 0, t);
            Position.Bank += PhysicsUtil.KinematicsDisplacement2((double)Position.BankRate, 0, t);
            Data.ThrustLeverPos += PhysicsUtil.KinematicsDisplacement2(Data.ThrustLeverVel, 0, t);

            // Calculate Performance Values
            (double accelFwd, double vs) = PerfDataHandler.CalculatePerformance(PerformanceData, Position.Pitch.Degrees, Data.ThrustLeverPos / 100.0, Position.IndicatedAirSpeed.Knots,
                Position.DensityAltitude.Feet, Data.Mass_kg, Data.SpeedBrakePos, Data.Config);

            // Calculate New Velocities
            Velocity curGs = Position.GroundSpeed;
            Position.IndicatedAirSpeed = (Velocity)PhysicsUtil.KinematicsFinalVelocity((double)Position.IndicatedAirSpeed, Velocity.ConvertKtsToMpers(accelFwd), t);
            Position.VerticalSpeed = Velocity.FromFeetPerMinute(vs);

            // Calculate Accelerations
            Position.Forward_Acceleration = (Acceleration)accelFwd;

            // Calculate Displacement
            Length displacement = (Length)(0.5 * (double)(Position.GroundSpeed + curGs) * t);

            // Calculate Position
            if (Math.Abs((double)Position.Bank) < double.Epsilon)
            {
                GeoPoint point = (GeoPoint)Position.PositionGeoPoint.Clone();
                point.MoveBy(Position.Track_True, displacement);
                Position.Latitude = point.Lat;
                Position.Longitude = point.Lon;

                // Calculate Yaw Rate
                Position.YawRate = (AngularVelocity)0;
            }
            else
            {
                // Calculate radius of turn
                Length radiusOfTurn = AviationUtil.CalculateRadiusOfTurn(Position.GroundSpeed, (Angle)Math.Abs((double)Position.Bank));

                // Calculate degrees to turn
                Angle turnAmt = (Angle)(double)(displacement / radiusOfTurn);

                // Figure out turn direction
                if ((double)Position.Bank < 0)
                {
                    turnAmt = -turnAmt;
                }

                // Calculate end heading
                Bearing endHeading = Position.Heading_Mag + turnAmt;

                // Calculate chord line data
                (var chordBearing, var chordLength) = AviationUtil.CalculateChordForTurn(Position.Heading_Mag, turnAmt, radiusOfTurn);

                // Calculate new position
                Position.Heading_Mag = chordBearing;
                GeoPoint point = (GeoPoint)Position.PositionGeoPoint.Clone();
                point.MoveBy(Position.Track_True, chordLength);
                Position.Latitude = point.Lat;
                Position.Longitude = point.Lon;
                Position.Heading_Mag = endHeading;

                // Calculate Yaw Rate
                Position.YawRate = turnAmt / TimeSpan.FromSeconds(t);
            }

            // Calculate Altitude
            Position.IndicatedAltitude += Position.VerticalSpeed * TimeSpan.FromSeconds(t);
        }

        public PilotPosition GetFsdPilotPosition()
        {
            return new PilotPosition(XpdrMode, (ushort)Squawk, Position.Latitude.Degrees, Position.Longitude.Degrees, Position.TrueAltitude.Feet, Position.TrueAltitude.Feet,
                Position.PressureAltitude.Feet, Position.GroundSpeed.Knots, Position.Pitch.Degrees, Position.Bank.Degrees, Position.Heading_True.Degrees, Position.OnGround, Position.Velocity_X.MetersPerSecond, Position.Velocity_Y.MetersPerSecond,
                Position.Velocity_Z.MetersPerSecond, Position.Pitch_Velocity.RadiansPerSecond, Position.Heading_Velocity.RadiansPerSecond, Position.Bank_Velocity.RadiansPerSecond);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Stop updating position
                    _shouldUpdatePosition = false;
                    _posUpdThread?.Join();

                    // Stop delay timer
                    Connection.Dispose();
                    _delayTimer?.Stop();
                    _delayTimer?.Dispose();
                }

                Connection = null;
                _posUpdThread = null;
                _position = null;
                _autopilot = null;
                _fms = null;
                //Control = null;
                _delayTimer = null;
                disposedValue = true;
            }
        }

        ~SimAircraft()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}