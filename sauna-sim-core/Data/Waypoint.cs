using AviationCalcUtilNet.GeoTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaunaSim.Core.Data
{
    /*public class Waypoint
    {
        private string _identifier;
        private GeoPoint _pointPosition;

        public Waypoint(string identifier, double lat, double lon)
        {
            _identifier = identifier.ToUpper();
            _pointPosition = new GeoPoint(lat, lon);
        }

        public double Latitude => _pointPosition.Lat;

        public double Longitude => _pointPosition.Lon;

        public GeoPoint Location => _pointPosition;

        public string Identifier => _identifier;

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Waypoint wpt = (Waypoint)obj;
            return wpt._pointPosition == _pointPosition && _identifier == wpt.Identifier;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return _pointPosition.GetHashCode();
        }
    }*/
}
