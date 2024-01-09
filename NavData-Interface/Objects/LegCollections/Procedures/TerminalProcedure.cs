using NavData_Interface.Objects.LegCollections.Legs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NavData_Interface.Objects.LegCollections.Procedures
{
    public abstract class TerminalProcedure : LegCollection
    {
        /// <summary>
        /// The identifier for the airport that this route is associated with
        /// </summary>
        public string AirportIdentifier { get; }

        /// <summary>
        /// The identifier for this terminal procedure. 
        /// <br></br>
        /// For SIDs and STARs, this will be the 5-letter identifier. 
        /// <para></para>
        /// For approaches, the identifier follows a different format: 
        /// <br></br>
        /// <list type="bullet">
        /// <item>The first character indicates the type of approach (ILS, IGS, VOR...)</item>
        /// <item>The next three characters indicate the runway.</item>
        /// <item>The next character indicates, if any, the 'variant' of the approach e.g ILS Y vs ILS Z</item>
        /// </list>
        /// 
        /// </summary>
        public string RouteIdentifier { get; }

        public TerminalProcedure(string airportIdentifier, string routeIdentifier)
        {
            AirportIdentifier = airportIdentifier;
            RouteIdentifier = routeIdentifier;
        }
    }
}
