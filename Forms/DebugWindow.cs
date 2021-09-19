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
    public partial class DebugWindow : Form
    {
        public event EventHandler FormCloseEvent;

        public DebugWindow()
        {
            InitializeComponent();
        }

        public void LogMessage(string msg)
        {
            debugConsoleBox.Invoke((MethodInvoker)delegate ()
            {
                debugConsoleBox.AppendText($"{msg}\r\n");
            });
        }

        private void DebugWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                FormCloseEvent.Invoke(this, new EventArgs());
            }
        }
    }
}
