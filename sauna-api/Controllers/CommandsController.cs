using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SaunaSim.Api.ApiObjects.Commands;
using SaunaSim.Api.Services;
using SaunaSim.Core.Simulator.Aircraft;

namespace SaunaSim.Api.Controllers
{
    [ApiController]
    [Route("api/session/{sessionId}/commands")]
    public class CommandsController : ControllerBase
    {
        private readonly ISaunaSessionService _sessionService;
        private readonly ILogger<DataController> _logger;

        public CommandsController(ILogger<DataController> logger, ISaunaSessionService sessionService)
        {
            _logger = logger;
            _sessionService = sessionService;
        }

        private void LogCommandInfo(SaunaSessionContainer sessionContainer, string msg)
        {
            try
            {
                sessionContainer.CommandsBufferLock.WaitOne();
                sessionContainer.CommandsBuffer.Add(msg);
            }
            finally
            {
                sessionContainer.CommandsBufferLock.ReleaseMutex();
            }

            sessionContainer.WebSocketHandler.SendCommandMsg(msg).ConfigureAwait(false);
        }

        [HttpGet("buffer")]
        public ActionResult<List<string>> GetCommandBuffer(string sessionId)
        {
            if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
            {
                return BadRequest("Session Not Found");
            }
            
            List<string> log;
            try
            {
                sessionContainer.CommandsBufferLock.WaitOne();
                log = [..sessionContainer.CommandsBuffer];
                sessionContainer.CommandsBuffer.Clear();
            }
            finally
            {
                sessionContainer.CommandsBufferLock.ReleaseMutex();
            }

            return Ok(log);
        }

        [HttpPost("send/textCommand")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendTextCommand(TextCommandRequest request, string sessionId)
        {
            if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
            {
                return BadRequest("Session Not Found");
            }
            
            SimAircraft client = sessionContainer.Session.AircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;
            var remainingArgs = sessionContainer.Session.CommandHandler.HandleCommand(request.Command, aircraft, request.Args, (msg) => LogCommandInfo(sessionContainer, msg));
            while (remainingArgs.Count > 0)
            {
                // Get command name
                string command = remainingArgs[0].ToLower();
                remainingArgs.RemoveAt(0);

                remainingArgs = sessionContainer.Session.CommandHandler.HandleCommand(command, aircraft, remainingArgs, (msg) => LogCommandInfo(sessionContainer, msg));
            }
            return Ok();
        }
    }
}
