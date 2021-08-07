using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator
{
    public interface IAircraftCommand
    { 
        string CommandName { get;}
        Action<string> Logger { get; set; }

        List<string> HandleCommand(VatsimClientPilot aircraft, List<string> args);
    }
}
