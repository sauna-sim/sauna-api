using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Units;
using FsdConnectorNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fixes;
using SaunaSim.Api.ApiObjects.Aircraft;
using SaunaSim.Api.ApiObjects.Data;
using SaunaSim.Api.Services;
using SaunaSim.Api.Utilities;
using SaunaSim.Core.Data;
using SaunaSim.Core.Data.Loaders;
using SaunaSim.Core.Data.Scenario;
using SaunaSim.Core.Simulator.Session;

namespace SaunaSim.Api.Controllers;

/// <summary>
/// Handles Simulator Session Management
/// </summary>
[ApiController]
[Route("api/session")]
public class SessionController : ControllerBase
{
    private readonly ILogger<ServerController> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ISaunaSessionService _sessionService;

    public SessionController(ILogger<ServerController> logger, IHostApplicationLifetime appLifetime, ISaunaSessionService sessionService)
    {
        _appLifetime = appLifetime;
        _logger = logger;
        _sessionService = sessionService;
    }

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<string> CreateSession(SimSessionDetails sessionDetails)
    {
        if (sessionDetails.SessionType == SimSessionType.FSD)
        {
            if (sessionDetails.ConnectionDetails == null)
            {
                return BadRequest();
            }

            if (sessionDetails.ClientInfo == null)
            {
                sessionDetails.ClientInfo = PrivateInfoLoader.GetClientInfo((msg) => _logger.LogInformation("Create Session: {}", msg));
            }
        }

        return Created("Session Created", _sessionService.CreateSession(sessionDetails));
    }

