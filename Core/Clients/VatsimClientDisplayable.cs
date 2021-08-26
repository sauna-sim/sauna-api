using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft;
using VatsimAtcTrainingSimulator.Core.Simulator.Aircraft.Control.FMS.Legs;

namespace VatsimAtcTrainingSimulator.Core.Clients
{
    public class VatsimClientDisplayable
    {
        public VatsimClientDisplayable(IVatsimClient client)
        {
            this.client = client;
        }

        [Browsable(false)]
        public IVatsimClient client { get; private set; }

        public string Callsign => client.Callsign;

        public string Type
        {
            get
            {
                if (client is VatsimClientPilot)
                {
                    return "Aircraft";
                }
                return "None";
            }
        }

        [DisplayName("Connection")]
        public CONN_STATUS ConnectionStatus => client.ConnectionStatus;

        public string Paused
        {
            get
            {
                if (client is VatsimClientPilot pilot && pilot.Paused)
                {
                    return "Paused";
                }
                return "Unpaused";
            }
        }

        private int RoundDoubles(double input)
        {
            try
            {
                return Convert.ToInt32(Math.Round(input, MidpointRounding.AwayFromZero));
            } catch (OverflowException)
            {
                return -1;
            }
        }

        [DisplayName("Heading (Magnetic)")]
        public int Heading
        {
            get
            {
                if (client is VatsimClientPilot pilot)
                {
                    return RoundDoubles(pilot.Position.Heading_Mag);
                }
                return -1;
            }
        }

        [DisplayName("Airspeed (KIAS)")]
        public int Airspeed
        {
            get
            {
                if (client is VatsimClientPilot pilot)
                {
                    return RoundDoubles(pilot.Position.IndicatedAirSpeed);
                }
                return -1;
            }
        }

        [DisplayName("Altitude (ft)")]
        public int Altitude
        {
            get
            {
                if (client is VatsimClientPilot pilot)
                {
                    return RoundDoubles(pilot.Position.IndicatedAltitude);
                }
                return -1;
            }
        }

        [DisplayName("Altimeter Setting")]
        public string AltimeterSetting
        {
            get
            {
                if (client is VatsimClientPilot pilot)
                {
                    return $"{RoundDoubles(pilot.Position.AltimeterSetting_hPa)}hPa";
                }
                return "";
            }
        }

        [DisplayName("Wind")]
        public string Wind
        {
            get
            {
                if (client is VatsimClientPilot pilot)
                {
                    return $"{RoundDoubles(pilot.Position.WindDirection)} @ {RoundDoubles(pilot.Position.WindSpeed)}kts";
                }
                return "";
            }
        }

        [DisplayName("Lateral Mode")]
        public string LatMode
        {
            get
            {
                if (client is VatsimClientPilot pilot && pilot.Control != null)
                {
                    return pilot.Control.CurrentLateralInstruction.ToString();
                }
                return "";
            }
        }

        [DisplayName("Armed Lateral Mode")]
        public string ArmedLatMode
        {
            get
            {
                if (client is VatsimClientPilot pilot && pilot.Control != null)
                {
                    if (pilot.Control.ArmedLateralInstruction != null)
                    {
                        return pilot.Control.ArmedLateralInstruction.ToString();
                    }
                    return "None";
                }
                return "";
            }
        }

        [DisplayName("Vertical Mode")]
        public string VertMode
        {
            get
            {
                if (client is VatsimClientPilot pilot && pilot.Control != null)
                {
                    return pilot.Control.CurrentVerticalInstruction.ToString();
                }
                return "";
            }
        }

        [DisplayName("Armed Vertical Mode")]
        public string ArmedVertMode
        {
            get
            {
                if (client is VatsimClientPilot pilot && pilot.Control != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (IVerticalControlInstruction instr in pilot.Control.ArmedVerticalInstructions)
                    {
                        sb.Append(instr.ToString());
                        sb.Append("; ");
                    }
                    return sb.ToString();
                }
                return "";
            }
        }

        [DisplayName("FMS Route")]
        public string FmsRoute
        {
            get
            {
                if (client is VatsimClientPilot pilot && pilot.Control != null && pilot.Control.FMS != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (IRouteLeg leg in pilot.Control.FMS.GetRouteLegs())
                    {
                        sb.Append(leg.ToString());
                        sb.Append("; ");
                    }
                    return sb.ToString();
                }
                return "";
            }
        }
    }
}
