using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Aircraft.Autopilot.Controller;
using SaunaSim.Core.Simulator.Aircraft.FMS;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Simulator.Commands
{
    public class GoAround : IAircraftCommand
    {
        public SimAircraft Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private int _gaAlt;

        public void ExecuteCommand()
        {
            if (!Aircraft.Position.OnGround && Aircraft.Autopilot.CurrentLateralMode == LateralModeType.APCH)
            {
                Aircraft.Autopilot.CurrentLateralMode = LateralModeType.HDG;
                Aircraft.Autopilot.SelectedHeading = (int)Aircraft.Position.Heading_Mag;
                Aircraft.Autopilot.CurrentVerticalMode = VerticalModeType.FLCH;
                Aircraft.Autopilot.SelectedAltitude = _gaAlt;

                Aircraft.Fms.PhaseType = FmsPhaseType.GO_AROUND;
            }
        }
        public bool HandleCommand(ref List<string> args)
        {
            if (args.Count < 2)
            {
                Logger?.Invoke($"ERROR: Go Around requires at least 2 arguments!");
                return false;
            }

            if(Aircraft.Position.TrueAltitude < Math.Abs(Aircraft.airportElev + 200))
            {
                Logger?.Invoke($"ERROR: Go Around requires at least 2 arguments!");
                return false;
            }

            // Get runway string
            string rwyStr = args[0];
            args.RemoveAt(0);

            _gaAlt = Convert.ToInt32(args[0]);
            args.RemoveAt(0);

            if(Aircraft.Position.OnGround)
            {
                Logger?.Invoke($"ERROR: Aircraft on the ground!");
                return false;
            }
            else if(Aircraft.Autopilot.CurrentLateralMode != LateralModeType.APCH)
            {
                Logger?.Invoke($"ERROR: Aircraft not on an Approach!");
                return false;
            }

            Logger?.Invoke($"{Aircraft.Callsign} Going Around Runway {rwyStr}");

            return true;
        }
    }
}
