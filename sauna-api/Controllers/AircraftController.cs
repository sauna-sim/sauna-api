using SaunaSim.Api.ApiObjects.Aircraft;
using SaunaSim.Core;
using SaunaSim.Core.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaunaSim.Core.Simulator.Aircraft;
using System.Runtime.CompilerServices;
using SaunaSim.Api.Utilities;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;
using SaunaSim.Core.Simulator.Aircraft.Performance;
using NavData_Interface.Objects.Fix;
using SaunaSim.Core.Data.Loaders;

namespace SaunaSim.Api.Controllers
{
    [ApiController]
    [Route("api/aircraft")]
    public class AircraftController : ControllerBase
    {

        private readonly ILogger<DataController> _logger;

        public AircraftController(ILogger<DataController> logger)
        {
            _logger = logger;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftResponse> CreateAircraft(CreateAircraftRequest request) // reuses code from DataController.LoadEuroScopeScenario. Both should use the same function.
        {
            try
            {
                AircraftBuilder builder = new AircraftBuilder(
                    request.Callsign,
                    request.Cid,
                    request.Password,
                    request.Server,
                    request.Port)
                {
                    Protocol = request.Protocol,
                    Position = new AviationCalcUtilNet.GeoTools.GeoPoint(request.Position.Latitude,
                    request.Position.Longitude,
                    request.Position.IndicatedAltitude),
                    HeadingMag = request.Position.MagneticHeading,
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

                if (request.FmsWaypointList != null)
                {
                    builder.FmsWaypoints = request.FmsWaypointList;
                }

                var pilot = builder.Push(PrivateInfoLoader.GetClientInfo((string msg) => { _logger.LogWarning($"{request.Callsign}: {msg}"); }));

                return Ok(new AircraftResponse(pilot, true));
            } catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("getAll")]
        public List<AircraftResponse> GetAllAircraft()
        {
            List<AircraftResponse> pilots = new List<AircraftResponse>();
            SimAircraftHandler.PerformOnAircraft((list =>
            {
                foreach (var pilot in list)
                {
                    pilots.Add(new AircraftResponse(pilot));
                }
            }));
            return pilots;
        }

        [HttpGet("getAllWithFms")]
        public List<AircraftResponse> GetAllAircraftWithFms()
        {
            List<AircraftResponse> pilots = new List<AircraftResponse>();
            SimAircraftHandler.PerformOnAircraft((list =>
            {
                foreach (var pilot in list)
                {
                    pilots.Add(new AircraftResponse(pilot, true));
                }
            }));
            return pilots;
        }

        [HttpGet("getByCallsign/{callsign}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftResponse> GetAircraftByCallsign(string callsign)
        {
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            return Ok(new AircraftResponse(client, true));
        }

        [HttpGet("getByPartialCallsign/{callsign}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftResponse> GetAircraftByPartialCallsign(string callsign)
        {
            SimAircraft client = SimAircraftHandler.GetAircraftWhichContainsCallsign(callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            return Ok(new AircraftResponse(client, true));
        }

        [HttpPost("all/unpause")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<AircraftStateRequestResponse> UnpauseAll()
        {
            SimAircraftHandler.AllPaused = false;

            return Ok(new AircraftStateRequestResponse {
                Paused = SimAircraftHandler.AllPaused,
                SimRate = SimAircraftHandler.SimRate
            });
        }

        [HttpPost("all/pause")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<AircraftStateRequestResponse> PauseAll()
        {
            SimAircraftHandler.AllPaused = true;

            return Ok(new AircraftStateRequestResponse
            {
                Paused = SimAircraftHandler.AllPaused,
                SimRate = SimAircraftHandler.SimRate
            });
        }

        [HttpPost("all/simrate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<AircraftStateRequestResponse> SetAllSimRate(AircraftStateRequestResponse request)
        {
            SimAircraftHandler.SimRate = request.SimRate;

            return Ok(new AircraftStateRequestResponse
            {
                Paused = SimAircraftHandler.AllPaused,
                SimRate = SimAircraftHandler.SimRate
            });
        }

        [HttpGet("all/simState")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<AircraftStateRequestResponse> GetAllSimState()
        {
            return Ok(new AircraftStateRequestResponse
            {
                Paused = SimAircraftHandler.AllPaused,
                SimRate = SimAircraftHandler.SimRate
            });
        }

        [HttpPost("byCallsign/{callsign}/unpause")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftStateRequestResponse> UnpauseAircraft(string callsign)
        {
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(callsign);

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

        [HttpPost("byCallsign/{callsign}/pause")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftStateRequestResponse> PauseAircraft(string callsign)
        {
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(callsign);

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

        [HttpPost("byCallsign/{callsign}/simrate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftStateRequestResponse> SetAircraftSimRate(string callsign, AircraftStateRequestResponse request)
        {
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(callsign);

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

        [HttpGet("byCallsign/{callsign}/simState")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftStateRequestResponse> GetAircraftSimState(string callsign)
        {
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            return Ok(new AircraftStateRequestResponse
            {
                Paused = SimAircraftHandler.AllPaused,
                SimRate = SimAircraftHandler.SimRate
            });
        }

        [HttpDelete("removeByCallsign/{callsign}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftResponse> RemoveAircraftByCallsign(string callsign)
        {
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraftHandler.RemoveAircraftByCallsign(callsign);

            return Ok(new AircraftResponse(client, true));
        }

        [HttpDelete("all/remove")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult RemoveAll()
        {
            SimAircraftHandler.DeleteAllAircraft();
            return Ok();
        }

    }
}
