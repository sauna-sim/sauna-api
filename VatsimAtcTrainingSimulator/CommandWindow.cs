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
    public partial class CommandWindow : Form
    {
        public event EventHandler FormCloseEvent;
        public bool Docked { get; set; }
        private int snapDist = 30;

        public CommandWindow()
        {
            InitializeComponent();
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
    }
}
