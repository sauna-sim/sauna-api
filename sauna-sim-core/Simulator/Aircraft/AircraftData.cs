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
        public int Config { get; set; }

        public double ThrustLeverPos { get; set; }

        public double ThrustLeverVel { get; set; }

        private double _spdBrakePos = 0;
        public double SpeedBrakePos
        {
            get => _spdBrakePos;
            set
            {
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
            }
        }

        public double Mass_kg { get; set; }

        public AircraftConfig AircraftConfig { get; set; }
    }
}
