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
                                    connectionsList.Invoke(new MethodInvoker(delegate { connectionsList.Items.RemoveByKey(callsign); }));
                                    connectionsList.Invoke(new MethodInvoker(delegate { connectionsList.Refresh(); }));

                                    ClientsHandler.RemoveClientByCallsign(callsign);
                                }
                            }
                        };

                        if (await pilot.Connect(Properties.Settings.Default.server, Properties.Settings.Default.port, callsign, Properties.Settings.Default.cid, Properties.Settings.Default.password, "Simulator Pilot", Properties.Settings.Default.vatsimServer))
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
    }
}
