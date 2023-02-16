using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaunaSim.Core.Data
{
    public class Localizer : WaypointNavaid
    {
        private double _course;
        public Localizer(string identifier, double lat, double lon, string name, decimal frequency, double course) : base(identifier, lat, lon, name, frequency, NavaidType.LOC)
        {
            this._course = course;
        }

        public double Course => _course;
    }
}
