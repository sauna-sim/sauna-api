using SaunaSim.Api.ApiObjects.Commands;
using SaunaSim.Core;
using SaunaSim.Core.Simulator.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator;

namespace SaunaSim.Api.Controllers
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

            AltitudeCommand command = new AltitudeCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.Altitude, request.PressureAlt, altimSetting, request.PressureInInHg);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }

        [HttpPost("send/departOnHeading")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendDepartOnHeadingCommand(DepartOnHeadingCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot)client;

            DepartOnHeadingCommand command = new DepartOnHeadingCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.Waypoint, request.Heading);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }

        [HttpPost("send/directWaypoint")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendDirectWaypointCommand(DirectWaypointCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot)client;

            DirectWaypointCommand command = new DirectWaypointCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.Waypoint);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }

        [HttpPost("send/flyHeading")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendFlyHeadingCommand(HeadingCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot)client;

            FlyHeadingCommand command = new FlyHeadingCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.Heading);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }

        [HttpPost("send/turnLeftByHeading")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendTurnLeftByHeadingCommand(DegTurnCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot)client;

            TurnLeftByHeadingCommand command = new TurnLeftByHeadingCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.DegreesToTurn);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }

        [HttpPost("send/turnLeftHeading")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendTurnLeftHeadingCommand(HeadingCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot)client;

            TurnLeftHeadingCommand command = new TurnLeftHeadingCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.Heading);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }

        [HttpPost("send/turnRightByHeading")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendTurnRightByHeadingCommand(DegTurnCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot)client;

            TurnRightByHeadingCommand command = new TurnRightByHeadingCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.DegreesToTurn);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }

        [HttpPost("send/turnRightHeading")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendTurnRightHeadingCommand(HeadingCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot)client;

            TurnRightHeadingCommand command = new TurnRightHeadingCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.Heading);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }

        [HttpPost("send/flyPresentHeading")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendFlyPresentHeadingCommand(NoArgCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot)client;

            FlyPresentHeadingCommand command = new FlyPresentHeadingCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg));

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }

        [HttpPost("send/hold")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendHoldCommand(HoldCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot)client;

            HoldCommand command = new HoldCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.Waypoint, request.PublishedHold, request.InboundCourse, request.TurnDirection, request.LegLengthType, request.LegLength);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }

        [HttpPost("send/ils")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendIlsCommand(LocIlsCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot)client;

            IlsCommand command = new IlsCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.Runway);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }

        [HttpPost("send/loc")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendLocCommand(LocIlsCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot)client;

            LocCommand command = new LocCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.Runway);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }

        [HttpPost("send/interceptCourse")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendInterceptCourseCommand(InterceptCourseCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot)client;

            InterceptCourseCommand command = new InterceptCourseCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.Waypoint, request.Course);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }

        [HttpPost("send/speed")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendSpeedCommand(SpeedCommandRequest request)
        {
            IVatsimClient client = ClientsHandler.GetClientByCallsign(request.Callsign);

            if (client == null || !(client is VatsimClientPilot))
            {
                return BadRequest("The aircraft was not found!");
            }

            VatsimClientPilot aircraft = (VatsimClientPilot)client;

            SpeedCommand command = new SpeedCommand();
            bool result = command.HandleCommand(aircraft, (string msg) => _logger.LogInformation(msg), request.ConstraintType, request.Speed);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }
    }
}