    [HttpGet("{sessionId}/settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<SimSessionDetails> GetSettings(string sessionId)
    {
        if (!_sessionService.Sessions.TryGetValue(sessionId, out var session))
        {
            return BadRequest("Session Not Found");
        }

        return Ok(session.Session.Details);
    }

    [HttpPost("{sessionId}/settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<SimSessionDetails> UpdateSettings(string sessionId, SimSessionDetails settings)
    {
        if (!_sessionService.Sessions.TryGetValue(sessionId, out var value))
        {
            return BadRequest("Session Not Found");
        }

        value.Session.Details = settings;

        return Ok(value.Session.Details);
    }

    [HttpGet("{sessionId}/websocket")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task WebSocketStream(string sessionId)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            try
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await _sessionService.Sessions[sessionId].WebSocketHandler.HandleGeneralSocket(webSocket, _appLifetime.ApplicationStopping);
            }
            catch (Exception e)
            {
                _logger.LogWarning("Websocket connection failed: {}", e.Message);
            }
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    [HttpDelete("{sessionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public void DeleteSession(string sessionId)
    {
        if (!_sessionService.RemoveSession(sessionId))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    [HttpPost("{sessionId}/loadScenario/sauna")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> LoadSaunaScenario(string sessionId, LoadScenarioFileRequest request)
    {
        if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
        {
            return BadRequest("Session Not Found");
        }

        try
        {
            List<AircraftBuilder> pilots = new List<AircraftBuilder>();
            AircraftBuilder lastPilot = null;

            foreach (SaunaScenarioAircraft aircraft in request.Scenario.Aircraft)
            {
                Latitude lat = Latitude.FromDegrees(aircraft.Pos.Lat);
                Longitude lon = Longitude.FromDegrees(aircraft.Pos.Lon);

                // XPDR code
                int squawk = 0;
                try
                {
                    squawk = Convert.ToInt32(aircraft.Squawk);
                }
                catch (Exception e)
                {
                    if (e is not OverflowException && e is not FormatException)
                    {
                        throw;
                    }
                }

                lastPilot = new AircraftBuilder(aircraft.Callsign, sessionContainer.Session.AircraftHandler.MagTileManager,
                    sessionContainer.Session.AircraftHandler.GribTileManager, sessionContainer.Session.CommandHandler)
                {
                    Position = new GeoPoint(lat, lon, Length.FromFeet(aircraft.Alt)),
                    HeadingMag = Bearing.FromDegrees(0), //TODO: Automatically infer this from the route they are on.
                    LogInfo = (string msg) => { _logger.LogInformation($"{aircraft.Callsign}: {msg}"); },
                    LogWarn = (string msg) => { _logger.LogWarning($"{aircraft.Callsign}: {msg}"); },
                    LogError = (string msg) => { _logger.LogError($"{aircraft.Callsign}: {msg}"); },
                    XpdrMode = TransponderModeType.ModeC,
                    Squawk = squawk,
                    FlightPlan = aircraft.Fp
                };

                pilots.Add(lastPilot);
            }

            List<Task> tasks = new();

            foreach (AircraftBuilder pilot in pilots)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    DateTime start = DateTime.UtcNow;
                    var aircraft = pilot.Create();
                    sessionContainer.Session.AircraftHandler.AddAircraft(aircraft);
                    _logger.LogInformation($"{pilot.Callsign} created in {(DateTime.UtcNow - start).TotalMilliseconds}ms");
                }));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.StackTrace);
        }

        return Ok();
    }

    [HttpPost("{sessionId}/loadScenario/euroScope")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> LoadEuroScopeScenario(string sessionId, LoadScenarioFileRequest request)
    {
        if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
        {
            return BadRequest("Session Not Found");
        }

        try
        {
            string[] filelines = await System.IO.File.ReadAllLinesAsync(request.FileName);

            List<AircraftBuilder> pilots = new List<AircraftBuilder>();

            AircraftBuilder lastPilot = null;

            double refLat = 190;
            double refLon = 190;

            foreach (string line in filelines)
            {
                // Create pilot and update position
                if (line.StartsWith("@N"))
                {
                    string[] items = line.Split(':');
                    string callsign = items[1];
                    TransponderModeType xpdrMode;
                    switch (items[0].ToCharArray()[1])
                    {
                        case 'N':
                            xpdrMode = TransponderModeType.ModeC;
                            break;
                        case 'S':
                            xpdrMode = TransponderModeType.Standby;
                            break;
                        case 'Y':
                            xpdrMode = TransponderModeType.Ident;
                            break;
                        default:
                            xpdrMode = TransponderModeType.ModeC;
                            break;
                    }
                    // Load the coordinates. These could be in decimal or DMS format.
                    // TODO: If this fails, skip this aircraft. Right now, we set pos to 0,0!

                    Latitude lat = (Latitude)0;
                    Longitude lon = (Longitude)0;

                    // XPDR code
                    int squawk = 0;
                    try
                    {
                        squawk = Convert.ToInt32(items[2]);
                    }
                    catch (Exception e)
                    {
                        if (e is not OverflowException && e is not FormatException)
                        {
                            throw;
                        }
                    }

                    try
                    {
                        (lat, lon) = CoordinateUtil.ParseCoordinate(items[4], items[5]);
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine($"ERROR loading aircraft {callsign}: Could not parse coordinates");
                    }

                    EuroScopeLoader.ReadVatsimPosFlag(Convert.ToInt32(items[8]), out double hdg, out double bank, out double pitch, out bool onGround);

                    lastPilot = new AircraftBuilder(callsign, sessionContainer.Session.AircraftHandler.MagTileManager, sessionContainer.Session.AircraftHandler.GribTileManager,
                        sessionContainer.Session.CommandHandler)
                    {
                        Position = new GeoPoint(lat, lon, Length.FromFeet(Convert.ToDouble(items[6]))),
                        HeadingMag = Bearing.FromDegrees(hdg),
                        LogInfo = (string msg) => { _logger.LogInformation($"{callsign}: {msg}"); },
                        LogWarn = (string msg) => { _logger.LogWarning($"{callsign}: {msg}"); },
                        LogError = (string msg) => { _logger.LogError($"{callsign}: {msg}"); },
                        XpdrMode = xpdrMode,
                        Squawk = squawk
                    };

                    // Add to temp list
                    pilots.Add(lastPilot);
                }
                else if (line.StartsWith("$FP"))
                {
                    if (lastPilot != null)
                    {
                        try
                        {
                            lastPilot.FlightPlan = FlightPlan.ParseFromEsScenarioFile(line);
                        }
                        catch (FlightPlanException e)
                        {
                            _logger.LogWarning("Error parsing flight plan");
                            _logger.LogWarning(e.Message);
                        }
                    }
                }
                else if (line.StartsWith("REQALT"))
                {
                    string[] items = line.Split(':');

                    if (lastPilot != null && items.Length >= 3)
                    {
                        try
                        {
                            int reqAlt = Convert.ToInt32(items[2]);
                            lastPilot.RequestedAlt = reqAlt;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                else if (line.StartsWith("START"))
                {
                    string[] items = line.Split(':');

                    if (lastPilot != null && items.Length >= 2)
                    {
                        try
                        {
                            int delay = Convert.ToInt32(items[1]) * 60000;
                            lastPilot.DelayMs = delay;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                else if (line.StartsWith("ILS"))
                {
                    string[] items = line.Split(':');
                    string wpId = items[0].Replace("ILS", "");

                    try
                    {
                        GeoPoint threshold = new GeoPoint(Convert.ToDouble(items[1]), Convert.ToDouble(items[2]));

                        if (refLat > 180 || refLon > 180)
                        {
                            refLat = threshold.Lat.Degrees;
                            refLon = threshold.Lon.Degrees;
                        }

                        Bearing course = Bearing.FromRadians(0);
                        if (items.Length == 4)
                        {
                            course = Bearing.FromDegrees(Convert.ToDouble(items[3]));
                        }
                        else if (items.Length > 4)
                        {
                            GeoPoint otherThreshold = new GeoPoint(Convert.ToDouble(items[3]), Convert.ToDouble(items[4]));
                            course = sessionContainer.Session.AircraftHandler.MagTileManager.TrueToMagnetic(threshold, DateTime.UtcNow,
                                GeoPoint.InitialBearing(threshold, otherThreshold));
                        }

                        Length elev = (Length)0;
                        Airport airport = DataHandler.GetAirportByIdentifier(DataHandler.FAKE_AIRPORT_NAME);

                        if (airport != null)
                        {
                            elev = airport.Elevation;
                        }

                        Glideslope gs = new Glideslope(threshold, Angle.FromDegrees(3.0), elev);
                        Localizer loc = new Localizer("", "", DataHandler.FAKE_AIRPORT_NAME, wpId, wpId, threshold, 0, course, (Angle)0, IlsCategory.CATI, gs, 0);

                        DataHandler.AddLocalizer(loc);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Well that didn't work did it.");
                    }
                }
                else if (line.StartsWith("HOLDING"))
                {
                    string[] items = line.Split(':');

                    try
                    {
                        string wpId = items[1];
                        double inboundCourse = Convert.ToDouble(items[2]);
                        HoldTurnDirectionEnum turnDirection = (HoldTurnDirectionEnum)Convert.ToInt32(items[3]);
                        Fix fix = DataHandler.GetClosestWaypointByIdentifier(wpId, new GeoPoint(Latitude.FromDegrees(refLat), Longitude.FromDegrees(refLon)));
                        if (fix != null)
                        {
                            DataHandler.AddPublishedHold(new PublishedHold(fix, Bearing.FromDegrees(inboundCourse), turnDirection));
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error Loading Hold.");
                    }
                }
                else if (line.StartsWith("AIRPORT_ALT"))
                {
                    string[] items = line.Split(':');

                    try
                    {
                        double airportElev = Convert.ToDouble(items[1]);
                        Airport airport = new Airport(DataHandler.FAKE_AIRPORT_NAME, new GeoPoint(0, 0, 0), "", "", "", DataHandler.FAKE_AIRPORT_NAME, true, RunwaySurfaceCode.Hard,
                            Length.FromFeet(airportElev), Length.FromFeet(18000), Length.FromFeet(18000), Velocity.FromKnots(250), Length.FromFeet(10000), "");
                        DataHandler.AddAirport(airport);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error loading airport elevation");
                    }
                }
            }

            List<Task> tasks = new();

            foreach (AircraftBuilder pilot in pilots)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    DateTime start = DateTime.UtcNow;
                    var aircraft = pilot.Create();
                    sessionContainer.Session.AircraftHandler.AddAircraft(aircraft);
                    _logger.LogInformation($"{pilot.Callsign} created in {(DateTime.UtcNow - start).TotalMilliseconds}ms");
                }));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.StackTrace);
        }

        return Ok();
    }

    [HttpPost("{sessionId}/unpause")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AircraftStateRequestResponse> UnpauseAll(string sessionId)
    {
        if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
        {
            return BadRequest("Session Not Found");
        }

        sessionContainer.Session.AircraftHandler.AllPaused = false;

        return Ok(new AircraftStateRequestResponse
        {
            Paused = sessionContainer.Session.AircraftHandler.AllPaused,
            SimRate = sessionContainer.Session.AircraftHandler.SimRate
        });
    }

    [HttpPost("{sessionId}/pause")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<AircraftStateRequestResponse> PauseAll(string sessionId)
    {
        if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
        {
            return BadRequest("Session Not Found");
        }

        sessionContainer.Session.AircraftHandler.AllPaused = true;

        return Ok(new AircraftStateRequestResponse
        {
            Paused = sessionContainer.Session.AircraftHandler.AllPaused,
            SimRate = sessionContainer.Session.AircraftHandler.SimRate
        });
    }

    [HttpPost("{sessionId}/simrate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<AircraftStateRequestResponse> SetAllSimRate(AircraftStateRequestResponse request, string sessionId)
    {
        if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
        {
            return BadRequest("Session Not Found");
        }

        sessionContainer.Session.AircraftHandler.SimRate = request.SimRate;

        return Ok(new AircraftStateRequestResponse
        {
            Paused = sessionContainer.Session.AircraftHandler.AllPaused,
            SimRate = sessionContainer.Session.AircraftHandler.SimRate
        });
    }

    [HttpGet("{sessionId}/simState")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<AircraftStateRequestResponse> GetAllSimState(string sessionId)
    {
        if (!_sessionService.Sessions.TryGetValue(sessionId, out var sessionContainer))
        {
            return BadRequest("Session Not Found");
        }

        return Ok(new AircraftStateRequestResponse
        {
            Paused = sessionContainer.Session.AircraftHandler.AllPaused,
            SimRate = sessionContainer.Session.AircraftHandler.SimRate
        });
    }
}