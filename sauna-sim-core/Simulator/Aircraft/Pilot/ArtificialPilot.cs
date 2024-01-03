using SaunaSim.Core.Simulator.Aircraft.FMS;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaunaSim.Core.Simulator.Aircraft.Pilot
{
    public class ArtificialPilot
    {
        private SimAircraft _parentAircraft;

        public ArtificialPilot(SimAircraft parentAircraft)
        {
            _parentAircraft = parentAircraft;
        }

        public void OnPositionUpdate(int intervalMs)
        {
            SetConfig();
        }

        private void SetConfig()
        {
            if(_parentAircraft.Fms.PhaseType == FmsPhaseType.CLIMB && _parentAircraft.Fms.FmsSpeedValue >= 210)
            {
                _parentAircraft.Data.Config = 0;
            }
            else if(_parentAircraft.Fms.PhaseType == FmsPhaseType.APPROACH && _parentAircraft.Fms.FmsSpeedValue <= 135)
            {
                _parentAircraft.Data.Config = 4;
            }
            else if (_parentAircraft.Fms.PhaseType == FmsPhaseType.APPROACH && _parentAircraft.Fms.FmsSpeedValue <= 160)
            {
                _parentAircraft.Data.Config = 3;
            }
            else if (_parentAircraft.Fms.PhaseType == FmsPhaseType.APPROACH && _parentAircraft.Fms.FmsSpeedValue <= 180)
            {
                _parentAircraft.Data.Config = 2;
            }
        }
    }
}
