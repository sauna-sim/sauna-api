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

namespace VatsimAtcTrainingSimulator
{
    public partial class MainForm : Form
    {
        private List<IVatsimClient> clients;

        public MainForm()
        {
            InitializeComponent();
            clients = new List<IVatsimClient>();
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

        private async void addAcftBtn_Click(object sender, EventArgs e)
        {
            VatsimClientPilot pilot = new VatsimClientPilot()
            {
                Logger = logMsg
            };
            if (await pilot.Connect(Properties.Settings.Default.server, Properties.Settings.Default.port, "UAL" + clients.Count, Properties.Settings.Default.cid, Properties.Settings.Default.password, "Simulator Pilot", Properties.Settings.Default.vatsimServer))
            {
                connectionsList.Items.Add(pilot.Callsign);
                connectionsList.Refresh();
                clients.Add(pilot);
            }
        }

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Disconnect all clients first
            foreach (IVatsimClient client in clients)
            {
                await client.Disconnect();
            }
            clients = new List<IVatsimClient>();
            connectionsList.Clear();
        }
    }
}
