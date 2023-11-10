using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NavData_Interface.Objects;
using NavData_Interface.Objects.Fix;
using SaunaSim.Core.Data;
using SaunaSim.Core.Simulator.Aircraft;
using SaunaSim.Core.Simulator.Aircraft.Control.Instructions.Lateral;
using SaunaSim.Core.Simulator.Aircraft.Control.Instructions.Vertical;

namespace SaunaSim.Core.Simulator.Commands
{
    public class IlsCommand : IAircraftCommand
    {
        public SimAircraft Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private Localizer _loc;

        public void ExecuteCommand()
        {

            /*if ((Aircraft.Control.CurrentLateralInstruction is IlsApproachInstruction instr1) && instr1.Type == LateralControlMode.APPROACH)
            {
                instr1.AircraftLanded += OnLanded;
            }
            else if ((Aircraft.Control.ArmedLateralInstruction is IlsApproachInstruction instr2) && instr2.Type == LateralControlMode.APPROACH)
            {
                instr2.AircraftLanded += OnLanded;
            }
            else
            {
                IlsApproachInstruction instr = new IlsApproachInstruction(_loc);
                instr.AircraftLanded += OnLanded;

                Aircraft.Control.ArmedLateralInstruction = instr;
            }
            Aircraft.Control.AddArmedVerticalInstruction(new GlidePathInstruction(_loc.Location, 3));*/
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
