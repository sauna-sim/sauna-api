using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    public static class SimAircraftHandler
    {
        private static List<SimAircraft> _aircrafts;
        private static object _aircraftsLock = new object();
        private static bool _allPaused = true;
        private static int _simRate = 10;

        public static event EventHandler<SimStateChangedEventArgs> GlobalSimStateChanged;
        public static event EventHandler<AircraftPositionUpdateEventArgs> AircraftCreated;
        public static event EventHandler<AircraftDeletedEventArgs> AircraftDeleted;
        public static event EventHandler<AircraftPositionUpdateEventArgs> AircraftPositionChanged;
        public static event EventHandler<AircraftConnectionStatusChangedEventArgs> AircraftConnectionStatusChanged;
        public static event EventHandler<AircraftSimStateChangedEventArgs> AircraftSimStateChanged;


        static SimAircraftHandler()
        {
            lock (_aircraftsLock)
            {
                _aircrafts = new List<SimAircraft>();
            }
        }

        public static void PerformOnAircraft(Action<List<SimAircraft>> action)
        {
            lock (_aircraftsLock)
            {
                action(_aircrafts);
            }
        }

        public static double SimRate
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
                lock (_aircraftsLock)
                {
                    foreach (SimAircraft aircraft in _aircrafts)
                    {
                        aircraft.SimRate = _simRate;
                    }
                }
                Task.Run(() => GlobalSimStateChanged?.Invoke(null, new SimStateChangedEventArgs(AllPaused, value)));
            }
        }

        public static bool AllPaused {
            get => _allPaused;
            set {
                _allPaused = value;
                lock (_aircraftsLock)
                {
                    foreach (SimAircraft aircraft in _aircrafts)
                    {
                        aircraft.Paused = _allPaused;
                    }
                }
                Task.Run(() => GlobalSimStateChanged?.Invoke(null, new SimStateChangedEventArgs(value, SimRate)));
            }
        }

        public static void RemoveAircraftByCallsign(string callsign)
        {
            SimAircraft foundAcft = null;
            lock (_aircraftsLock)
            {
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
            }

            foundAcft?.Dispose();
        }

        public static bool AddAircraft(SimAircraft aircraft)
        {
            lock (_aircraftsLock)
            {
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
            }
            return true;
        }

        private static void Aircraft_SimStateChanged(object sender, AircraftSimStateChangedEventArgs e)
        {
            Task.Run(() => AircraftSimStateChanged?.Invoke(sender, e));
        }

        private static void Aircraft_PositionUpdated(object sender, AircraftPositionUpdateEventArgs e)
        {
            Task.Run(() => AircraftPositionChanged?.Invoke(sender, e));
        }

        private static void Aircraft_ConnectionStatusChanged(object sender, AircraftConnectionStatusChangedEventArgs e)
        {
            Task.Run(() => AircraftConnectionStatusChanged?.Invoke(sender, e));
        }

        public static SimAircraft GetAircraftWhichContainsCallsign(string callsignMatch)
        {
            lock (_aircraftsLock)
            {
                foreach (SimAircraft aircraft in _aircrafts)
                {
                    if (aircraft.Callsign.ToLower().Contains(callsignMatch.ToLower()))
                    {
                        return aircraft;
                    }
                }
            }

            return null;
        }

        public static SimAircraft GetAircraftByCallsign(string callsign)
        {
            lock (_aircraftsLock)
            {
                foreach (SimAircraft aircraft in _aircrafts)
                {
                    if (aircraft.Callsign.ToLower().Equals(callsign.ToLower()))
                    {
                        return aircraft;
                    }
                }
            }

            return null;
        }

        public static void DeleteAllAircraft()
        {
            lock (_aircraftsLock)
            {
                foreach (SimAircraft aircraft in _aircrafts)
                {
                    Task.Run(() => AircraftDeleted?.Invoke(null, new AircraftDeletedEventArgs(aircraft.Callsign)));
                    aircraft.Dispose();
                }
                _aircrafts.Clear();
            }
        }
    }
}
