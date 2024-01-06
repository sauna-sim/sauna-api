using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AviationCalcUtilNet.Atmos.Grib;
using AviationCalcUtilNet.Geo;
using AviationCalcUtilNet.GeoTools;
using AviationCalcUtilNet.Magnetic;
using AviationCalcUtilNet.Units;
using FsdConnectorNet;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fixes;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;
using SaunaSim.Core.Simulator.Aircraft.Performance;

namespace SaunaSim.Core.Data.Loaders
{
	public class AircraftBuilder
	{
		public MagneticTileManager MagTileManager { get; private set; }
		public GribTileManager GribTileManager { get; private set; }
		public string Callsign { get; set; }
		public string Cid { get; set; }
		public string Password { get; set; }
		public string FullName { get; set; } = "Simulator Pilot";
		public string Server { get; set; }
		public int Port { get; set; }
		public int Squawk { get; set; } = 0;
		public GeoPoint Position { get; set; } = new GeoPoint(0, 0, 10000);
		public Action<string> LogInfo { get; set; }
        public Action<string> LogWarn { get; set; }
        public Action<string> LogError { get; set; }
        public string AircraftType { get; set; } = "A320";
		public ProtocolRevision Protocol { get; set; } = ProtocolRevision.Classic;
		public Bearing HeadingMag { get; set; } = Bearing.FromDegrees(0);
		public double Speed { get; set; } = 250;
		public bool IsSpeedMach { get; set; } = false;
		public TransponderModeType XpdrMode { get; set; } = TransponderModeType.ModeC;
		public int DelayMs { get; set; } = 0;
		public string EsFlightPlanStr { get; set; } = "";
		public int RequestedAlt { get; set; } = -1;
		public List<FactoryFmsWaypoint> FmsWaypoints { get; set; } = new List<FactoryFmsWaypoint>();

		internal AircraftBuilder(MagneticTileManager magTileMgr, GribTileManager gribTileMgr) {
			MagTileManager = magTileMgr;
			GribTileManager = gribTileMgr;
			LogInfo = (string msg) => { Console.WriteLine($"{Callsign}: [INFO] {msg}"); };
            LogWarn = (string msg) => { Console.WriteLine($"{Callsign}: [WARN] {msg}"); };
            LogError = (string msg) => { Console.WriteLine($"{Callsign}: [ERROR] {msg}"); };
        }

		public AircraftBuilder(string callsign, string cid, string password, string server, int port, MagneticTileManager magTileMgr, GribTileManager gribTileMgr) : this(magTileMgr, gribTileMgr)
		{
			Callsign = callsign;
			Cid = cid;
			Server = server;
			Port = port;
			Password = password;
		}

		public SimAircraft Push(ClientInfo clientInfo)
		{
			SimAircraft aircraft = new SimAircraft(
				Callsign,
				Cid,
				Password,
				FullName,
				Server,
				(ushort)Port,
				Protocol,
				clientInfo,
				PerfDataHandler.LookupForAircraft(AircraftType),
				Position.Lat,
				Position.Lon,
				Position.Alt,
				HeadingMag,
				MagTileManager,
				GribTileManager,
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
			if(!aircraft.Position.OnGround)
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
            FlightPlan flightPlan;
            try
            {
                flightPlan = FlightPlan.ParseFromEsScenarioFile(EsFlightPlanStr);
				aircraft.FlightPlan = flightPlan;
            }
            catch (FlightPlanException e)
            {
                LogWarn("Error parsing flight plan");
                LogWarn(e.Message);
            }

			// Requested Alt
			if (RequestedAlt >= 0)
			{
				aircraft.Autopilot.SelectedAltitude = RequestedAlt;
                aircraft.Autopilot.CurrentVerticalMode = VerticalModeType.FLCH;
			}

            // Route
            List<IRouteLeg> legs = new List<IRouteLeg>();

            FmsPoint lastPoint = null;

			foreach (FactoryFmsWaypoint waypoint in FmsWaypoints)
			{
                Fix nextWp = DataHandler.GetClosestWaypointByIdentifier(waypoint.Identifier, aircraft.Position.PositionGeoPoint);

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

					if (waypoint.ShouldHold)
					{
                        PublishedHold pubHold = DataHandler.GetPublishedHold(fmsPt.Point.PointName, fmsPt.Point.PointPosition);

                        if (pubHold != null)
                        {
                            fmsPt.PointType = RoutePointTypeEnum.FLY_OVER;
                            HoldToManualLeg leg = new HoldToManualLeg(lastPoint, BearingTypeEnum.MAGNETIC, pubHold.InboundCourse, pubHold.TurnDirection, pubHold.LegLengthType, pubHold.LegLength, MagTileManager);
                            legs.Add(leg);
                            lastPoint = leg.EndPoint;
                        }
                    }
                }
            }

            foreach (IRouteLeg leg in legs)
            {
                aircraft.Fms.AddRouteLeg(leg);
            }

            if (legs.Count > 0)
            {
                aircraft.Fms.ActivateDirectTo(legs[0].StartPoint.Point);
                aircraft.Autopilot.AddArmedLateralMode(LateralModeType.LNAV);
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

