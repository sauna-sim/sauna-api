using SaunaSim.Api.ApiObjects.Data;
using SaunaSim.Core;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS;
using SaunaSim.Core.Simulator.Aircraft.Control.FMS.Legs;
using SaunaSim.Core.Simulator.Commands;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.GeoTools.MagneticTools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FsdConnectorNet;
using SaunaSim.Core.Data.Loaders;
using SaunaSim.Api.Utilities;
using NavData_Interface.Objects.Fix;
using NavData_Interface.Objects;

namespace SaunaSim.Api.Controllers
{
    [ApiController]
    [Route("api/data")]
    public class DataController : ControllerBase
    {

        private readonly ILogger<DataController> _logger;

        public DataController(ILogger<DataController> logger)
        {
            _logger = logger;
        }

        [HttpGet("settings")]
        public AppSettingsRequestResponse GetSettings()
        {
            return new AppSettingsRequestResponse(AppSettingsManager.Settings);
        }

        [HttpPost("settings")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<AppSettings> UpdateSettings(AppSettingsRequestResponse settings)
        {
            try
            {
                AppSettingsManager.Settings = settings.ToAppSettings();
            } catch (Exception ex)
            {
                if (ex is IndexOutOfRangeException || ex is FormatException || ex is OverflowException)
                {
                    return BadRequest("Command frequency was not in the correct format.");
                }
                throw;
            }


            return Ok(new AppSettingsRequestResponse(AppSettingsManager.Settings));
        }

        [HttpGet("navigraphApiCreds")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<NavigraphApiCreds> GetNavigraphApiCreds()
        {
            string error = "";
            var navigraphCreds = PrivateInfoLoader.GetNavigraphCreds((string s) =>
            {
                error = s;
            });

            if (navigraphCreds == null)
            {
                return BadRequest(error);
            }

            return navigraphCreds;
        }
        	
        [HttpGet("hasNavigraphDataLoaded")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<NavigraphLoadedResponse> GetHasNavigraphDataLoaded()
        {
            return Ok(new NavigraphLoadedResponse() { Loaded = DataHandler.HasNavigraphDataLoaded(), Uuid = DataHandler.GetNavigraphFileUuid()});
        }

        [HttpPost("loadSectorFile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult LoadSectorFile(LoadFileRequest request)
        {
            try
            {
                DataHandler.LoadSectorFile(request.FileName);
                return Ok();
            } catch (System.IO.FileNotFoundException)
            {
                return BadRequest("The file could not be found.");
            }
            catch (Exception)
            {
                return BadRequest("The file is not a vaid Sector file.");
            }
        }

        [HttpPost("loadDFDNavData")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult LoadDFDNavData(LoadDfdFileRequest request)
        {
            try
            {
                DataHandler.LoadNavigraphDataFile(request.FileName, request.Uuid);
                return Ok();
            } catch (System.IO.FileNotFoundException ex)
            {
                return BadRequest("The file could not be found.");
            } catch (Exception ex)
            {
                return BadRequest("The file is not a vaid NavData file.");
            }
        }

        [HttpPost("loadEuroscopeScenario")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult LoadEuroscopeScenario(LoadScenarioFileRequest request)
        {
            try
            {
                string[] filelines = System.IO.File.ReadAllLines(request.FileName);

                List<SimAircraft> pilots = new List<SimAircraft>();

                SimAircraft lastPilot = null;

                double refLat = 0;
                double refLon = 0;

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

                        double lat = 0;
                        double lon = 0;

                        try
                        {
                            (lat, lon) = CoordinateUtil.ParseCoordinate(items[4], items[5]);
                        } catch (FormatException e)
                        {
                            Console.WriteLine($"ERROR loading aircraft {callsign}: Could not parse coordinates");
                        }

                        EuroScopeLoader.ReadVatsimPosFlag(Convert.ToInt32(items[8]), out double hdg, out double bank, out double pitch, out bool onGround);
                        //SimAircraft(string callsign, string networkId, string password,        string fullname, string hostname, ushort port, bool vatsim,   ProtocolRevision protocol,      double lat, double lon, double alt, double hdg_mag, int delayMs = 0)
                        lastPilot = new SimAircraft(callsign, request.Cid, request.Password, "Simulator Pilot", request.Server, (ushort)request.Port, request.Protocol,
                            PrivateInfoLoader.GetClientInfo((string msg) => { _logger.LogWarning($"{callsign}: {msg}"); }),
                            lat, lon, Convert.ToDouble(items[6]), hdg)
                        {
                            LogInfo = (string msg) =>
                            {
                                _logger.LogInformation($"{callsign}: {msg}");
                            },
                            LogWarn = (string msg) =>
                            {
                                _logger.LogWarning($"{callsign}: {msg}");
                            },
                            LogError = (string msg) =>
                            {
                                _logger.LogError($"{callsign}: {msg}");
                            },
                            XpdrMode = xpdrMode,
                        };
                        lastPilot.Position.IndicatedAirSpeed = 250.0;

                        // Add to temp list
                        pilots.Add(lastPilot);
                    } else if (line.StartsWith("$FP"))
                    {
                        if (lastPilot != null)
                        {
                            FlightPlan flightPlan;
                            try
                            {
                                flightPlan = FlightPlan.ParseFromEsScenarioFile(line);
                            } catch (FlightPlanException e)
                            {
                                Console.WriteLine("Error parsing flight plan");
                                Console.WriteLine(e.Message);
                                continue;
                            }
                            lastPilot.FlightPlan = flightPlan;
                        }
                    } else if (line.StartsWith("REQALT"))
                    {

                        string[] items = line.Split(':');

                        if (lastPilot != null && items.Length >= 3)
                        {
                            try
                            {
                                int reqAlt = Convert.ToInt32(items[2]);
                                reqAlt /= 100;

                                List<string> args = new List<string>
                                {
                                    $"FL{reqAlt}"
                                };
                                CommandHandler.HandleCommand("dm", lastPilot, args, (string msg) => _logger.LogInformation(msg));
                            } catch (Exception) { }
                        }
                    } else if (line.StartsWith("$ROUTE"))
                    {
                        string[] items = line.Split(':');

                        if (lastPilot != null && items.Length >= 2)
                        {
                            string[] waypoints = items[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            List<IRouteLeg> legs = new List<IRouteLeg>();
                            FmsPoint lastPoint = null;


                            for (int i = 0; i < waypoints.Length; i++)
                            {
                                if (waypoints[i].ToLower() == "hold" && lastPoint != null)
                                {
                                    PublishedHold pubHold = DataHandler.GetPublishedHold(lastPoint.Point.PointName, lastPoint.Point.PointPosition.Lat, lastPoint.Point.PointPosition.Lon);

                                    if (pubHold != null)
                                    {
                                        lastPoint.PointType = RoutePointTypeEnum.FLY_OVER;
                                        HoldToManualLeg leg = new HoldToManualLeg(lastPoint, BearingTypeEnum.MAGNETIC, pubHold.InboundCourse, pubHold.TurnDirection, pubHold.LegLengthType, pubHold.LegLength);
                                        legs.Add(leg);
                                        lastPoint = leg.EndPoint;
                                    }
                                } else
                                {
                                    if (waypoints[i].Contains("/"))
                                    {
                                        var splitWp = waypoints[i].Split("/");

                                        if (splitWp.Length == 2)
                                        {
                                            try
                                            {
                                                int altitudeRestriction = int.Parse(splitWp[2]);
                                                // TODO: add the altitude restriction to the FMS

                                                waypoints[i] = splitWp[0];

                                            } catch (Exception e)
                                            {
                                                Console.Error.WriteLine($"Invalid altitude restriction {splitWp[1]}");
                                                continue;
                                            }
                                        } else
                                        {
                                            Console.Error.WriteLine($"Invalid waypoint name {waypoints[i]}");
                                        }
                                    }

                                    Fix nextWp = DataHandler.GetClosestWaypointByIdentifier(waypoints[i], lastPilot.Position.Latitude, lastPilot.Position.Longitude);

                                    if (nextWp != null)
                                    {
                                        FmsPoint fmsPt = new FmsPoint(new RouteWaypoint(nextWp), RoutePointTypeEnum.FLY_BY);
                                        if (lastPoint == null)
                                        {
                                            lastPoint = fmsPt;
                                        } else
                                        {
                                            legs.Add(new TrackToFixLeg(lastPoint, fmsPt));
                                            lastPoint = fmsPt;
                                        }
                                    }
                                }
                            }

                            foreach (IRouteLeg leg in legs)
                            {
                                lastPilot.Control.FMS.AddRouteLeg(leg);
                            }

                            if (legs.Count > 0)
                            {
                                lastPilot.Control.FMS.ActivateDirectTo(legs[0].StartPoint.Point);
                                LnavRouteInstruction instr = new LnavRouteInstruction();
                                lastPilot.Control.CurrentLateralInstruction = instr;
                            }
                        }
                    } else if (line.StartsWith("START"))
                    {
                        string[] items = line.Split(':');

                        if (lastPilot != null && items.Length >= 2)
                        {
                            try
                            {
                                int delay = Convert.ToInt32(items[1]) * 60000;
                                lastPilot.DelayMs = delay;
                            } catch (Exception) { }
                        }
                    } else if (line.StartsWith("ILS"))
                    {
                        string[] items = line.Split(':');
                        string wpId = items[0].Replace("ILS", "");

                        try
                        {
                            GeoPoint threshold = new GeoPoint(Convert.ToDouble(items[1]), Convert.ToDouble(items[2]));

                            refLat = threshold.Lat;
                            refLon = threshold.Lon;

                            double course = 0;
                            if (items.Length == 4)
                            {
                                course = Convert.ToDouble(items[3]);
                            } else if (items.Length > 4)
                            {
                                GeoPoint otherThreshold = new GeoPoint(Convert.ToDouble(items[3]), Convert.ToDouble(items[4]));
                                course = MagneticUtil.ConvertTrueToMagneticTile(GeoPoint.InitialBearing(threshold, otherThreshold), threshold);
                            }

                            DataHandler.AddLocalizer(new Localizer("", "", "_fake_airport", wpId, wpId, threshold, 0, course, 0, IlsCategory.CATI, 0));
                        } catch (Exception)
                        {
                            Console.WriteLine("Well that didn't work did it.");
                        }
                    } else if (line.StartsWith("HOLDING"))
                    {
                        string[] items = line.Split(':');

                        try
                        {
                            string wpId = items[1];
                            double inboundCourse = Convert.ToDouble(items[2]);
                            HoldTurnDirectionEnum turnDirection = (HoldTurnDirectionEnum)Convert.ToInt32(items[3]);
                            Fix fix = DataHandler.GetClosestWaypointByIdentifier(wpId, refLat, refLon);
                            DataHandler.AddPublishedHold(new PublishedHold(fix, inboundCourse, turnDirection));
                        } catch (Exception)
                        {
                            Console.WriteLine("Well that didn't work did it.");
                        }
                    }
                }

                foreach (SimAircraft pilot in pilots)
                {
                    SimAircraftHandler.AddAircraft(pilot);
                    pilot.Start();
                }
            } catch (Exception ex)
            {
                return BadRequest(ex.StackTrace);
            }
            return Ok();
        }

    }
}
