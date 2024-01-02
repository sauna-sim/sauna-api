using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FsdConnectorNet;

namespace SaunaSim.Core.Simulator.Aircraft
{
    public class AircraftData
    {
        private SimAircraft _parentAircraft;

        public AircraftData(SimAircraft parentAircraft)
        {
            _parentAircraft = parentAircraft;
        }

        private int _config;
        public int Config {
            get => _config;
            set
            {
                if (value > _parentAircraft.PerformanceData.ConfigList.Count - 1)
                {
                    _config = _parentAircraft.PerformanceData.ConfigList.Count - 1;
                } else {
                    _config = value;
                }

                // Update FSD
                var flapsPct = (double)_config / _parentAircraft.PerformanceData.ConfigList.Count;

                _parentAircraft.Connection.SetFlapsPct((int)(flapsPct * 100.0));
                _parentAircraft.Connection.SetGearDown(_parentAircraft.PerformanceData.ConfigList[_config].GearDown);
            }
        }

        public double ThrustLeverPos { get; set; }

        public double ThrustLeverVel { get; set; }

        private double _spdBrakePos = 0;
        public double SpeedBrakePos
        {
            get => _spdBrakePos;
            set
            {
                var oldValue = _spdBrakePos;
                if (value < 0)
                {
                    _spdBrakePos = 0;
                } else if (value > 1)
                {
                    _spdBrakePos = 1;
                } else
                {
                    _spdBrakePos = value;
                }

                // Update FSD
                if (oldValue != _spdBrakePos)
                {
                    _parentAircraft.Connection.SetSpoilersDeployed(_spdBrakePos > 0);
                }
            }
        }

        public double Mass_kg { get; set; }

        public AircraftConfig AircraftConfig { get; set; }
    }
}
