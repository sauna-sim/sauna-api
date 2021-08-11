using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Data
{
    public class Waypoint
    {
        public string Identifier { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        public Waypoint(string identifier, double lat, double lon)
        {
            this.Identifier = identifier.ToUpper();
            this.Latitude = lat;
            this.Longitude = lon;
        }
    }
}
