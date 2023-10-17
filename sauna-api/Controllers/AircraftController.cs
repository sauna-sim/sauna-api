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
                SimAircraft pilot = new SimAircraft(
                    request.Callsign, 
                    request.Cid,
                    request.Password, 
                    request.FullName, 
                    request.Server, 
                    (ushort)request.Port, 
                    request.Protocol,
                    ClientInfoLoader.GetClientInfo((string msg) => { _logger.LogWarning($"{request.Callsign}: {msg}"); }),
                    PerfDataHandler.LookupForAircraft("A320"),
                    request.Position.Latitude, 
                    request.Position.Longitude, 
                    request.Position.IndicatedAltitude, 
                    request.Position.MagneticHeading
                    )
                {
                    LogInfo = (string msg) => {
                        _logger.LogInformation($"{request.Callsign}: {msg}");
                    },
                    LogWarn = (string msg) => {
                        _logger.LogWarning($"{request.Callsign}: {msg}");
                    },
                    LogError = (string msg) => {
                        _logger.LogError($"{request.Callsign}: {msg}");
                    },

                    XpdrMode = request.TransponderMode,
                    Squawk = request.Squawk,
                    Paused = request.Paused,
                    //TODO: flight plan
                };


                if (request.Position.IsMachNumber)
                {
                    pilot.Position.MachNumber = request.Position.IndicatedSpeed;
                } else
                {
                    pilot.Position.IndicatedAirSpeed = request.Position.IndicatedSpeed;
                }



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
                    pilot.Fms.AddRouteLeg(leg);
                }

                if (legs.Count > 0)
                {
                    pilot.Fms.ActivateDirectTo(legs[0].StartPoint.Point);
                    pilot.Autopilot.AddArmedLateralMode(LateralModeType.LNAV);
                }

                SimAircraftHandler.AddAircraft(pilot);
                pilot.Start();

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

        [HttpPost("all/simState")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<AircraftStateRequestResponse> SetAllSimState(AircraftStateRequestResponse request)
        {
            SimAircraftHandler.AllPaused = request.Paused;
            SimAircraftHandler.SimRate = request.SimRate;

            return Ok(new AircraftStateRequestResponse {
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

        [HttpPost("byCallsign/{callsign}/simState")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AircraftStateRequestResponse> SetAircraftSimState(string callsign, AircraftStateRequestResponse request)
        {
            SimAircraft client = SimAircraftHandler.GetAircraftByCallsign(callsign);

            if (client == null)
            {
                return BadRequest("The aircraft was not found!");
            }

            client.Paused = request.Paused;
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
