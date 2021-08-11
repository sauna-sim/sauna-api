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
using VatsimAtcTrainingSimulator.Core.Simulator.Commands;

namespace VatsimAtcTrainingSimulator
{
    public partial class CommandWindow : Form
    {
        public event EventHandler FormCloseEvent;
        public bool Docked { get; set; }
        private int snapDist = 30;

        public CommandWindow()
        {
            InitializeComponent();            
        }

        public void LogMessage(string msg)
        {
            outputWindow.AppendText($"{msg}\r\n");
        }

        private void CommandWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                FormCloseEvent.Invoke(this, new EventArgs());
            }
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

                // Handle Pause/Unpause All


                IVatsimClient aircraft = ClientsHandler.GetClientWhichContainsCallsign(split[0]);

                split.RemoveAt(0);

                // If we didn't find any aircraft
                if (aircraft == null || !(aircraft is VatsimClientPilot))
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

                        split = CommandHandler.HandleCommand(command, (VatsimClientPilot) aircraft, split, LogMessage);
                    }
                }

                // Clear command box
                commandInputBx.ResetText();
            }
        }
    }
}
