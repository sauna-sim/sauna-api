using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaunaSim.Core.Simulator.Commands
{
    public interface IAircraftCommand
    {
        VatsimClientPilot Aircraft { get; set; }

        Action<string> Logger { get; set; }

        void ExecuteCommand();

        bool HandleCommand(ref List<string> args);
    }
}
