using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VatsimAtcTrainingSimulator.Core;

namespace VatsimAtcTrainingSimulator
{
    public partial class MainForm : Form
    {
        private List<IVatsimClient> clients;
        private bool AllPaused = true;
        private CommandWindow commandWindow;

        public MainForm()
        {
            InitializeComponent();
            clients = new List<IVatsimClient>();
            commandWindow = new CommandWindow(clients);
            commandWindow.Show(this);
            commandWindow.FormCloseEvent += commandWindow_Closed;
        }

        private void settingsBtn_Click(object sender, EventArgs e)
        {
            SettingsForm sForm = new SettingsForm();
            sForm.ShowDialog();
        }

        public void logMsg(string msg)
        {
            try
            {
                msgBox.Invoke((MethodInvoker)delegate ()
                {
                    msgBox.AppendText(msg + "\r\n");
                });
            }
            catch (InvalidOperationException) { }
        }

        private async void euroscopeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Create and open file dialog
            OpenFileDialog fileDialog = new OpenFileDialog()
            {
                Title = "Open ES Scenario",
                Filter = "ES Scenario File|*.txt"
            };

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                // Read file
                string filename = fileDialog.FileName;
                string[] filelines = File.ReadAllLines(filename);

                foreach (string line in filelines)
                {
                    // Create pilot and update position
                    if (line.StartsWith("@N"))
                    {
                        string[] items = line.Split(':');
                        string callsign = items[1];
                        XpdrMode xpdrMode = (XpdrMode) items[0].ToCharArray()[1];

                        VatsimClientPilot pilot = new VatsimClientPilot()
                        {
                            Logger = logMsg
                        };

                        if (await pilot.Connect(Properties.Settings.Default.server, Properties.Settings.Default.port, callsign, Properties.Settings.Default.cid, Properties.Settings.Default.password, "Simulator Pilot", Properties.Settings.Default.vatsimServer))
                        {
                            connectionsList.Items.Add(pilot.Callsign);
                            connectionsList.Refresh();
                            clients.Add(pilot);

                            // Send init position
                            pilot.SetInitialData(xpdrMode, Convert.ToInt32(items[2]), Convert.ToInt32(items[3]), Convert.ToDouble(items[4]), Convert.ToDouble(items[5]), Convert.ToDouble(items[6]), 250, Convert.ToInt32(items[8]), Convert.ToInt32(items[9]));
                        }
                    } else if (line.StartsWith("$FP"))
                    {
                        string callsign = line.Split(':')[0].Replace("$FP", "");
                        foreach (IVatsimClient client in clients)
                        {
                            if (client.Callsign.Equals(callsign))
                            {
                                await client.ConnHandler.SendData(line);
                            }
                        }
                    }
                }
            }
        }

        private void pauseAllBtn_Click(object sender, EventArgs e)
        {
            if (AllPaused)
            {
                foreach (IVatsimClient client in clients){
                    if (client is VatsimClientPilot)
                    {
                        ((VatsimClientPilot)client).Paused = false;
                    }
                }

                AllPaused = false;
                pauseAllBtn.Text = "Pause";
            } else
            {
                foreach (IVatsimClient client in clients)
                {
                    if (client is VatsimClientPilot)
                    {
                        ((VatsimClientPilot)client).Paused = true;
                    }
                }

                AllPaused = true;
                pauseAllBtn.Text = "Unpause";
            }
        }

        private void commandWindowMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (commandWindowMenuItem.Checked)
            {
                commandWindow.Show(this);
            } else
            {
                commandWindow.Hide();
            }
        }

        private void commandWindow_Closed(object sender, EventArgs e)
        {
            commandWindowMenuItem.Checked = false;
        }

        private void MainForm_LocationChanged(object sender, EventArgs e)
        {
            if (commandWindow.Docked && commandWindowMenuItem.Checked)
            {
                commandWindow.Left = this.Right;
                commandWindow.Top = this.Top;
            }
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            if (commandWindow.Docked && commandWindowMenuItem.Checked)
            {
                commandWindow.Height = this.Height;
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Disconnect all clients first
            foreach (IVatsimClient client in clients)
            {
                client.Disconnect();
            }
            connectionsList.Clear();
        }
    }
}
