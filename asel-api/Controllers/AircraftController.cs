using AselAtcTrainingSim.AselApi.RestObjects;
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

        [HttpGet]
        public List<AircraftResponse> GetAircraft()
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

    }
}
