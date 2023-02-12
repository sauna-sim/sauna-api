using AselAtcTrainingSim.AselApi.RestObjects.Aircraft;
using AselAtcTrainingSim.AselSimCore;
using AselAtcTrainingSim.AselSimCore.Clients;
using AselAtcTrainingSim.AselSimCore.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator;

namespace AselAtcTrainingSim.AselApi.Controllers
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

        [HttpGet("getAll")]
        public List<AircraftResponse> GetAllAircraft()
        {
            List<AircraftResponse> pilots = new List<AircraftResponse>();
            foreach (var disp in ClientsHandler.DisplayableList)
            {
                if (disp.client is VatsimClientPilot pilot)
                {
                    pilots.Add(new AircraftResponse(pilot));
                }
            }
            return pilots;
        }

        [HttpGet("getAllWithFms")]
        public List<AircraftResponse> GetAllAircraftWithFms()
        {
            List<AircraftResponse> pilots = new List<AircraftResponse>();
            foreach (var disp in ClientsHandler.DisplayableList)
            {
                if (disp.client is VatsimClientPilot pilot)
                {
                    pilots.Add(new AircraftResponse(pilot, true));
                }
            }
            return pilots;
        }

        [HttpGet("getByCallsign/{callsign}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftResponse> GetAircraftByCallsign(string callsign)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            return Ok(new AircraftResponse((VatsimClientPilot)client, true));
        }

        [HttpGet("getByPartialCallsign/{callsign}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftResponse> GetAircraftByPartialCallsign(string callsign)
        {
            IVatsimClient client = ClientsHandler.GetClientWhichContainsCallsign(callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            return Ok(new AircraftResponse((VatsimClientPilot)client, true));
        }

        [HttpPost("all/pause")]
        public ActionResult PauseAll()
        {
            ClientsHandler.AllPaused = true;
            return Ok();
        }

        [HttpPost("all/unpause")]
        public ActionResult UnpauseAll()
        {
            ClientsHandler.AllPaused = false;
            return Ok();
        }

        [HttpDelete("removeByCallsign/{callsign}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftResponse> RemoveAircraftByCallsign(string callsign)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            ClientsHandler.RemoveClientByCallsign(callsign);

            return Ok(new AircraftResponse((VatsimClientPilot)client, true));
        }

        [HttpDelete("all/remove")]
        public ActionResult RemoveAll()
        {
            ClientsHandler.DisconnectAllClients();
            return Ok();
        }

    }
}
