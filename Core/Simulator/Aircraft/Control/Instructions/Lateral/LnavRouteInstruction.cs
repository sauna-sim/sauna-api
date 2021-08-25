using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS.Legs;

namespace VatsimAtcTrainingSimulator.Core.Simulator.Aircraft
{
    public class LnavRouteInstruction : ILateralControlInstruction
    {
        private IRouteLeg _currentLeg;

        public LateralControlMode Type => LateralControlMode.NAV_ROUTE;

        public LnavRouteInstruction()
        {
        }

        public bool ShouldActivateInstruction(AircraftPosition position, AircraftFms fms, int posCalcInterval)
        {
            IRouteLeg leg = fms.GetFirstLeg();

            if (leg == null)
            {
                return false;
            }

            return leg.ShouldBeginTurn(position, fms, posCalcInterval);
        }

        public void UpdatePosition(ref AircraftPosition position, ref AircraftFms fms, int posCalcInterval)
        {
            if (fms.ActiveLeg == null)
            {
                IRouteLeg leg = fms.GetFirstLeg();

                // If there is no leg, hold current track
                if (leg == null)
                {
                    new TrackHoldInstruction(position.Track_True).UpdatePosition(ref position, ref fms, posCalcInterval);
                    return;
                }

                fms.ActivateNextLeg();
            }

            _currentLeg = fms.ActiveLeg;

            // Update position
            fms.ActiveLeg.UpdateLateralPosition(ref position, ref fms, posCalcInterval);
        }

        public override string ToString()
        {
            if (_currentLeg != null)
            {
                string instrStr = _currentLeg.Instruction != null ? _currentLeg.Instruction.ToString() : "";
                return $"LNAV {_currentLeg} {instrStr}";
            }

            return "LNAV";
        }
    }
}
