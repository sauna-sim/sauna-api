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

namespace SaunaSim.Core.Simulator.Commands
{
    public class IlsCommand : IAircraftCommand
    {
        public SimAircraft Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private Localizer _loc;

        public void ExecuteCommand()
        {
            // Create localizer (just a CourseToFixLeg) and glidepath (setting the angleConstraint on the leg)
            CourseToFixLeg locLeg = new CourseToFixLeg(new FmsPoint(new RouteWaypoint(_loc.Loc_location), RoutePointTypeEnum.FLY_OVER), BearingTypeEnum.MAGNETIC, _loc.Loc_bearing);
            locLeg.EndPoint.AngleConstraint = 3.0;

            Aircraft.Fms.AddRouteLeg(locLeg);

            // Activate leg now, skipping all previous legs

            Aircraft.Fms.ActivateNextLeg();

            foreach (IRouteLeg leg in Aircraft.Fms.GetRouteLegs())
            {
                if (leg.Equals(locLeg))
                {
                    break;
                }
            }

            Aircraft.Autopilot.SelectedFpa = locLeg.EndPoint.AngleConstraint;
            Aircraft.Autopilot.CurrentVerticalMode = VerticalModeType.APCH;
        }

        public void OnLanded(object sender, EventArgs e)
        {
            Aircraft.Dispose();
        }

        public bool HandleCommand(SimAircraft aircraft, Action<string> logger, string runway)
        {
            Aircraft = aircraft;
            Logger = logger;
            // Find Waypoint
            Localizer wp = DataHandler.GetLocalizer("_fake_airport", runway);

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
            Localizer wp = DataHandler.GetLocalizer("_fake_airport", rwyStr);
            
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
