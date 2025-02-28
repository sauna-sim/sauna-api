using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaunaSim.Api.Controllers;
using SaunaSim.Core.Simulator.Session;

namespace SaunaSim.Api.Services
{
	public class SaunaSessionService : ISaunaSessionService
	{
		private readonly ILogger<DataController> _logger;
		private readonly CancellationToken _cxToken;
		public Dictionary<string, SaunaSessionContainer> Sessions { get; }

        public SaunaSessionService(ILogger<DataController> logger, IHostApplicationLifetime appLifetime)
		{
			_logger = logger;
			_cxToken = appLifetime.ApplicationStopping;
			Sessions = new Dictionary<string, SaunaSessionContainer>();
		}

		private void LogFunc(string msg, int priority)
		{
			switch (priority)
			{
				case 0:
					_logger.LogInformation(msg);
					break;
				case 1:
					_logger.LogWarning(msg);
					break;
				case 2:
					_logger.LogError(msg);
					break;
			}
		}

		public string CreateSession(SimSessionDetails details)
		{
			string sessionId = Guid.NewGuid().ToString();
			Sessions.Add(sessionId, new SaunaSessionContainer(sessionId, details, LogFunc, _cxToken));
			return sessionId;
		}

		public SaunaSessionContainer GetSession(string sessionId)
		{
			return Sessions[sessionId];
		}

		public bool RemoveSession(string sessionId)
		{
			return Sessions.Remove(sessionId);
		}
	}
}

