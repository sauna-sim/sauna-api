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

        public double SpeedBrakePos { get; set; }

        public double Mass_kg { get; set; }

        public AircraftConfig AircraftConfig { get; set; }
    }
}
