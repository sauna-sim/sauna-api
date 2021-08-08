using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VatsimAtcTrainingSimulator
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            serverTxtBx.Text = Properties.Settings.Default.server;
            cidTxtBx.Text = Properties.Settings.Default.cid;
            passTxtBx.Text = Properties.Settings.Default.password;
            vatsimServerChxBx.Checked = Properties.Settings.Default.vatsimServer;
            portNumBx.Value = Properties.Settings.Default.port;
            protocolVersionInputBx.Value = Properties.Settings.Default.protocol;
            posSendIntvlBx.Value = Properties.Settings.Default.updateRate;

            cmdFreqInput.Value = Properties.Settings.Default.commandFrequency;
        }

        private void closeBtn_Click(object sender, EventArgs e)
        {
            // Server Settings
            Properties.Settings.Default.server = serverTxtBx.Text;
            Properties.Settings.Default.cid = cidTxtBx.Text;
            Properties.Settings.Default.password = passTxtBx.Text;
            Properties.Settings.Default.vatsimServer = vatsimServerChxBx.Checked;
            Properties.Settings.Default.port = Convert.ToInt32(portNumBx.Value);
            Properties.Settings.Default.protocol = Convert.ToInt32(protocolVersionInputBx.Value);
            Properties.Settings.Default.updateRate = Convert.ToInt32(posSendIntvlBx.Value);

            // Command Input Settings
            Properties.Settings.Default.commandFrequency = cmdFreqInput.Value;
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
