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


    public enum ConnectionStatusType
    {
        WAITING,
        DISCONNECTED,
        CONNECTING,
        CONNECTED
    }

    public class SimAircraft : IDisposable
    {
        private Thread _posUpdThread;
        private PauseableTimer _delayTimer;
        private bool _paused;
        private string _flightPlan;
        private AircraftPosition _position;
        private bool disposedValue;
        private bool _shouldUpdatePosition = false;
        private ClientInfo _clientInfo;

        // Connection Info
        public LoginInfo LoginInfo { get; private set; }
        public string Callsign => LoginInfo.callsign;
        public ConnectionStatusType ConnectionStatus { get; private set; } = ConnectionStatusType.WAITING;
        public Connection Connection { get; private set; }

        // Aircraft Info
        public AircraftPosition Position
        {
            get => _position;
            set => _position = value;
        }

        public int Config { get; set; }
        public double ThrustLeverPos { get; set; }
        public double SpeedBrakePos { get; set; }
        public PerfData PerformanceData { get; set; }
        public double Mass_kg { get; set; }
        public TransponderModeType XpdrMode { get; set; }
        public int Squawk { get; set; }
        public int DelayMs { get; set; }
        public AircraftConfig AircraftConfig { get; set; }
        public string AircraftType { get; private set; }
        public string AirlineCode { get; private set; }

        // Loggers
        public Action<string> LogInfo { get; set; }
        public Action<string> LogWarn { get; set; }
        public Action<string> LogError { get; set; }

        // TODO: Convert to FlightPlan Struct/Object
        public string FlightPlan
        {
            get => _flightPlan;
            set
            {
                _flightPlan = value;
                if (ConnectionStatus == ConnectionStatusType.CONNECTED)
                {
                    // TODO: Send Flight Plan
                }
            }
        }

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

        // Assigned values
        public AircraftAutopilot Autopilot { get; private set; }
        public AircraftControl Control { get; private set; }
        public int Assigned_IAS { get; set; } = -1;
        public ConstraintType Assigned_IAS_Type { get; set; } = ConstraintType.FREE;


        public SimAircraft(string callsign, string networkId, string password, string fullname, string hostname, ushort port, ProtocolRevision protocol, ClientInfo clientInfo,
            PerfData perfData, double lat, double lon, double alt, double hdg_mag, int delayMs = 0)
        {
            LoginInfo = new LoginInfo(networkId, password, callsign, fullname, PilotRatingType.Student, hostname, protocol, AppSettingsManager.CommandFrequency, port);
            _clientInfo = clientInfo;
            Connection = new Connection();
            Connection.Connected += OnConnectionEstablished;
            Connection.Disconnected += OnConnectionTerminated;
            Connection.FrequencyMessageReceived += OnFrequencyMessageReceived;

            Paused = true;
            Position = new AircraftPosition
            {
                Latitude = lat,
                Longitude = lon,
                IndicatedAltitude = alt,
                Heading_Mag = hdg_mag
            };
            Autopilot = new AircraftAutopilot(this);
            PerformanceData = perfData;
            Control = new AircraftControl(new HeadingHoldInstruction(Convert.ToInt32(hdg_mag)), new AltitudeHoldInstruction(Convert.ToInt32(alt)));
            DelayMs = delayMs;
            AircraftConfig = new AircraftConfig(true, false, false, true, true, false, false, 0, false, false, new AircraftEngine(true, false), new AircraftEngine(true, false));
            _flightPlan = "";
            AircraftType = "A320";
            AirlineCode = "FFT";

            // TODO: Change This To Actually Calculate Mass
            Mass_kg = (perfData.MTOW_kg + perfData.OEW_kg) / 2;

            // TODO: Remove This
            Position.Pitch = 2.5;
            ThrustLeverPos = 0;
        }

        public void Start()
        {
            // Set initial assignments
            Position.UpdateGribPoint();

            // Connect if no delay, otherwise start timer
            if (DelayMs <= 0)
            {
                OnTimerElapsed(this, null);
            }
            else
            {
                _delayTimer = new PauseableTimer(DelayMs);
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
            Connection.Connect(_clientInfo, LoginInfo, GetFsdPilotPosition(), AircraftConfig, new PlaneInfo(AircraftType, AirlineCode));
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
            // TODO: Send Flight Plan
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
                if (!Paused)
                {
                    // TODO: Run Autopilot
                    // TODO: Remove
                    double spdDelta = Position.IndicatedAirSpeed - 230;
                    SpeedBrakePos = 0;
                    Config = 0;
                    ThrustLeverPos = 1;
                    Position.Bank = 0;
                    Mass_kg = PerformanceData.OEW_kg;
                    if (spdDelta > double.Epsilon)
                    {
                        if (Math.Abs(spdDelta) > 1)
                        {
                            Position.Pitch += 0.1;
                        }
                        else
                        {
                            Position.Pitch = PerfDataHandler.GetRequiredPitchForThrust(PerformanceData, ThrustLeverPos, 0, Position.IndicatedAirSpeed, Position.DensityAltitude,
                                Mass_kg, SpeedBrakePos, Config);
                        }
                    }
                    else if (spdDelta < double.Epsilon)
                    {
                        if (Math.Abs(spdDelta) > 1)
                        {
                            Position.Pitch -= 0.1;
                        }
                    }

                    // Calculate Performance Values
                    (double accelFwd, double vs) = PerfDataHandler.CalculatePerformance(PerformanceData, Position.Pitch, ThrustLeverPos, Position.IndicatedAirSpeed,
                        Position.DensityAltitude, Mass_kg, SpeedBrakePos, Config);

                    // Calculate New Velocities
                    double t = AppSettingsManager.PosCalcRate / 1000.0;
                    double curGs = Position.GroundSpeed;
                    Position.IndicatedAirSpeed = MathUtil.ConvertMpersToKts(PerfDataHandler.CalculateFinalVelocity(
                        MathUtil.ConvertKtsToMpers(Position.IndicatedAirSpeed), MathUtil.ConvertKtsToMpers(accelFwd), t));
                    Position.VerticalSpeed = vs;

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

                    /*
                    int slowDownKts = -2;
                    int speedUpKts = 5;

                    // Calculate Speed Change
                    if (Assigned_IAS != -1)
                    {
                        if (Assigned_IAS <= Position.IndicatedAirSpeed)
                        {
                            Position.IndicatedAirSpeed = Math.Max(Assigned_IAS, Position.IndicatedAirSpeed + (slowDownKts * AppSettingsManager.PosCalcRate / 1000.0));
                        }
                        else
                        {
                            Position.IndicatedAirSpeed = Math.Min(Assigned_IAS, Position.IndicatedAirSpeed + (speedUpKts * AppSettingsManager.PosCalcRate / 1000.0));
                        }
                    }

                    Control.UpdatePosition(ref _position, AppSettingsManager.PosCalcRate);*/

                    // Recalculate values
                    Position.UpdateGribPoint();
                    
                    // Update FSD Position
                    Connection.UpdatePosition(GetFsdPilotPosition());
                }

                Thread.Sleep(AppSettingsManager.PosCalcRate);
            }
        }

        public PilotPosition GetFsdPilotPosition()
        {
            return new PilotPosition(XpdrMode, (ushort)Squawk, Position.Latitude, Position.Longitude, Position.AbsoluteAltitude, Position.AbsoluteAltitude,
                Position.PressureAltitude, Position.GroundSpeed, Position.Pitch, Position.Bank, Position.Heading_True, false, Position.Velocity_X_MPerS, Position.Velocity_Y_MPerS,
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
                Position = null;
                Control = null;
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