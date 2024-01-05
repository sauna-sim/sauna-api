using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fixes;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;

namespace SaunaSim.Core.Simulator.Commands
{
    public class IlsCommand : IAircraftCommand
    {
        public SimAircraft Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private Localizer _loc;
        private IRoutePoint _locRoutePoint;
        
        public void ExecuteCommand()
        {
            if (Aircraft.Autopilot.CurrentLateralMode == LateralModeType.LNAV)
            {
                Aircraft.Autopilot.SelectedHeading = (int)Aircraft.Position.Heading_Mag;
            }

            // Add the LOC leg
            _locRoutePoint = new RouteWaypoint("LOC" + _loc.Runway_identifier, _loc.Loc_location);
            FmsPoint locFmsPoint = new FmsPoint(_locRoutePoint, RoutePointTypeEnum.FLY_OVER)
            {
                LowerAltitudeConstraint = _loc.Glideslope.Gs_elevation + 50,
                UpperAltitudeConstraint = _loc.Glideslope.Gs_elevation + 50,
                AngleConstraint = _loc.Glideslope.Gs_angle
            };
            CourseToFixLeg locLeg = new CourseToFixLeg(locFmsPoint, BearingTypeEnum.MAGNETIC, _loc.Loc_bearing);

            Aircraft.Fms.AddRouteLeg(locLeg);

            // Activate leg now, skipping all previous legs
            while ((Aircraft.Fms.ActiveLeg == null || !Aircraft.Fms.ActiveLeg.Equals(locLeg)) && Aircraft.Fms.GetRouteLegs().Count > 0)
            {
                Aircraft.Fms.ActivateNextLeg();
            }

            Aircraft.Autopilot.AddArmedLateralMode(LateralModeType.APCH);
            Aircraft.Autopilot.AddArmedVerticalMode(VerticalModeType.APCH);

            // Add event handler
            //Aircraft.Fms.WaypointPassed += OnLanded;
        }

        public void OnLanded(object sender, WaypointPassedEventArgs e)
        {
            if (e.RoutePoint.Equals(_locRoutePoint))
            {
                SimAircraftHandler.RemoveAircraftByCallsign(Aircraft.Callsign);
            }
        }

        public bool HandleCommand(SimAircraft aircraft, Action<string> logger, string runway)
        {
            Aircraft = aircraft;
            Logger = logger;
            // Find Waypoint
            Localizer wp = DataHandler.GetLocalizer(DataHandler.FAKE_AIRPORT_NAME, runway);

            if (wp == null)
            {
                Logger?.Invoke($"ERROR: Localizer {runway} not found!");
                return false;
            }

            _loc = wp;

            Logger?.Invoke($"{Aircraft.Callsign} flying ILS {runway}");

            return true;
        }

        public bool HandleCommand(ref List<string> args)
        {
            // Check argument length
            if (args.Count < 1)
            {
                Logger?.Invoke($"ERROR: ILS requires at least 1 arguments!");
                return false;
            }

            // Get runway string
            string rwyStr = args[0];

            args.RemoveAt(0);

            // Find Waypoint
            Localizer wp = DataHandler.GetLocalizer(DataHandler.FAKE_AIRPORT_NAME, rwyStr);
            
            if (wp == null)
            {
                Logger?.Invoke($"ERROR: Localizer {rwyStr} not found!");
                return false;
            }

            _loc = wp;

            Logger?.Invoke($"{Aircraft.Callsign} flying ILS {rwyStr}");

            return true;
        }
    }
}
