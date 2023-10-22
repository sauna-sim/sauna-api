using AviationCalcUtilNet.GeoTools;
using NavData_Interface.Objects.Fix.Navaid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaunaSim.Core.Data
{
    public class Localizer : VhfNavaid
    {
        private double _course;
        public Localizer(string identifier, double lat, double lon, string name, decimal frequency, double course) : 
            base (new GeoPoint(lat, lon), "", "", "", identifier, name, (double)frequency, "", new GeoPoint(lat, lon), 0, 0, 999, 0)
        {
            this._course = course;
        }

        public double Course => _course;
    }
}
