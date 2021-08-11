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
using VatsimAtcTrainingSimulator.Core.Data;
using VatsimAtcTrainingSimulator.Core.GeoTools;

namespace VatsimAtcTrainingSimulator
{
    public partial class MainForm : Form
    {
        private CommandWindow commandWindow;

        public MainForm()
        {
            InitializeComponent();
            commandWindow = new CommandWindow();
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
                        XpdrMode xpdrMode = (XpdrMode)items[0].ToCharArray()[1];

                        VatsimClientPilot pilot = new VatsimClientPilot()
                        {
                            Logger = (string msg) =>
                            {
                                logMsg($"{callsign}: {msg}");
                            },
                            StatusChangeAction = (CONN_STATUS status) =>
                            {
                                if (status == CONN_STATUS.DISCONNECTED)
                                {
                                    connectionsList.Invoke(new MethodInvoker(delegate {
                                        for (int i = 0; i < connectionsList.Items.Count; i++)
                                        {
                                            if (connectionsList.Items[i].Text == callsign)
                                            {
                                                connectionsList.Items.RemoveAt(i);
                                                break;
                                            }
                                        }
                                    }));
                                    connectionsList.Invoke(new MethodInvoker(delegate { connectionsList.Refresh(); }));

                                    ClientsHandler.RemoveClientByCallsign(callsign);
                                }
                            }
                        };

                        if (ClientsHandler.GetClientByCallsign(pilot.Callsign) == null && await pilot.Connect(Properties.Settings.Default.server, Properties.Settings.Default.port, callsign, Properties.Settings.Default.cid, Properties.Settings.Default.password, "Simulator Pilot", Properties.Settings.Default.vatsimServer))
                        {
                            connectionsList.Items.Add(pilot.Callsign);
                            connectionsList.Refresh();
                            ClientsHandler.AddClient(pilot);

                            // Send init position
                            pilot.SetInitialData(xpdrMode, Convert.ToInt32(items[2]), Convert.ToInt32(items[3]), Convert.ToDouble(items[4]), Convert.ToDouble(items[5]), Convert.ToDouble(items[6]), 250, Convert.ToInt32(items[8]), Convert.ToInt32(items[9]));
                        }
                    }
                    else if (line.StartsWith("$FP"))
                    {
                        string callsign = line.Split(':')[0].Replace("$FP", "");

                        ClientsHandler.SendDataForClient(callsign, line);
                    }
                }
            }
        }

        private void pauseAllBtn_Click(object sender, EventArgs e)
        {
            if (ClientsHandler.AllPaused)
            {
                ClientsHandler.AllPaused = false;
                pauseAllBtn.Text = "Pause";
            }
            else
            {
                ClientsHandler.AllPaused = true;
                pauseAllBtn.Text = "Unpause";
            }
        }

        private void commandWindowMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (commandWindowMenuItem.Checked)
            {
                commandWindow.Show(this);
            }
            else
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
            ClientsHandler.DisconnectAllClients();
            connectionsList.Clear();
        }

        private void loadSectorFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Create and open file dialog
            OpenFileDialog fileDialog = new OpenFileDialog()
            {
                Title = "Open Sector File",
                Filter = "Sector File|*.sct2|Sector File (Old)|*.sct"
            };

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                // Read file
                string filename = fileDialog.FileName;
                string[] filelines = File.ReadAllLines(filename);

                string sectionName = "";

                // Loop through sector file
                foreach (string line in filelines)
                {
                    // Ignore comments
                    if (line.Trim().StartsWith(";"))
                    {
                        continue;
                    }

                    if (line.StartsWith("["))
                    {
                        // Get section name
                        sectionName = line.Replace("[", "").Replace("]", "").Trim();
                    }
                    else
                    {
                        NavaidType type = NavaidType.VOR;
                        string[] items;
                        switch (sectionName)
                        {
                            case "VOR":
                                type = NavaidType.VOR;
                                goto case "AIRPORT";
                            case "NDB":
                                type = NavaidType.NDB;
                                goto case "AIRPORT";
                            case "AIRPORT":
                                type = NavaidType.AIRPORT;

                                items = line.Split(' ');

                                if (items.Length >= 4)
                                {
                                    decimal freq = 0;
                                    try
                                    {
                                        freq = Convert.ToDecimal(items[1]);
                                    }
                                    catch (Exception) { }

                                    DataHandler.AddWaypoint(new WaypointNavaid(items[0], AcftGeoUtil.ConvertSectorFileDegMinSecToDecimalDeg(items[2]), AcftGeoUtil.ConvertSectorFileDegMinSecToDecimalDeg(items[3]), "", freq, type));
                                }
                                break;
                            case "FIXES":
                                items = line.Split(' ');

                                if (items.Length >= 3)
                                {
                                    DataHandler.AddWaypoint(new Waypoint(items[0], AcftGeoUtil.ConvertSectorFileDegMinSecToDecimalDeg(items[1]), AcftGeoUtil.ConvertSectorFileDegMinSecToDecimalDeg(items[2])));
                                }
                                break;
                        }
                    }
                }
            }
        }
    }
}
