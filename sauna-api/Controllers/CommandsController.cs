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
using SaunaSim.Core.Simulator.Aircraft;
using FsdConnectorNet;
using System.Runtime.CompilerServices;
using System.Threading;
using SaunaSim.Api.WebSockets;

namespace SaunaSim.Api.Controllers
{
    [ApiController]
    [Route("api/commands")]
    public class CommandsController : ControllerBase
    {
        private static List<string> _commandsBuffer = new List<string>();
        private static SemaphoreSlim _commandsBufferLock = new SemaphoreSlim(1);

        private readonly ILogger<DataController> _logger;

        public CommandsController(ILogger<DataController> logger)
        {
            _logger = logger;
        }

        private static void LogCommandInfo(string msg)
        {
            _commandsBufferLock.Wait();
            _commandsBuffer.Add(msg);
            _commandsBufferLock.Release();

            WebSocketHandler.SendCommandMsg(msg).ConfigureAwait(false);
        }

        [HttpGet("commandBuffer")]
        public List<string> GetCommandBuffer()
        {
            List<string> log;

            _commandsBufferLock.Wait();
            log = _commandsBuffer;
            _commandsBuffer = new List<string>();
            _commandsBufferLock.Release();

            return log;
        }

        [HttpPost("send/textCommand")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendTextCommand(TextCommandRequest request)
        {
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;
            var remaining_args = CommandHandler.HandleCommand(request.Command, aircraft, request.Args, LogCommandInfo);
            while (remaining_args.Count > 0)
            {
                // Get command name
                string command = remaining_args[0].ToLower();
                remaining_args.RemoveAt(0);

                remaining_args = CommandHandler.HandleCommand(command, aircraft, remaining_args, LogCommandInfo);
            }
            return Ok();
        }

        [HttpPost("send/altitude")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult SendAltitudeCommand(AltitudeCommandRequest request)
        {
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            double altimSetting = -1;
            if (request.Pressure > 0)
            {
                altimSetting = request.Pressure;
            }

            AltitudeCommand command = new AltitudeCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo, request.Altitude, request.PressureAlt, altimSetting, request.PressureInInHg);

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
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            DepartOnHeadingCommand command = new DepartOnHeadingCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo, request.Waypoint, request.Heading);

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
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            DirectWaypointCommand command = new DirectWaypointCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo, request.Waypoint);

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
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            FlyHeadingCommand command = new FlyHeadingCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo, request.Heading);

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
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            TurnLeftByHeadingCommand command = new TurnLeftByHeadingCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo, request.DegreesToTurn);

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
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            TurnLeftHeadingCommand command = new TurnLeftHeadingCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo, request.Heading);

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
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            TurnRightByHeadingCommand command = new TurnRightByHeadingCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo, request.DegreesToTurn);

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
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            TurnRightHeadingCommand command = new TurnRightHeadingCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo, request.Heading);

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
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            FlyPresentHeadingCommand command = new FlyPresentHeadingCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo);

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
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            HoldCommand command = new HoldCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo, request.Waypoint, request.PublishedHold, request.InboundCourse, request.TurnDirection, request.LegLengthType, request.LegLength);

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
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            IlsCommand command = new IlsCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo, request.Runway);

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
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            LocCommand command = new LocCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo, request.Runway);

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
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            InterceptCourseCommand command = new InterceptCourseCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo, request.Waypoint, request.Course);

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
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(request.Callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            SimAircraft aircraft = client;

            SpeedCommand command = new SpeedCommand();
            bool result = command.HandleCommand(aircraft, LogCommandInfo, request.ConstraintType, request.Speed);

            if (result && CommandHandler.QueueCommand(command))
            {
                return Ok();
            }
            return BadRequest("An error occured sending the command.");
        }
    }
}
