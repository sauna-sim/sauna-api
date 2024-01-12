using AviationCalcUtilNet.GeoTools;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.Fixes
{
    public abstract class Fix
    {
        /// <summary>
        /// The 'standard' identifier for this fix. 
        /// This is usually related to their full name, but abbreviated to fit within certain restrictions.
        /// </summary>
        public string Identifier { get; }
        public GeoPoint Location { get; }

        /// <summary>
        /// The full name for this fix.
        /// For example, a VOR might have a three letter identifier of LAM, but its full name is LAMBOURNE.
        /// For waypoints, this is usually the same as their identifier, but it can vary, for example, for VRPs.
        /// </summary>
        public string Name { get; }

        protected Fix(string identifier, string name, GeoPoint location)
        {
            Identifier = identifier;
            Location = location;
        }
    }
}