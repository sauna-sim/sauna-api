using AselAtcTrainingSim.AselApi.ApiObjects.Aircraft;
using AselAtcTrainingSim.AselSimCore;
using AselAtcTrainingSim.AselSimCore.Clients;
using AselAtcTrainingSim.AselSimCore.Data;
using AselAtcTrainingSim.AselSimCore.Simulator.Aircraft;
using AselAtcTrainingSim.AselSimCore.Simulator.Aircraft.Control.FMS;
using AselAtcTrainingSim.AselSimCore.Simulator.Aircraft.Control.FMS.Legs;
using AselAtcTrainingSim.AselSimCore.Simulator.Commands;
using AviationCalcUtilNet.GeoTools;
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

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftResponse> CreateAircraft(CreateAircraftRequest request) // reuses code from DataController.LoadEuroScopeScenario. Both should use the same function.
        {
            try
            {
                VatsimClientPilot pilot = new VatsimClientPilot(request.Callsign, request.Cid, request.Password, request.FullName, request.Server, request.Port, request.VatsimServer, request.Protocol)
                {
                    Logger = (string msg) =>
                    {
                        _logger.LogInformation($"{request.Callsign}: {msg}");
                    },

                    Rating = request.PilotRating,

                    XpdrMode = request.TransponderMode,
                    Squawk = request.Squawk,
                    Paused = request.Paused,
                    FlightPlan = request.FlightPlan,
                };

                pilot.Position.Latitude = request.Position.Latitude;
                pilot.Position.Longitude = request.Position.Longitude;
                pilot.Position.IndicatedAltitude = request.Position.IndicatedAltitude;

                if (request.Position.IsMachNumber)
                {
                    pilot.Position.MachNumber = request.Position.IndicatedSpeed;
                } else
                {
                    pilot.Position.IndicatedAirSpeed = request.Position.IndicatedSpeed;
                }

                pilot.Position.Heading_Mag = request.Position.MagneticHeading;

                List<IRouteLeg> legs = new List<IRouteLeg>();

                FmsPoint lastPoint = null;

                foreach (FmsWaypointRequest waypoint in request.FmsWaypointList)
                {
                    if (waypoint.Identifier.ToLower() == "hold" && lastPoint != null)
                    {
                        PublishedHold pubHold = DataHandler.GetPublishedHold(lastPoint.Point.PointName);

                        if (pubHold != null)
                        {
                            lastPoint.PointType = RoutePointTypeEnum.FLY_OVER;
                            HoldToManualLeg leg = new HoldToManualLeg(lastPoint, BearingTypeEnum.MAGNETIC, pubHold.InboundCourse, pubHold.TurnDirection, pubHold.LegLengthType, pubHold.LegLength);
                            legs.Add(leg);
                            lastPoint = leg.EndPoint;
                        }
                    } else
                    {
                        Waypoint nextWp = DataHandler.GetClosestWaypointByIdentifier(waypoint.Identifier, pilot.Position.Latitude, pilot.Position.Longitude);

                        if (nextWp != null)
                        {
                            FmsPoint fmsPt = new FmsPoint(new RouteWaypoint(nextWp), RoutePointTypeEnum.FLY_BY)
                            {
                                UpperAltitudeConstraint = waypoint.UpperAltitudeConstraint,
                                LowerAltitudeConstraint = waypoint.LowerAltitudeConstraint,
                                SpeedConstraintType = waypoint.SpeedConstratintType,
                                SpeedConstraint = waypoint.SpeedConstraint,

                            };

                            if (lastPoint == null)
                            {
                                lastPoint = fmsPt;
                            }
                            else
                            {
                                legs.Add(new TrackToFixLeg(lastPoint, fmsPt));
                                lastPoint = fmsPt;
                            }
                        }
                    }
                }

                foreach (IRouteLeg leg in legs)
                {
                    pilot.Control.FMS.AddRouteLeg(leg);
                }

                if (legs.Count > 0)
                {
                    pilot.Control.FMS.ActivateDirectTo(legs[0].StartPoint.Point);
                    LnavRouteInstruction lnavInstr = new LnavRouteInstruction();
                    pilot.Control.CurrentLateralInstruction = lnavInstr;
                }

                AltitudeHoldInstruction altInstr = new AltitudeHoldInstruction((int)pilot.Position.IndicatedAltitude);
                pilot.Control.CurrentVerticalInstruction = altInstr;

                ClientsHandler.AddClient(pilot);
                pilot.ShouldSpawn = true;

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
