using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VatsimAtcTrainingSimulator.Core;
using VatsimAtcTrainingSimulator.Core.Simulator;

namespace VatsimAtcTrainingSimulator
{
    public partial class CommandWindow : Form
    {
        public event EventHandler FormCloseEvent;
        public bool Docked { get; set; }
        private List<IVatsimClient> Clients { get; set; } = new List<IVatsimClient>();
        private Dictionary<string, IAircraftCommand> Commands = new Dictionary<string, IAircraftCommand>();
        private int snapDist = 30;

        public CommandWindow(List<IVatsimClient> clients)
        {
            Clients = clients;
            InitializeComponent();

            // Get types
            List<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IAircraftCommand).IsAssignableFrom(p) && p.GetInterfaces().Contains(typeof(IAircraftCommand)))
                .ToList();

            foreach (Type type in types)
            {
                IAircraftCommand cmd = (IAircraftCommand)Activator.CreateInstance(type);
                cmd.Logger = LogMessage;
                Commands.Add(cmd.CommandName, cmd);
            }
        }

        public void LogMessage(string msg)
        {
            outputWindow.AppendText($"{msg}\r\n");
        }

        private void CommandWindow_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void CommandWindow_LocationChanged(object sender, EventArgs e)
        {
            if (Owner != null)
            {
                if (Math.Abs(Owner.Right - Left) < snapDist && Math.Abs(Owner.Top - Top) < snapDist)
                {
                    Docked = true;
                    Left = Owner.Right;
                    Top = Owner.Top;
                    Height = Owner.Height;
                } else
                {
                    Docked = false;
                }
            }
        }

        private void CommandWindow_Load(object sender, EventArgs e)
        {

        }

        private void CommandWindow_SizeChanged(object sender, EventArgs e)
        {
            if (Owner != null && Docked)
            {
                Height = Owner.Height;
            }
        }

        private void CommandWindow_Move(object sender, EventArgs e)
        {

        }

        private void commandInputBx_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                sendBtn_Click(this, new EventArgs());
            }
        }

        private void sendBtn_Click(object sender, EventArgs e)
        {
            if (commandInputBx.Text.Length > 0 && Owner != null)
            {
                // Process Input
                List<string> split = commandInputBx.Text.Split(' ').ToList();

                VatsimClientPilot aircraft = null;
                foreach (IVatsimClient client in Clients)
                {
                    if (client is VatsimClientPilot testPilot)
                    {
                        // Match callsign
                        if (testPilot.Callsign.ToLower().Contains(split[0].ToLower())){
                            aircraft = testPilot;
                            break;
                        }
                    }
                }

                split.RemoveAt(0);

                // If we didn't find any aircraft
                if (aircraft == null)
                {
                    outputWindow.AppendText($"ERROR: {split[0]} was not found in the aircraft list!\r\n");
                } else
                {
                    // Loop through command list
                    while (split.Count > 0)
                    {
                        // Get command name
                        string command = split[0].ToLower();
                        split.RemoveAt(0);

                        // Get command
                        if (Commands.TryGetValue(command, out IAircraftCommand cmd))
                        {
                            split = cmd.HandleCommand(aircraft, split);
                        }
                        else
                        {
                            outputWindow.AppendText($"ERROR: Command {command} not valid!\r\n");
                        }
                    }
                }

                // Clear command box
                commandInputBx.ResetText();
            }
        }
    }
}
