using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AselAtcTrainingSim.AselSimCore.Data;
using AselAtcTrainingSim.AselSimCore.Simulator.Aircraft;
using AselAtcTrainingSim.AselSimCore.Simulator.Aircraft.Control.FMS;
using AselAtcTrainingSim.AselSimCore.Simulator.Aircraft.Control.Instructions.Lateral;
using AselAtcTrainingSim.AselSimCore.Simulator.Aircraft.Control.Instructions.Vertical;

namespace AselAtcTrainingSim.AselSimCore.Simulator.Commands
{
    public class IlsCommand : IAircraftCommand
    {
        public VatsimClientPilot Aircraft { get; set; }
        public Action<string> Logger { get; set; }

        private Localizer _loc;

        public void ExecuteCommand()
        {

            if ((Aircraft.Control.CurrentLateralInstruction is IlsApproachInstruction instr1) && instr1.Type == LateralControlMode.APPROACH)
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
            Aircraft.Control.AddArmedVerticalInstruction(new GlidePathInstruction(_loc.Location, 3));
        }

        public void OnLanded(object sender, EventArgs e)
        {
            _ = Aircraft.Disconnect();
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
            Waypoint wp = DataHandler.GetClosestWaypointByIdentifier($"ILS{rwyStr}", Aircraft.Position.Latitude, Aircraft.Position.Longitude);

            if (wp == null || !(wp is Localizer))
            {
                Logger?.Invoke($"ERROR: Localizer {rwyStr} not found!");
                return false;
            }

            _loc = (Localizer)wp;

            Logger?.Invoke($"{Aircraft.Callsign} flying ILS {rwyStr}");

            return true;
        }
    }
}
