using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AselAtcTrainingSim.AselSimCore
{
    public class AccConfigLights
    {
        public bool strobe_on { get; set; }
        public bool landing_on { get; set; }
        public bool taxi_on { get; set; }
        public bool beacon_on { get; set; }
        public bool nav_on { get; set; }
        public bool logo_on { get; set; }
    }
    
    public class AccConfig
    {
        public bool is_full_data { get; set;}
        public AccConfigLights lights { get; set; }
        public bool gear_down { get; set; }
        public int flaps_pct { get; set; }
        public bool spoilers_out { get; set; }
        public bool on_ground { get; set; }
    }
}
