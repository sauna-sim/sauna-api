using AselAtcTrainingSim.AselApi.RestObjects.Commands;
using AselAtcTrainingSim.AselSimCore;
using AselAtcTrainingSim.AselSimCore.Simulator.Commands;
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
    [Route("api/commands")]
    public class CommandsController : ControllerBase
    {

        private readonly ILogger<DataController> _logger;

        public CommandsController(ILogger<DataController> logger)
        {
            _logger = logger;
        }

        [HttpPost("send/altitude")]
        public ActionResult SendAltitudeCommand(AltitudeCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot) client;

            double altimSetting = -1;
            if (request.Pressure > 0) {
                altimSetting = request.Pressure;
            }

            AltitudeCommand command = new AltitudeCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.Altitude, request.PressureAlt, altimSetting, request.PressureInInHg);

            if (CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }
    }
}
