using System;
using System.Collections.Generic;
using System.Threading;
using SaunaSim.Api.WebSockets;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Commands;

namespace SaunaSim.Api.Services
{
	public interface ISimAircraftService
	{
		public SimAircraftHandler Handler {get;}

        public CommandHandler CommandHandler { get; }

        public Mutex AircraftListLock { get; }

		public List<SimAircraft> AircraftList { get; }

        public WebSocketHandler WebSocketHandler { get; }
    }
}

