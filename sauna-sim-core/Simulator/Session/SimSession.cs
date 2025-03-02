using System;
using System.Threading;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Commands;

namespace SaunaSim.Core.Simulator.Session
{
    
    /// <summary>
    /// A Sauna Simulator Session.
    /// </summary>
    public class SimSession : IDisposable
    {
        private SimSessionDetails _details;
        public SimSessionDetails Details
        {
            get => _details;
            set
            {
                _details = value;
                AircraftHandler.SessionDetails = _details;
            }
        }
        
        public CancellationTokenSource SessionCancel { get; } 
        
        public SimAircraftHandler AircraftHandler { get; }
        
        public CommandHandler CommandHandler { get; }

        public SimSession(
            SimSessionDetails details,
            string magCofFile,
            string gribTilePath,
            Action<string, int> logger, 
            CancellationToken externalCancel)
        {
            SessionCancel = new CancellationTokenSource();
            _details = details;
            AircraftHandler = new SimAircraftHandler(details, magCofFile, gribTilePath, logger, externalCancel);
            CommandHandler = new CommandHandler(AircraftHandler);
        }

        ~SimSession()
        {
            Dispose(false);
        }
        
        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                SessionCancel?.Dispose();
                AircraftHandler?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}