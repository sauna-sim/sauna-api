using System;
using System.Collections.Generic;
using System.Threading;
using SaunaSim.Core.Simulator.Aircraft;

namespace SaunaSim.Api.Services
{
	public interface ISimAircraftService
	{
		public SimAircraftHandler Handler {get;}

		public Mutex AircraftListLock { get; }

		public List<SimAircraft> AircraftList { get; }
	}
}

