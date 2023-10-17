using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace SaunaSim.Core
{
    public class PauseableTimer : IDisposable
    {
        private readonly Timer _timer;
        private double _timeMs;
        private double _timeLeftMs;
        private DateTime _started;
        private bool _running;
        private int _rate; // Rate is a percent

        public event ElapsedEventHandler Elapsed;

        public PauseableTimer(int timeMs, int rate = 100)
        {
            RatePercent = rate;
            _timeMs = timeMs;
            _timeLeftMs = _timeMs;
            _timer = new Timer
            {
                Interval = _timeMs / (_rate / 100.0)
            };
            _timer.Elapsed += _timer_Elapsed;
            _running = false;
        }

        public bool AutoReset
        {
            get => _timer.AutoReset;
            set => _timer.AutoReset = value;
        }

        public int RatePercent
        {
            get => _rate;
            set
            {
                bool tempRunning = _running;
                if (tempRunning)
                {
                    Pause();
                }
                if (value < 1)
                {
                    _rate = 1;
                } else
                {
                    _rate = value;
                }

                if (tempRunning)
                {
                    Start();
                }
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Elapsed?.Invoke(sender, e);
        }

        public void Start()
        {
            if (!_running)
            {
                _started = DateTime.UtcNow;
                if (_timeLeftMs < 0)
                {
                    _timeLeftMs = _timeMs;
                }
                _timer.Interval = _timeLeftMs / (_rate / 100.0);
                _timer.Start();
                _running = true;
            }
        }

        public void Pause()
        {
            if (_running)
            {
                _timer.Stop();
                _running = false;
                _timeLeftMs -= (DateTime.UtcNow - _started).TotalMilliseconds * (_rate / 100.0);
            }
        }

        public void Stop()
        {
            _timer.Stop();
            _running = false;
            _timeLeftMs = _timeMs;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public int TimeRemainingMs()
        {
            if (!_running)
            {
                return (int) _timeLeftMs;
            } else
            {
                return (int) (_timeLeftMs - ((DateTime.UtcNow - _started).TotalMilliseconds * (_rate / 100.0)));
            }
        }
    }
}
