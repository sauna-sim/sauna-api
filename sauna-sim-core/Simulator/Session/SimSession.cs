using System;
using System.Threading;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Commands;

namespace SaunaSim.Core.Simulator.Session
{
    
    /// <summary>
    /// A Sauna Simulator Session.
    /// </summary>
    public class SimSession
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
        
        public SimAircraftHandler AircraftHandler { get; }
        
        public CommandHandler CommandHandler { get; }

        public SimSession(
            SimSessionDetails details,
            string magCofFile,
            string gribTilePath,
            Action<string, int> logger, 
            CancellationToken cancelToken)
        {
            _details = details;
            AircraftHandler = new SimAircraftHandler(details, magCofFile, gribTilePath, logger, cancelToken);
            CommandHandler = new CommandHandler(AircraftHandler);
        }
    }
}