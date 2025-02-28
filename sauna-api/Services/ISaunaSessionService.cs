using System;
using System.Collections.Generic;
using System.Threading;
using SaunaSim.Api.WebSockets;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Commands;
using SaunaSim.Core.Simulator.Session;

namespace SaunaSim.Api.Services
{
	public interface ISaunaSessionService
	{
		public Dictionary<string, SaunaSessionContainer> Sessions {get;}

		public string CreateSession(SimSessionDetails details);
		public SaunaSessionContainer GetSession(string sessionId);
		public bool RemoveSession(string sessionId);
	}
}

