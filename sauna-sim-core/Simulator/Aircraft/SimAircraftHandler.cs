using AviationCalcUtilNet.Atmos.Grib;
using AviationCalcUtilNet.Magnetic;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SaunaSim.Core.Simulator.Aircraft
{
    public class SimStateChangedEventArgs : EventArgs
    {
        public bool AllPaused { get; set; }
        public double SimRate { get; set; }

        public SimStateChangedEventArgs(bool allPaused, double simRate)
        {
            AllPaused = allPaused;
            SimRate = simRate;
        }
    }

    public class AircraftDeletedEventArgs : EventArgs
    {
        public string Callsign { get; set; }

        public AircraftDeletedEventArgs(string callsign)
        {
            Callsign = callsign;
        }
    }

    public class SimAircraftHandler : IDisposable
    {
        private List<SimAircraft> _aircrafts;
        private Mutex _aircraftsLock;
        private bool _allPaused;
        private int _simRate;
        private bool disposedValue;
        private Action<string, int> _logger;

        public event EventHandler<SimStateChangedEventArgs> GlobalSimStateChanged;
        public event EventHandler<AircraftPositionUpdateEventArgs> AircraftCreated;
        public event EventHandler<AircraftDeletedEventArgs> AircraftDeleted;
        public event EventHandler<AircraftPositionUpdateEventArgs> AircraftPositionChanged;
        public event EventHandler<AircraftConnectionStatusChangedEventArgs> AircraftConnectionStatusChanged;
        public event EventHandler<AircraftSimStateChangedEventArgs> AircraftSimStateChanged;


        public SimAircraftHandler(string magCofFile, string gribTilePath, Action<string, int> logger)
        {
            _logger = logger;
            _allPaused = true;
            _simRate = 10;
            _aircraftsLock = new Mutex();
            _aircraftsLock.WaitOne();
            _aircrafts = new List<SimAircraft>();
            _aircraftsLock.ReleaseMutex();
            try
            {
                var model = MagneticModel.FromFile(magCofFile);
                _logger("Magnetic File Loaded", 0);
                MagTileManager = new MagneticTileManager(ref model);
            } catch (Exception e)
            {
                _logger("There was an error loading the WMM.COF file. Magnetic correction will NOT be applied.", 1);
                Console.WriteLine(e.Message);
                MagTileManager = new MagneticTileManager();
            }
            GribTileManager = new GribTileManager(gribTilePath);
        }

        public MagneticTileManager MagTileManager { get; private set; }

        public GribTileManager GribTileManager { get; private set; }

        public Mutex SimAircraftListLock => _aircraftsLock;

        public List<SimAircraft> SimAircraftList => _aircrafts;

        public double SimRate
        {
            get => _simRate / 10.0;
            set
            {
                if (value > 8.0)
                {
                    _simRate = 80;
                } else if (value < 0.1)
                {
                    _simRate = 1;
                } else
                {
                    _simRate = (int)(value * 10);
                }
                _aircraftsLock.WaitOne();
                foreach (SimAircraft aircraft in _aircrafts)
                {
                    aircraft.SimRate = _simRate;
                }
                _aircraftsLock.ReleaseMutex();
                Task.Run(() => GlobalSimStateChanged?.Invoke(null, new SimStateChangedEventArgs(AllPaused, value)));
            }
        }

        public bool AllPaused {
            get => _allPaused;
            set {
                _allPaused = value;
                _aircraftsLock.WaitOne();

                    foreach (SimAircraft aircraft in _aircrafts)
                    {
                        aircraft.Paused = _allPaused;
                    }
                _aircraftsLock.ReleaseMutex();
                Task.Run(() => GlobalSimStateChanged?.Invoke(null, new SimStateChangedEventArgs(value, SimRate)));
            }
        }

        public void RemoveAircraftByCallsign(string callsign)
        {
            SimAircraft foundAcft = null;
            _aircraftsLock.WaitOne();

                foreach (SimAircraft aircraft in _aircrafts)
                {
                    if (aircraft.Callsign.Equals(callsign))
                    {
                        foundAcft = aircraft;
                        _aircrafts.Remove(aircraft);
                        Task.Run(() => AircraftDeleted?.Invoke(null, new AircraftDeletedEventArgs(foundAcft.Callsign)));
                        break;
                    }
                }
            _aircraftsLock.ReleaseMutex();

            foundAcft?.Dispose();
        }

        public bool AddAircraft(SimAircraft aircraft)
        {
            _aircraftsLock.WaitOne();
            List<int> deletionList = new List<int>();

                for (int i = _aircrafts.Count - 1; i >= 0; i--)
                {
                    SimAircraft c = _aircrafts[i];
                    if (c.ConnectionStatus == ConnectionStatusType.DISCONNECTED || (c.Callsign.Equals(aircraft.Callsign) && c.ConnectionStatus == ConnectionStatusType.WAITING))
                    {
                        Task.Run(() => AircraftDeleted?.Invoke(null, new AircraftDeletedEventArgs(_aircrafts[i].Callsign)));
                        _aircrafts[i].Dispose();
                        _aircrafts.RemoveAt(i);
                    } else if (c.Callsign.Equals(aircraft.Callsign))
                    {
                        return false;
                    }
                }

                aircraft.Paused = _allPaused;
                aircraft.SimRate = _simRate;
                aircraft.ConnectionStatusChanged += Aircraft_ConnectionStatusChanged;
                aircraft.PositionUpdated += Aircraft_PositionUpdated;
                aircraft.SimStateChanged += Aircraft_SimStateChanged;
                _aircrafts.Add(aircraft);
                Task.Run(() => AircraftCreated?.Invoke(null, new AircraftPositionUpdateEventArgs(DateTime.UtcNow, aircraft)));
            _aircraftsLock.ReleaseMutex();
            return true;
        }

        private void Aircraft_SimStateChanged(object sender, AircraftSimStateChangedEventArgs e)
        {
            Task.Run(() => AircraftSimStateChanged?.Invoke(sender, e));
        }

        private void Aircraft_PositionUpdated(object sender, AircraftPositionUpdateEventArgs e)
        {
            Task.Run(() => AircraftPositionChanged?.Invoke(sender, e));
        }

        private void Aircraft_ConnectionStatusChanged(object sender, AircraftConnectionStatusChangedEventArgs e)
        {
            Task.Run(() => AircraftConnectionStatusChanged?.Invoke(sender, e));
        }

        public SimAircraft GetAircraftWhichContainsCallsign(string callsignMatch)
        {
            _aircraftsLock.WaitOne();
                foreach (SimAircraft aircraft in _aircrafts)
                {
                    if (aircraft.Callsign.ToLower().Contains(callsignMatch.ToLower()))
                    {
                        return aircraft;
                    }
                }
            _aircraftsLock.ReleaseMutex();

            return null;
        }

        public SimAircraft GetAircraftByCallsign(string callsign)
        {
            _aircraftsLock.WaitOne();
            foreach (SimAircraft aircraft in _aircrafts)
                {
                    if (aircraft.Callsign.ToLower().Equals(callsign.ToLower()))
                    {
                        return aircraft;
                    }
                }
            _aircraftsLock.ReleaseMutex();

            return null;
        }

        public void DeleteAllAircraft()
        {
            _aircraftsLock.WaitOne();
                foreach (SimAircraft aircraft in _aircrafts)
                {
                    Task.Run(() => AircraftDeleted?.Invoke(null, new AircraftDeletedEventArgs(aircraft.Callsign)));
                    aircraft.Dispose();
                }
                _aircrafts.Clear();
            _aircraftsLock.ReleaseMutex();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DeleteAllAircraft();
                }

                _aircraftsLock.WaitOne();
                _aircrafts = null;
                _aircraftsLock.ReleaseMutex();
                _aircraftsLock.Dispose();
                disposedValue = true;
            }
        }

        ~SimAircraftHandler()
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
