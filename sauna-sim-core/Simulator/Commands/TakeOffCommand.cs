using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fix;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.Control.Instructions.Lateral;
using SaunaSim.Core.Simulator.Aircraft.Control.Instructions.Vertical;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using SaunaSim.Core.Simulator.Aircraft.FMS.Legs;
using SaunaSim.Core.Simulator.Aircraft.Ground;

namespace SaunaSim.Core.Simulator.Commands
{
    public class TakeOffCommand : IAircraftCommand
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

            // Add the LOC/TO leg
            _locRoutePoint = new RouteWaypoint("RWY" + _loc.Runway_identifier, _loc.Loc_location);
            FmsPoint locFmsPoint = new FmsPoint(_locRoutePoint, RoutePointTypeEnum.FLY_OVER);
            FixToManualLeg toLeg = new FixToManualLeg(locFmsPoint, BearingTypeEnum.MAGNETIC, _loc.Loc_bearing);

            Aircraft.Fms.InsertAtIndex(toLeg, 0);

            // Activate leg now, skipping all previous legs
            while ((Aircraft.Fms.ActiveLeg == null || !Aircraft.Fms.ActiveLeg.Equals(toLeg)) && Aircraft.Fms.GetRouteLegs().Count > 0)
            {
                Aircraft.Fms.ActivateNextLeg();
            }

            foreach (IRouteLeg leg in Aircraft.Fms.GetRouteLegs())
            {
                if (leg.Equals(toLeg))
                {
                    break;
                }
                Aircraft.Fms.ActivateNextLeg();
            }

            Aircraft.GroundHandler.GroundPhase = GroundPhaseType.TAKEOFF;
            //Aircraft.Autopilot.AddArmedLateralMode(LateralModeType.LNAV);
            //Aircraft.Autopilot.AddArmedVerticalMode(VerticalModeType.APCH);

            // Add event handler
            //Aircraft.Fms.WaypointPassed += OnLanded;
        }

        public bool HandleCommand(SimAircraft aircraft, Action<string> logger, string runway)
        {
            Aircraft = aircraft;
            Logger = logger;
            // Find Waypoint
            Localizer wp = DataHandler.GetLocalizer(DataHandler.FAKE_AIRPORT_NAME, runway);

            if (wp == null)
            {
                Logger?.Invoke($"ERROR: Threshold {runway} not found!");
                return false;
            }

            _loc = wp;

            Logger?.Invoke($"{Aircraft.Callsign} Taking off Runway {runway}");

            return true;
        }

        public bool HandleCommand(ref List<string> args)
        {
            // Check argument length
            if (args.Count < 1)
            {
                Logger?.Invoke($"ERROR: Takeoff requires at least 1 arguments!");
                return false;
            }

            // Get runway string
            string rwyStr = args[0];

            args.RemoveAt(0);

            // Find Waypoint
            Localizer wp = DataHandler.GetLocalizer(DataHandler.FAKE_AIRPORT_NAME, rwyStr);
            
            if (wp == null)
            {
                Logger?.Invoke($"ERROR: Runway {rwyStr} not found!");
                return false;
            }

            _loc = wp;

            Logger?.Invoke($"{Aircraft.Callsign} Taking off Runway {rwyStr}");

            return true;
        }
    }
}
