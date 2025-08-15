using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AviationCalcUtilNet.Atmos.Grib;
using AviationCalcUtilNet.Aviation;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Magnetic;
using AviationCalcUtilNet.Physics;
using AviationCalcUtilNet.Units;
using FsdConnectorNet;
using FsdConnectorNet.Args;
using NavData_Interface.Objects.Fixes;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.Autopilot;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using SaunaSim.Core.Simulator.Aircraft.Ground;
using SaunaSim.Core.Simulator.Aircraft.Performance;
using SaunaSim.Core.Simulator.Aircraft.Pilot;
using SaunaSim.Core.Simulator.Commands;
using SaunaSim.Core.Simulator.Session;

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
        private readonly Stopwatch _lagTimer;
        private readonly GribTileManager _gribTileManager;
        private MagneticTileManager _magTileManager;
        private readonly CommandHandler _commandHandler;
        
        public const int PositionUpdateInterval = 100;
        
        public CancellationToken CancelToken { private get; set; } = CancellationToken.None;

        // Events
        public event EventHandler<AircraftPositionUpdateEventArgs> PositionUpdated;
        public event EventHandler<AircraftConnectionStatusChangedEventArgs> ConnectionStatusChanged;
        public event EventHandler<AircraftSimStateChangedEventArgs> SimStateChanged;

        // Connection Info
        public string Callsign { get; private set; }

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
        private SimSessionDetails _sessionDetails;

        public SimSessionDetails SessionDetails
        {
            get => _sessionDetails;
            set
            {
                _sessionDetails = value;

                Connection?.Dispose();

                // Start FSD Connection if there is no delay
                if (_sessionDetails.SessionType == SimSessionType.FSD && DelayMs < 0)
                {
                    InitializeFsdConnection();
                }
            }
        }
        
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
        public AircraftPosition Position { get; private set; }

        public AircraftAutopilot Autopilot { get; private set; }

        public AircraftGroundHandler GroundHandler { get; }

        public ArtificialPilot ArtificialPilot { get; }

        public AircraftFms Fms { get; private set; }

        public AircraftData Data { get; }

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
            get => Connection?.CurrentFlightPlan;
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

        public SimAircraft(string callsign, PerfData perfData, Latitude lat, Longitude lon, Length alt, Bearing hdg_mag, MagneticTileManager magTileManager, GribTileManager gribTileManager, CommandHandler commandHandler, int delayMs = 0)
        {
            Callsign = callsign;
            _lagTimer = new Stopwatch();

            _magTileManager = magTileManager;
            _gribTileManager = gribTileManager;
            _commandHandler = commandHandler;

            _simRate = 10;
            _paused = true;

            Position = new AircraftPosition(lat, lon, alt, this, magTileManager)
            {
                Pitch = Angle.FromDegrees(2.5),
                Bank = Angle.FromDegrees(0),
                IndicatedAirSpeed = Velocity.FromKnots(250.0),
                Heading_Mag = hdg_mag
            };

            FlightPhase = FlightPhaseType.IN_FLIGHT;

            Data = new AircraftData(this)
            {
                ThrustLeverPos = 0,
                SpeedBrakePos = 0,
                AircraftConfig = new AircraftConfig(true, false, false, true, true, false, false, 0, false, false, new AircraftEngine(true, false), new AircraftEngine(true, false)),

                // TODO: Change This To Actually Calculate Mass
                Mass_kg = (perfData.MTOW_kg + perfData.OEW_kg) / 2
            };

            Autopilot = new AircraftAutopilot(this)
            {
                SelectedAltitude = Convert.ToInt32(alt.Feet),
                SelectedHeading = Convert.ToInt32(hdg_mag.Degrees),
                SelectedSpeed = Convert.ToInt32(250.0),
                CurrentLateralMode = LateralModeType.HDG,
                CurrentThrustMode = ThrustModeType.SPEED,
                CurrentVerticalMode = VerticalModeType.FLCH
            };

            GroundHandler = new AircraftGroundHandler(this) { };

            ArtificialPilot = new ArtificialPilot(this) { };

            Fms = new AircraftFms(this, magTileManager);
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

        public void Start(SimSessionDetails details)
        {
            // Set initial assignments
            _sessionDetails = details;
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
            if (FsdConnectionDetails.ConvertFrequency(e.Frequency) == _sessionDetails.ConnectionDetails?.CommandFrequency && e.Message.StartsWith($"{Callsign}, "))
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
            
            // Start Position Update Thread
            _shouldUpdatePosition = true;
            _posUpdThread = new Thread(AircraftPositionWorker);
            _posUpdThread.Name = $"{Callsign} Position Worker";
            _posUpdThread.Start();

            // Connect to FSD Server
            if (_sessionDetails.SessionType == SimSessionType.FSD)
            {
                InitializeFsdConnection();
            }
        }

        private void InitializeFsdConnection()
        {
            if (_sessionDetails.ConnectionDetails == null ||
                _sessionDetails.ClientInfo == null)
            {
                return;
            }
            
            Connection = new Connection();
            Connection.Connected += OnConnectionEstablished;
            Connection.Disconnected += OnConnectionTerminated;
            Connection.FrequencyMessageReceived += OnFrequencyMessageReceived;
            Connection.PrivateMessageReceived += OnPrivateMessageReceived;

            Connection.Connect(
                _sessionDetails.ClientInfo.Value,
                _sessionDetails.ConnectionDetails.Value.ToLoginInfo(Callsign),
                GetFsdPilotPosition(),
                Data.AircraftConfig,
                new PlaneInfo(AircraftType, AirlineCode));
            ConnectionStatus = ConnectionStatusType.CONNECTING;
        }

        private void OnConnectionEstablished(object sender, EventArgs e)
        {
            ConnectionStatus = ConnectionStatusType.CONNECTED;

            // Send Flight Plan
            Connection.SendFlightPlan(_flightPlan);

            // Stick to Ground
            Connection.SetOnGround(Position.OnGround);

            // Set Flap, Gear and Spoiler Position
            var flapsPct = (double)Data.Config / PerformanceData.ConfigList.Count;
            Connection.SetFlapsPct((int)(flapsPct * 100.0));
            Connection.SetGearDown(PerformanceData.ConfigList[Data.Config].GearDown);
            Connection.SetSpoilersDeployed(Data.SpeedBrakePos > 0);

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
        }
        private void HandleInFlight()
        {
            // Update FMS
            Fms.OnPositionUpdate((int)(PositionUpdateInterval * (_simRate / 10.0)));

            // Run Config Handler
            ArtificialPilot.OnPositionUpdate((int)(PositionUpdateInterval * (_simRate / 10.0)));

            // Run Autopilot
            Autopilot.OnPositionUpdate((int)(PositionUpdateInterval * (_simRate / 10.0)));

            // TODO: Update Mass

            // Move Aircraft
            MoveAircraft((int)(PositionUpdateInterval * (_simRate / 10.0)));
        }
        private void HandleOnGround()
        {
            // Run ground handler
            GroundHandler.OnPositionUpdate((int)(PositionUpdateInterval * (_simRate / 10.0)));

            // TODO: Update Mass

            // Move aircraft
            MoveAircraftOnGround((int)(PositionUpdateInterval * (_simRate / 10.0)));
        }
        private void AircraftPositionWorker()
        {
            while (_shouldUpdatePosition && CancelToken.IsCancellationRequested == false)
            {
                // Check calculation step time
                _lagTimer.Restart();

                // Calculate position
                if (!_paused)
                {
                    if (FlightPhase == FlightPhaseType.IN_FLIGHT)
                    {
                        //If we're on APCH below 50ft afe then switch flight phase to ground
                        if (Autopilot.CurrentVerticalMode == VerticalModeType.LAND &&
                            (Position.TrueAltitude < RelaventAirport.Elevation + Length.FromFeet(1)))
                        {
                            FlightPhase = FlightPhaseType.ON_GROUND;

                            GroundHandler.GroundPhase = GroundPhaseType.LAND;
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
                    ArtificialPilot.AircraftLights();

                    // Update Grib Data
                    UpdateGribPoint();

                    // Update FSD
                    if (_sessionDetails.SessionType == SimSessionType.FSD && Connection.IsConnected)
                    {
                        Connection.UpdatePosition(GetFsdPilotPosition());
                    }

                    Task.Run(() => PositionUpdated?.Invoke(this, new AircraftPositionUpdateEventArgs(DateTime.UtcNow, this)), CancelToken);
                }

                // Remove calculation time from position calculation rate
                int sleepTime = PositionUpdateInterval - (int)_lagTimer.ElapsedMilliseconds;

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

        private PilotPosition GetFsdPilotPosition()
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
                    if (_posUpdThread != null && _posUpdThread.IsAlive)
                    {
                        _posUpdThread.Join();
                    }

                    // Stop delay timer
                    Connection?.Dispose();
                    _delayTimer?.Stop();
                    _delayTimer?.Dispose();
                }

                Connection = null;
                _posUpdThread = null;
                Position = null;
                Autopilot = null;
                Fms = null;
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