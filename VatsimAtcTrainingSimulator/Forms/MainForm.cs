using CoordinateSharp;
using CoordinateSharp.Magnetic;
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
using VatsimAtcTrainingSimulator.Core.GeoTools.Helpers;
using VatsimAtcTrainingSimulator.Core.Simulator.AircraftControl;
using VatsimAtcTrainingSimulator.Core.Simulator.Commands;

namespace VatsimAtcTrainingSimulator
{
    public partial class MainForm : Form
    {
        private CommandWindow commandWindow;
        private DebugWindow debugWindow;

        public MainForm()
        {
            InitializeComponent();

            commandWindow = new CommandWindow();
            commandWindow.Show(this);
            commandWindow.FormCloseEvent += commandWindow_Closed;

            debugWindow = new DebugWindow();
            debugWindow.Show(this);
            debugWindow.FormCloseEvent += debugWindow_Closed;

            clientsDataGridView.DataSource = ClientsHandler.DisplayableList;
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
                debugWindow.Invoke((MethodInvoker)delegate ()
                {
                    if (debugWindow.Visible)
                    {
                        debugWindow.LogMessage(msg);
                    }
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

                string lastCallsign = "";

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
                            }
                        };

                        lastCallsign = callsign;

                        if (ClientsHandler.GetClientByCallsign(callsign) == null && await pilot.Connect(Properties.Settings.Default.server, Properties.Settings.Default.port, callsign, Properties.Settings.Default.cid, Properties.Settings.Default.password, "Simulator Pilot", Properties.Settings.Default.vatsimServer))
                        {
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
                    else if (line.StartsWith("REQALT"))
                    {
                        IVatsimClient acft = ClientsHandler.GetClientByCallsign(lastCallsign);

                        string[] items = line.Split(':');

                        if (acft is VatsimClientPilot pilot && pilot != null && items.Length >= 3)
                        {
                            try
                            {
                                int reqAlt = Convert.ToInt32(items[2]);
                                reqAlt /= 100;

                                List<string> args = new List<string>
                                {
                                    $"FL{reqAlt}"
                                };
                                CommandHandler.HandleCommand("dm", pilot, args, logMsg);
                            }
                            catch (Exception) { }
                        }
                    }
                    else if (line.StartsWith("$ROUTE"))
                    {
                        IVatsimClient acft = ClientsHandler.GetClientByCallsign(lastCallsign);

                        string[] items = line.Split(':');

                        if (acft is VatsimClientPilot pilot && pilot != null && items.Length >= 2)
                        {
                            string[] waypoints = items[1].Split(' ');

                            foreach (string wp in waypoints)
                            {
                                pilot.Position.Route.Enqueue(wp);
                            }

                            Waypoint nextWp = DataHandler.GetClosestWaypointByIdentifier(pilot.Position.Route.Peek(), pilot.Position.Latitude, pilot.Position.Longitude);

                            if (nextWp != null)
                            {
                                pilot.Position.Route.Dequeue();
                                double course = AcftGeoUtil.CalculateDirectBearingAfterTurn(
                                    new GeoPoint(pilot.Position.Latitude, pilot.Position.Longitude, pilot.Position.AbsoluteAltitude),
                                    new GeoPoint(nextWp.Latitude, nextWp.Longitude),
                                    AcftGeoUtil.CalculateRadiusOfTurn(AcftGeoUtil.CalculateBankAngle(pilot.Position.GroundSpeed, 25, 3), pilot.Position.GroundSpeed),
                                    pilot.Position.Track_True);

                                if (course >= 0)
                                {
                                    InterceptCourseInstruction instr = new InterceptCourseInstruction(nextWp)
                                    {
                                        TrueCourse = course
                                    };

                                    pilot.Control.CurrentLateralInstruction = instr;
                                }
                            }
                        }
                    }
                    else if (line.StartsWith("ILS"))
                    {
                        string[] items = line.Split(':');
                        string wpId = items[0];

                        try
                        {
                            double lat = Convert.ToDouble(items[1]);
                            double lon = Convert.ToDouble(items[2]);

                            DataHandler.AddWaypoint(new Waypoint(wpId, lat, lon));
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Well that didn't work did it.");
                        }
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

        private void debugWindow_Closed(object sender, EventArgs e)
        {
            debugConsoleToolStripMenuItem.Checked = false;
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

        private void debugConsoleToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (debugConsoleToolStripMenuItem.Checked)
            {
                debugWindow.Show(this);
            }
            else
            {
                debugWindow.Hide();
            }
        }

        private void dataGridUpdateTimer_Tick(object sender, EventArgs e)
        {
            clientsDataGridView.Refresh();
        }
    }
}
