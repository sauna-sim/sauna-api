using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SaunaSim.Core.Simulator.Aircraft;

namespace SaunaSim.Api.Services
{
	public class SimAircraftService : ISimAircraftService
	{
		public SimAircraftHandler Handler { get; private set; }

		public Mutex AircraftListLock => Handler.SimAircraftListLock;

		public List<SimAircraft> AircraftList => Handler.SimAircraftList;

        public SimAircraftService()
		{
			Handler = new SimAircraftHandler(
				Path.Join(AppDomain.CurrentDomain.BaseDirectory, "magnetic", "WMM.COF"),
				Path.Join(Path.GetTempPath(), "sauna-api", "grib-tiles")
			);
		}
	}
}

