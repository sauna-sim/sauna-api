using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AviationCalcUtilNet.Atmos.Grib;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Magnetic;
using AviationCalcUtilNet.Units;
using FsdConnectorNet;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fixes;
using NavData_Interface.Objects.LegCollections.Airways;
using NavData_Interface.Objects.LegCollections.Legs;
using NavData_Interface.Objects.LegCollections.Procedures;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;
using SaunaSim.Core.Simulator.Aircraft.Performance;
using SaunaSim.Core.Simulator.Commands;

namespace SaunaSim.Core.Data.Loaders
{
    public class AircraftBuilder
    {
        private readonly MagneticTileManager _magTileManager;
        private readonly GribTileManager _gribTileManager;
        private readonly CommandHandler _commandHandler;
        public string Callsign { get; }
        public int Squawk { get; set; } = 0;
        public GeoPoint Position { get; set; } = new GeoPoint(0, 0, 10000);
        public Action<string> LogInfo { get; set; }
        public Action<string> LogWarn { get; set; }
        public Action<string> LogError { get; set; }
        public string AircraftType { get; set; } = "A320";
        public Bearing HeadingMag { get; set; } = Bearing.FromDegrees(0);
        public double Speed { get; set; } = 250;
        public bool IsSpeedMach { get; set; } = false;
        public TransponderModeType XpdrMode { get; set; } = TransponderModeType.ModeC;
        public int DelayMs { get; set; } = 0;
        public FlightPlan? FlightPlan { get; set; }
        public int RequestedAlt { get; set; } = -1;

        internal AircraftBuilder(MagneticTileManager magTileMgr, GribTileManager gribTileMgr, CommandHandler commandHandler) {
            _magTileManager = magTileMgr;
            _gribTileManager = gribTileMgr;
            _commandHandler = commandHandler;
            LogInfo = (string msg) => { Console.WriteLine($"{Callsign}: [INFO] {msg}"); };
            LogWarn = (string msg) => { Console.WriteLine($"{Callsign}: [WARN] {msg}"); };
            LogError = (string msg) => { Console.WriteLine($"{Callsign}: [ERROR] {msg}"); };
        }

        public AircraftBuilder(string callsign, MagneticTileManager magTileMgr, GribTileManager gribTileMgr, CommandHandler commandHandler) : this(magTileMgr, gribTileMgr, commandHandler)
        {
            Callsign = callsign;
        }

