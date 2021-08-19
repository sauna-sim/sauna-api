using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS.Legs;

namespace VatsimAtcTrainingSimulator.Core.Data.Procedures.Approaches
{
    public class ProcedureSegment
    {
        private List<IRouteLeg> _routeLegs;

        public ProcedureSegment(List<IRouteLeg> legs)
        {
            _routeLegs = legs;
        }

        public ProcedureSegment() :
            this(new List<IRouteLeg>())
        { }

        public List<IRouteLeg> RouteLegs => _routeLegs.ToList();
    }
}
