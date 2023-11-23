using FsdConnectorNet;
using FsdConnectorNet.Args;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft.Control;
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


namespace SaunaSim.Core.Simulator.Aircraft
{
    public enum ConstraintType
    {
        FREE = -2,
        LESS = -1,
        EXACT = 0,
        MORE = 1
    }

    public class SimAircraft : IDisposable
    {
        private Thread _posUpdThread;
        private PauseableTimer _delayTimer;
        private bool disposedValue;
        private bool _shouldUpdatePosition = false;
        private ClientInfo _clientInfo;

        // Connection Info
        public LoginInfo LoginInfo { get; private set; }

        public string Callsign => LoginInfo.callsign;

        public Connection Connection { get; private set; }

        public ConnectionStatusType ConnectionStatus { get; private set; } = ConnectionStatusType.WAITING;

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
            }
        }

        // Aircraft Info
        private AircraftPosition _position;
        public AircraftPosition Position => _position;

        private AircraftAutopilot _autopilot;
        public AircraftAutopilot Autopilot => _autopilot;

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
            get => _flightPlan;
            set
            {
                _flightPlan = value;
                if (ConnectionStatus == ConnectionStatusType.CONNECTED)
                {
                    Connection.SendFlightPlan(value);
                }
            }
        }

        public FlightPhaseType FlightPhase { get; set; }

        // Loggers
        public Action<string> LogInfo { get; set; }

        public Action<string> LogWarn { get; set; }

        public Action<string> LogError { get; set; }


        // Assigned values
        //public AircraftControl Control { get; private set; }
        //public int Assigned_IAS { get; set; } = -1;
        //public ConstraintType Assigned_IAS_Type { get; set; } = ConstraintType.FREE;


        public SimAircraft(string callsign, string networkId, string password, string fullname, string hostname, ushort port, ProtocolRevision protocol, ClientInfo clientInfo,
            PerfData perfData, double lat, double lon, double alt, double hdg_mag, int delayMs = 0)
        {
            LoginInfo = new LoginInfo(networkId, password, callsign, fullname, PilotRatingType.Student, hostname, protocol, AppSettingsManager.CommandFrequency, port);
            _clientInfo = clientInfo;
            Connection = new Connection();
            Connection.Connected += OnConnectionEstablished;
            Connection.Disconnected += OnConnectionTerminated;
            Connection.FrequencyMessageReceived += OnFrequencyMessageReceived;

            _simRate = 10;
            _paused = true;
            _position = new AircraftPosition(lat, lon, alt)
            {
                Pitch = 2.5,
                Bank = 0,
                IndicatedAirSpeed = 250.0,
                Heading_Mag = hdg_mag
            };

            FlightPhase = FlightPhaseType.ENROUTE;

            _data = new AircraftData()
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

            _fms = new AircraftFms(this);
            PerformanceData = perfData;
            // Control = new AircraftControl(new HeadingHoldInstruction(Convert.ToInt32(hdg_mag)), new AltitudeHoldInstruction(Convert.ToInt32(alt)));
            DelayMs = delayMs;

            AircraftType = "A320"; // TODO: Change This
            AirlineCode = "JBU"; // TODO: Change This
        }

        public void Start()
        {
            // Set initial assignments
            Position.UpdateGribPoint();

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

                    split = CommandHandler.HandleCommand(command, this, split, (string msg) =>
                    {
                        string returnMsg = msg.Replace($"{Callsign} ", "");
                        Connection.SendFrequencyMessage(e.Frequency, returnMsg);
                    });
                }
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
            Connection.SendFlightPlan(FlightPlan);
        }

        private void OnConnectionTerminated(object sender, EventArgs e)
        {
            ConnectionStatus = ConnectionStatusType.DISCONNECTED;
            _shouldUpdatePosition = false;
            _delayTimer?.Stop();
        }

        private void AircraftPositionWorker()
        {
            while (_shouldUpdatePosition)
            {
                // Calculate position
                if (!_paused)
                {
                    // Update FMS
                    _fms.OnPositionUpdate((int)(AppSettingsManager.PosCalcRate * (_simRate / 10.0)));

                    // Run Autopilot
                    _autopilot.OnPositionUpdate((int)(AppSettingsManager.PosCalcRate * (_simRate / 10.0)));

                    // TODO: Update Mass

                    // Move Aircraft
                    MoveAircraft((int)(AppSettingsManager.PosCalcRate * (_simRate / 10.0)));

                    // Update Grib Data
                    Position.UpdateGribPoint();

                    // Update FSD
                    Connection.UpdatePosition(GetFsdPilotPosition());
                }

                Thread.Sleep(AppSettingsManager.PosCalcRate);
            }
        }

        private void MoveAircraft(int intervalMs)
        {
            double t = intervalMs / 1000.0;

            // Calculate Pitch, Bank, and Thrust Lever Position
            Position.Pitch += PerfDataHandler.CalculateDisplacement(Position.PitchRate, 0, t);
            Position.Bank += PerfDataHandler.CalculateDisplacement(Position.BankRate, 0, t);
            Data.ThrustLeverPos += PerfDataHandler.CalculateDisplacement(Data.ThrustLeverVel, 0, t);

            // Calculate Performance Values
            (double accelFwd, double vs) = PerfDataHandler.CalculatePerformance(PerformanceData, Position.Pitch, Data.ThrustLeverPos / 100.0, Position.IndicatedAirSpeed,
                Position.DensityAltitude, Data.Mass_kg, Data.SpeedBrakePos, Data.Config);

            // Calculate New Velocities
            double curGs = Position.GroundSpeed;
            Position.IndicatedAirSpeed = MathUtil.ConvertMpersToKts(PerfDataHandler.CalculateFinalVelocity(
                MathUtil.ConvertKtsToMpers(Position.IndicatedAirSpeed), MathUtil.ConvertKtsToMpers(accelFwd), t));
            Position.VerticalSpeed = vs;

            // Calculate Accelerations
            Position.Forward_Acceleration = accelFwd;

            // Calculate Displacement
            double displacement = 0.5 * (MathUtil.ConvertKtsToMpers(Position.GroundSpeed + curGs)) * t;
            double distanceTravelledNMi = MathUtil.ConvertMetersToNauticalMiles(displacement);

            // Calculate Position
            if (Math.Abs(Position.Bank) < double.Epsilon)
            {
                GeoPoint point = new GeoPoint(Position.PositionGeoPoint);
                point.MoveByNMi(Position.Track_True, distanceTravelledNMi);
                Position.Latitude = point.Lat;
                Position.Longitude = point.Lon;
            }
            else
            {
                // Calculate radius of turn
                double radiusOfTurn = GeoUtil.CalculateRadiusOfTurn(Math.Abs(Position.Bank), Position.GroundSpeed);

                // Calculate degrees to turn
                double degreesToTurn = GeoUtil.CalculateDegreesTurned(distanceTravelledNMi, radiusOfTurn);

                // Figure out turn direction
                bool isRightTurn = Position.Bank > 0;

                // Calculate end heading
                double endHeading = GeoUtil.CalculateEndHeading(Position.Heading_Mag, degreesToTurn, isRightTurn);

                // Calculate chord line data
                Tuple<double, double> chordLine = GeoUtil.CalculateChordHeadingAndDistance(Position.Heading_Mag, degreesToTurn, radiusOfTurn, isRightTurn);

                // Calculate new position
                Position.Heading_Mag = chordLine.Item1;
                GeoPoint point = new GeoPoint(Position.PositionGeoPoint);
                point.MoveByNMi(Position.Track_True, distanceTravelledNMi);
                Position.Latitude = point.Lat;
                Position.Longitude = point.Lon;
                Position.Heading_Mag = endHeading;
            }

            // Calculate Altitude
            Position.IndicatedAltitude += Position.VerticalSpeed * t / 60;
        }

        public PilotPosition GetFsdPilotPosition()
        {
            return new PilotPosition(XpdrMode, (ushort)Squawk, Position.Latitude, Position.Longitude, Position.TrueAltitude, Position.TrueAltitude,
                Position.PressureAltitude, Position.GroundSpeed, Position.Pitch, Position.Bank, Position.Heading_True, Position.OnGround, Position.Velocity_X_MPerS, Position.Velocity_Y_MPerS,
                Position.Velocity_Z_MPerS, Position.Pitch_Velocity_RadPerS, Position.Heading_Velocity_RadPerS, Position.Bank_Velocity_RadPerS);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Connection.Dispose();
                    _shouldUpdatePosition = false;
                    _posUpdThread?.Join();
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