        public SimAircraft Create()
        {
            SimAircraft aircraft = new SimAircraft(
                Callsign,
                PerfDataHandler.LookupForAircraft(AircraftType),
                Position.Lat,
                Position.Lon,
                Position.Alt,
                HeadingMag,
                _magTileManager,
                _gribTileManager,
                _commandHandler,
                DelayMs)
            {
                LogInfo = LogInfo,
                LogWarn = LogWarn,
                LogError = LogError,
                XpdrMode = XpdrMode
            };

            //Figure out if aircraft on ground
            //Find closest airport within 10miles
            //if airplane alt is <= 500ft afe = onground
            var closestAirport = DataHandler.GetAirportByIdentifier(DataHandler.FAKE_AIRPORT_NAME);

            if (closestAirport != null && aircraft.Position.TrueAltitude < closestAirport.Elevation + 500)
            {
                aircraft.Position.OnGround = true;
                aircraft.Position.TrueAltitude = closestAirport.Elevation;
                aircraft.Position.GroundSpeed = Velocity.FromMetersPerSecond(0);
                aircraft.Position.Pitch = Angle.FromRadians(0);
                aircraft.Position.Bank = Angle.FromRadians(0);

                aircraft.FlightPhase = FlightPhaseType.ON_GROUND;
            }

            // Speed
            if (!aircraft.Position.OnGround)
            {
                if (IsSpeedMach)
                {
                    aircraft.Position.MachNumber = Speed;
                }
                else
                {
                    aircraft.Position.IndicatedAirSpeed = Velocity.FromKnots(Speed);
                }
            }

            // Flightplan           
            aircraft.FlightPlan = FlightPlan;
            var flightPlan = FlightPlan;

            if(flightPlan != null)
            {
                string[] waypoints = flightPlan?.route?.Split(' ', '.') ?? Array.Empty<string>();
                List<Leg> navDataFormatLegs = new List<Leg>();

                if (waypoints.Length > 0 && waypoints[0] != "")
                {
                    // Is the first point a SID?

                    bool foundSid = false;
                    string rawSidName = waypoints[0];

                    rawSidName = System.Text.RegularExpressions.Regex.Replace(rawSidName, @"\d", "#");

                    for (int j = 9; j > 0; j--)
                    {
                        Sid potentialSid = DataHandler.GetSidByAirportAndIdentifier(flightPlan?.origin, rawSidName.Replace('#', (char)(j + 48)));

                        if (potentialSid != null)
                        {
                            // TODO: Handle runway transition and SID transition. This just loads the 'central' part of the SID
                            foreach (Leg l in potentialSid)
                            {
                                navDataFormatLegs.Add(l);
                            }

                            // Our last point of the SID should now match with the following FP route point. If not, TF there
                            if (waypoints.Length > 1)
                            {
                                Leg prevLeg = navDataFormatLegs[navDataFormatLegs.Count - 1];
                                if (navDataFormatLegs[navDataFormatLegs.Count - 1].EndPoint.Identifier != waypoints[1])
                                {
                                    Fix nextWp = DataHandler.GetClosestWaypointByIdentifier(waypoints[1], prevLeg.EndPoint.Location);

                                    if (nextWp != null)
                                    {
                                        Leg tfLeg = new Leg(LegType.TRACK_TO_FIX, null, null, null, null, nextWp, null, null, null, null, null, null, null, null, null);
                                        navDataFormatLegs.Add(tfLeg);
                                    }
                                }
                            }

                            foundSid = true;
                            break;
                        }
                    }


                    int i = 0;

                    if (!foundSid)
                    {
                        // DF to first FP waypoint.

                        for (bool foundValidWp = false; !foundValidWp && i < waypoints.Length; i++)
                        {
                            Fix firstWp = DataHandler.GetClosestWaypointByIdentifier(waypoints[i], aircraft.Position.PositionGeoPoint);

                            if (firstWp == null)
                            {
                                continue;
                            }
                            else
                            {
                                Leg dfLeg = new Leg(LegType.DIRECT_TO_FIX, null, null, null, null, firstWp, new NavData_Interface.Objects.Fixes.Waypoints.WaypointDescription(false, true, false), null, null, null, null, null, null, null, null);
                                navDataFormatLegs.Add(dfLeg);
                                foundValidWp = true;
                            }
                        }
                    }
                    else
                    {
                        i = 2;
                    }

                    for (; i < waypoints.Length; i++)
                    {
                        // On each iteration of this function we are 'elegible' to start an airway.
                        // So, waypoints[i] could be an airway, a Fix to go direct to, or a DCT meaning we go direct to waypoints[i+1]
                        // DCT is ALWAYS interpreted as direct, then we try to interpret as airway, otherwise as a Fix to go direct to

                        // If this is the last point on the route it may be a STAR. If that is the case, try to use the previous point as transition
                        // Otherwise go to the last waypoint and then DCT DEST

                        if (i == waypoints.Length - 1)
                        {
                            Star potentialStar = DataHandler.GetStarByAirportAndIdentifier(flightPlan?.destination, waypoints[i]);

                            if (potentialStar != null)
                            {
                                // Is the previous point a transition?
                                try
                                {
                                    potentialStar.selectTransition(waypoints[i - 1]);
                                }
                                catch (ArgumentException ex)
                                {
                                    // Not a transition. Load the STAR without a transition
                                }

                                foreach (Leg l in potentialStar)
                                {
                                    navDataFormatLegs.Add(l);
                                }
                            }
                            else
                            {
                                // Go to this non-STAR waypoint, if it's not DCT, then DCT dest.
                                if (waypoints[i] != "DCT")
                                {
                                    Fix finalWp = DataHandler.GetClosestWaypointByIdentifier(waypoints[i], navDataFormatLegs[navDataFormatLegs.Count - 1].EndPoint.Location);

                                    if (finalWp != null)
                                    {
                                        Leg tfLeg = new Leg(LegType.TRACK_TO_FIX, null, null, null, null, finalWp, new NavData_Interface.Objects.Fixes.Waypoints.WaypointDescription(false, true, false), null, null, null, null, null, null, null, null);
                                        navDataFormatLegs.Add(tfLeg);
                                    }
                                }

                                string destAptIdentifier = flightPlan?.destination;

                                Airport dest = DataHandler.GetAirportByIdentifier(destAptIdentifier);

                                if (dest != null)
                                {
                                    Leg tfLeg = new Leg(LegType.TRACK_TO_FIX, null, null, null, null, dest, new NavData_Interface.Objects.Fixes.Waypoints.WaypointDescription(true, true, false), null, null, null, null, null, null, null, null);
                                    navDataFormatLegs.Add(tfLeg);
                                }
                            }
                        }

                        if (waypoints[i] == "DCT")
                        {
                            continue;
                        }

                        if (DataHandler.IsValidAirwayIdentifier(waypoints[i]) && i != waypoints.Length - 1)
                        {
                            // This is an airway! If we can process it, and select the previous and following waypoint, we'll accept it and add all legs.
                            // Otherwise try to process as waypoint anyways

                            Fix prevWp = navDataFormatLegs[navDataFormatLegs.Count - 1].EndPoint;
                            string airwayIdentifier = waypoints[i];

                            // In case there's a duplicate waypoint, use the one closest to the starting waypoint
                            // What if we have a really long airway?
                            // nats moment
                            Fix nextWp = DataHandler.GetClosestWaypointByIdentifier(waypoints[i + 1], prevWp.Location);

                            if (nextWp != null)
                            {
                                try
                                {
                                    Airway airway = DataHandler.GetAirwayFromIdentifierAndFixes(airwayIdentifier, prevWp, nextWp);

                                    foreach (Leg l in airway)
                                    {
                                        navDataFormatLegs.Add(l);
                                    }

                                    // All legs added, also, we now have a leg to the next waypoint. So skip that one.
                                    i++;
                                    continue;
                                }
                                catch (ArgumentException e)
                                {
                                    // Airway not valid between those points. We'll try to handle the airway name as a waypoint
                                    // So nothing to do here, continue down
                                }
                            }
                        }

                        Fix dctWp = DataHandler.GetClosestWaypointByIdentifier(waypoints[i], navDataFormatLegs[navDataFormatLegs.Count - 1].EndPoint.Location);

                        if (dctWp != null)
                        {
                            Leg tfLeg = new Leg(LegType.TRACK_TO_FIX, null, null, null, null, dctWp, new NavData_Interface.Objects.Fixes.Waypoints.WaypointDescription(false, true, false), null, null, null, null, null, null, null, null);
                            navDataFormatLegs.Add(tfLeg);
                        }
                    }
                }


                IList<IRouteLeg> legs = LegFactory.RouteLegsFromNavDataLegs(navDataFormatLegs, _magTileManager);

                foreach (IRouteLeg leg in legs)
                {
                    aircraft.Fms.AddRouteLeg(leg);
                }

                if (legs.Count > 0)
                {
                    // Not needed anymore because we'll add a DF leg above
                    // aircraft.Fms.ActivateDirectTo(legs[0].StartPoint.Point);

                    aircraft.Autopilot.AddArmedLateralMode(LateralModeType.LNAV);
                }
              
                // Find closest leg to activate
                var lowestAtk = Length.FromMeters(double.MaxValue);
                var lowestIndex = -1;
                var fmsLegs = aircraft.Fms.GetRouteLegs();
                for (int i = 0; i < fmsLegs.Count; i++)
                {
                    var currLeg = fmsLegs[i];
                    currLeg.ProcessLeg(aircraft, 100);
                    var aTk = currLeg.GetCourseInterceptInfo(aircraft).alongTrackDistance;

                    if (currLeg != null && aTk > Length.FromMeters(0) && aTk < lowestAtk)
                    {
                        lowestAtk = aTk;
                        lowestIndex = i;
                    }
                }

                if (lowestIndex >= 0)
                {
                    for (int i = 0; i < lowestIndex; i++)
                    {
                        aircraft.Fms.RemoveFirstLeg();
                    }
                }
            }

            // Requested Alt
            if (RequestedAlt >= 0)
            {
                aircraft.Autopilot.SelectedAltitude = RequestedAlt;
                aircraft.Autopilot.CurrentVerticalMode = VerticalModeType.FLCH;
            }


            return aircraft;
        }

        public class FactoryFmsWaypoint
        {
            public RoutePointTypeEnum PointType { get; set; } = RoutePointTypeEnum.FLY_BY;
            public string Identifier { get; set; }
            public int UpperAltitudeConstraint { get; set; }
            public int LowerAltitudeConstraint { get; set; }
            public ConstraintType SpeedConstratintType { get; set; } = ConstraintType.FREE;
            public double SpeedConstraint { get; set; } = 0;
            public bool ShouldHold { get; set; } = false;

            internal FactoryFmsWaypoint() { }

            public FactoryFmsWaypoint(string identifier)
            {
                Identifier = identifier;
            }
        }
    }
}

