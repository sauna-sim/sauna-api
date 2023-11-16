using SaunaSim.Core;
using SaunaSim.Core.Simulator.Aircraft;
using System.Collections.Generic;

namespace SaunaSim.Api.ApiObjects.Commands
{
    public class TextCommandRequest
    {
        public string Callsign { get; set; }
        public string Command { get; set; }
        public List<string> Args { get; set; }
    }
}