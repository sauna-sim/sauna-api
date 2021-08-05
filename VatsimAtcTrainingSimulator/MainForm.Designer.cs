using System.Drawing;
using System.Windows.Forms;

namespace VatsimAtcTrainingSimulator
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private const int HTCAPTION = 2;
        private const int WM_NCHITTEST = 0x84;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem("Test");
            this.headerPnl = new System.Windows.Forms.Panel();
            this.settingsBtn = new System.Windows.Forms.Button();
            this.addAcftBtn = new System.Windows.Forms.Button();
            this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.connectionsList = new System.Windows.Forms.ListView();
            this.msgBox = new System.Windows.Forms.TextBox();
            this.headerPnl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).BeginInit();
            this.mainSplitContainer.Panel1.SuspendLayout();
            this.mainSplitContainer.Panel2.SuspendLayout();
            this.mainSplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // headerPnl
            // 
            this.headerPnl.Controls.Add(this.settingsBtn);
            this.headerPnl.Controls.Add(this.addAcftBtn);
            this.headerPnl.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPnl.Location = new System.Drawing.Point(0, 0);
            this.headerPnl.Name = "headerPnl";
            this.headerPnl.Size = new System.Drawing.Size(584, 50);
            this.headerPnl.TabIndex = 0;
            // 
            // settingsBtn
            // 
            this.settingsBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.settingsBtn.Location = new System.Drawing.Point(93, 12);
            this.settingsBtn.Name = "settingsBtn";
            this.settingsBtn.Size = new System.Drawing.Size(75, 23);
            this.settingsBtn.TabIndex = 1;
            this.settingsBtn.Text = "Settings";
            this.settingsBtn.UseVisualStyleBackColor = true;
            this.settingsBtn.Click += new System.EventHandler(this.settingsBtn_Click);
            // 
            // addAcftBtn
            // 
            this.addAcftBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.addAcftBtn.Location = new System.Drawing.Point(12, 12);
            this.addAcftBtn.Name = "addAcftBtn";
            this.addAcftBtn.Size = new System.Drawing.Size(75, 23);
            this.addAcftBtn.TabIndex = 0;
            this.addAcftBtn.Text = "Add Aircraft";
            this.addAcftBtn.UseVisualStyleBackColor = true;
            this.addAcftBtn.Click += new System.EventHandler(this.addAcftBtn_Click);
            // 
            // mainSplitContainer
            // 
            this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainSplitContainer.Location = new System.Drawing.Point(0, 50);
            this.mainSplitContainer.Name = "mainSplitContainer";
            // 
            // mainSplitContainer.Panel1
            // 
            this.mainSplitContainer.Panel1.Controls.Add(this.connectionsList);
            // 
            // mainSplitContainer.Panel2
            // 
            this.mainSplitContainer.Panel2.Controls.Add(this.msgBox);
            this.mainSplitContainer.Size = new System.Drawing.Size(584, 311);
            this.mainSplitContainer.SplitterDistance = 194;
            this.mainSplitContainer.TabIndex = 1;
            // 
            // connectionsList
            // 
            this.connectionsList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.connectionsList.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1});
            this.connectionsList.Location = new System.Drawing.Point(0, 0);
            this.connectionsList.Name = "connectionsList";
            this.connectionsList.Size = new System.Drawing.Size(194, 311);
            this.connectionsList.TabIndex = 0;
            this.connectionsList.UseCompatibleStateImageBehavior = false;
            this.connectionsList.View = System.Windows.Forms.View.List;
            // 
            // msgBox
            // 
            this.msgBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.msgBox.Location = new System.Drawing.Point(0, 0);
            this.msgBox.Multiline = true;
            this.msgBox.Name = "msgBox";
            this.msgBox.ReadOnly = true;
            this.msgBox.Size = new System.Drawing.Size(386, 311);
            this.msgBox.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.mainSplitContainer);
            this.Controls.Add(this.headerPnl);
            this.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.MinimumSize = new System.Drawing.Size(600, 200);
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "VATSIM ATC Training Simulator";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.headerPnl.ResumeLayout(false);
            this.mainSplitContainer.Panel1.ResumeLayout(false);
            this.mainSplitContainer.Panel2.ResumeLayout(false);
            this.mainSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).EndInit();
            this.mainSplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Panel headerPnl;
        private Button addAcftBtn;
        private Button settingsBtn;
        private SplitContainer mainSplitContainer;
        private ListView connectionsList;
        private TextBox msgBox;
    }
}

