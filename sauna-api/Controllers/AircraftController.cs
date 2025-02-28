using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaunaSim.Api.ApiObjects.Aircraft;
using SaunaSim.Api.Services;
using SaunaSim.Core.Data.Loaders;
using SaunaSim.Core.Simulator.Aircraft;

namespace SaunaSim.Api.Controllers
{
    [ApiController]
    [Route("api/session/{sessionId}/aircraft")]
    public class AircraftController : ControllerBase
    {
        private readonly ILogger<DataController> _logger;
        private readonly ISaunaSessionService _sessionService;
        private readonly IHostApplicationLifetime _applicationLifetime;

        public AircraftController(ILogger<DataController> logger, ISaunaSessionService sessionService, IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _sessionService = sessionService;
            _applicationLifetime = appLifetime;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftResponse> CreateAircraft(CreateAircraftRequest request, string sessionId)
        {
            if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
            {
                return BadRequest("Session Not Found");
            }
            
            try
            {
                AircraftBuilder builder = new AircraftBuilder(
                    request.Callsign,
                    sessionContainer.Session.AircraftHandler.MagTileManager,
                    sessionContainer.Session.AircraftHandler.GribTileManager,
                    sessionContainer.Session.CommandHandler)
                {
                    Position = new GeoPoint(request.Position.Latitude,
                    request.Position.Longitude,
                    request.Position.IndicatedAltitude),
                    HeadingMag = Bearing.FromDegrees(request.Position.MagneticHeading),
                    LogInfo = (string msg) =>
                    {
                        _logger.LogInformation($"{request.Callsign}: {msg}");
                    },
                    LogWarn = (string msg) =>
                    {
                        _logger.LogWarning($"{request.Callsign}: {msg}");
                    },
                    LogError = (string msg) =>
                    {
                        _logger.LogError($"{request.Callsign}: {msg}");
                    },
                    XpdrMode = request.TransponderMode,
                    Squawk = request.Squawk,
                    Speed = request.Position.IndicatedSpeed,
                    IsSpeedMach = request.Position.IsMachNumber
                };

                // TODO: VERY IMPORTANT this parameter is no longer needed!!!!! or used!!!!!!
                if (request.FmsWaypointList != null)
                {
                    //builder.FmsWaypoints = request.FmsWaypointList;
                }

                var pilot = builder.Create();
                sessionContainer.Session.AircraftHandler.AddAircraft(pilot);

                return Ok(new AircraftResponse(pilot, true));
            } catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("all/{fms:bool?}")]
        public ActionResult<List<AircraftResponse>> GetAllAircraft(bool? fms, string sessionId)
        {
            if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
            {
                return BadRequest("Session Not Found");
            }
            
            List<AircraftResponse> pilots = new List<AircraftResponse>();
            try
            {
                sessionContainer.Session.AircraftHandler.SimAircraftListLock.WaitOne();

                foreach (var pilot in sessionContainer.Session.AircraftHandler.SimAircraftList)
                {
                    pilots.Add(new AircraftResponse(pilot, fms.HasValue && fms.Value));
                }
            }
            finally
            {
                sessionContainer.Session.AircraftHandler.SimAircraftListLock.ReleaseMutex();
            }

            return pilots;
        }

        [HttpGet("{callsign}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftResponse> GetAircraftByCallsign(string callsign, string sessionId)
        {
            if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
            {
                return BadRequest("Session Not Found");
            }
            
            SimAircraft client = sessionContainer.Session.AircraftHandler.GetAircraftByCallsign(callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            return Ok(new AircraftResponse(client, true));
        }

        [HttpGet("{callsign}/websocket")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task WebSocketForCallsign(string callsign, string sessionId)
        {
            if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            
            if (sessionContainer.Session.AircraftHandler.GetAircraftByCallsign(callsign) == null)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            } else if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    await sessionContainer.WebSocketHandler.HandleAircraftSocket(callsign, webSocket, _applicationLifetime.ApplicationStopping);
                } catch (Exception e)
                {
                    _logger.LogWarning($"Websocket connection failed: {e.Message}");
                }
            } else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        [HttpGet("byPartialCallsign/{callsign}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftResponse> GetAircraftByPartialCallsign(string callsign, string sessionId)
        {
            if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
            {
                return BadRequest("Session Not Found");
            }
            
            SimAircraft client = sessionContainer.Session.AircraftHandler.GetAircraftWhichContainsCallsign(callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            return Ok(new AircraftResponse(client, true));
        }

        [HttpPost("{callsign}/unpause")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftStateRequestResponse> UnpauseAircraft(string callsign, string sessionId)
        {
            if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
            {
                return BadRequest("Session Not Found");
            }
            
            SimAircraft client = sessionContainer.Session.AircraftHandler.GetAircraftByCallsign(callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            client.Paused = false;

            return Ok(new AircraftStateRequestResponse
            {
                Paused = client.Paused,
                SimRate = client.SimRate / 10.0
            });
        }

        [HttpPost("{callsign}/pause")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftStateRequestResponse> PauseAircraft(string callsign, string sessionId)
        {
            if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
            {
                return BadRequest("Session Not Found");
            }
            
            SimAircraft client = sessionContainer.Session.AircraftHandler.GetAircraftByCallsign(callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            client.Paused = true;

            return Ok(new AircraftStateRequestResponse
            {
                Paused = client.Paused,
                SimRate = client.SimRate / 10.0
            });
        }

        [HttpPost("{callsign}/simrate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftStateRequestResponse> SetAircraftSimRate(string callsign, AircraftStateRequestResponse request, string sessionId)
        {
            if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
            {
                return BadRequest("Session Not Found");
            }
            
            SimAircraft client = sessionContainer.Session.AircraftHandler.GetAircraftByCallsign(callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            client.SimRate = (int)(request.SimRate * 10);

            return Ok(new AircraftStateRequestResponse
            {
                Paused = client.Paused,
                SimRate = client.SimRate / 10.0
            });
        }

        [HttpGet("{callsign}/simState")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftStateRequestResponse> GetAircraftSimState(string callsign, string sessionId)
        {
            if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
            {
                return BadRequest("Session Not Found");
            }
            
            SimAircraft client = sessionContainer.Session.AircraftHandler.GetAircraftByCallsign(callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            return Ok(new AircraftStateRequestResponse
            {
                Paused = sessionContainer.Session.AircraftHandler.AllPaused,
                SimRate = sessionContainer.Session.AircraftHandler.SimRate
            });
        }

        [HttpDelete("{callsign}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftResponse> RemoveAircraftByCallsign(string callsign, string sessionId)
        {
            if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
            {
                return BadRequest("Session Not Found");
            }
            
            SimAircraft client = sessionContainer.Session.AircraftHandler.GetAircraftByCallsign(callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            sessionContainer.Session.AircraftHandler.RemoveAircraftByCallsign(callsign);

            return Ok(new AircraftResponse(client, true));
        }

        [HttpDelete("all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult RemoveAll(string sessionId)
        {
            if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
            {
                return BadRequest("Session Not Found");
            }
            
            sessionContainer.Session.AircraftHandler.DeleteAllAircraft();
            return Ok();
        }
    }
}
