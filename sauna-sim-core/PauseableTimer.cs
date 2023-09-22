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

        public event ElapsedEventHandler Elapsed;

        public PauseableTimer(int timeMs)
        {
            _timeMs = timeMs;
            _timeLeftMs = _timeMs;
            _timer = new Timer
            {
                Interval = _timeMs
            };
            _timer.Elapsed += _timer_Elapsed;
        }

        public bool AutoReset
        {
            get => _timer.AutoReset;
            set => _timer.AutoReset = value;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Elapsed?.Invoke(sender, e);
        }

        public void Start()
        {
            _started = DateTime.UtcNow;
            if (_timeLeftMs < 0)
            {
                _timeLeftMs = _timeMs;
            }
            _timer.Interval = _timeLeftMs;
            _timer.Start();
            _running = true;
        }

        public void Pause()
        {
            _timer.Stop();
            _running = false;
            _timeLeftMs = _timeMs - (DateTime.UtcNow - _started).TotalMilliseconds;
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
                return (int) (_timeMs - (DateTime.UtcNow - _started).TotalMilliseconds);
            }
        }
    }
}
