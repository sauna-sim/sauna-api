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
            this.headerPnl = new System.Windows.Forms.Panel();
            this.pauseAllBtn = new System.Windows.Forms.Button();
            this.settingsBtn = new System.Windows.Forms.Button();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openScenarioToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.euroscopeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.connectionsList = new System.Windows.Forms.ListView();
            this.msgBox = new System.Windows.Forms.TextBox();
            this.headerPnl.SuspendLayout();
            this.menuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).BeginInit();
            this.mainSplitContainer.Panel1.SuspendLayout();
            this.mainSplitContainer.Panel2.SuspendLayout();
            this.mainSplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // headerPnl
            // 
            this.headerPnl.Controls.Add(this.pauseAllBtn);
            this.headerPnl.Controls.Add(this.settingsBtn);
            this.headerPnl.Controls.Add(this.menuStrip);
            this.headerPnl.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPnl.Location = new System.Drawing.Point(0, 0);
            this.headerPnl.Name = "headerPnl";
            this.headerPnl.Size = new System.Drawing.Size(584, 57);
            this.headerPnl.TabIndex = 0;
            // 
            // pauseAllBtn
            // 
            this.pauseAllBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.pauseAllBtn.Location = new System.Drawing.Point(12, 28);
            this.pauseAllBtn.Name = "pauseAllBtn";
            this.pauseAllBtn.Size = new System.Drawing.Size(75, 23);
            this.pauseAllBtn.TabIndex = 2;
            this.pauseAllBtn.Text = "Unpause All";
            this.pauseAllBtn.UseVisualStyleBackColor = true;
            this.pauseAllBtn.Click += new System.EventHandler(this.pauseAllBtn_Click);
            // 
            // settingsBtn
            // 
            this.settingsBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.settingsBtn.Location = new System.Drawing.Point(93, 28);
            this.settingsBtn.Name = "settingsBtn";
            this.settingsBtn.Size = new System.Drawing.Size(75, 23);
            this.settingsBtn.TabIndex = 1;
            this.settingsBtn.Text = "Settings";
            this.settingsBtn.UseVisualStyleBackColor = true;
            this.settingsBtn.Click += new System.EventHandler(this.settingsBtn_Click);
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(584, 24);
            this.menuStrip.TabIndex = 3;
            this.menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openScenarioToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openScenarioToolStripMenuItem
            // 
            this.openScenarioToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.euroscopeToolStripMenuItem});
            this.openScenarioToolStripMenuItem.Name = "openScenarioToolStripMenuItem";
            this.openScenarioToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.openScenarioToolStripMenuItem.Text = "Open Scenario";
            // 
            // euroscopeToolStripMenuItem
            // 
            this.euroscopeToolStripMenuItem.Name = "euroscopeToolStripMenuItem";
            this.euroscopeToolStripMenuItem.Size = new System.Drawing.Size(129, 22);
            this.euroscopeToolStripMenuItem.Text = "Euroscope";
            this.euroscopeToolStripMenuItem.Click += new System.EventHandler(this.euroscopeToolStripMenuItem_Click);
            // 
            // mainSplitContainer
            // 
            this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainSplitContainer.Location = new System.Drawing.Point(0, 57);
            this.mainSplitContainer.Name = "mainSplitContainer";
            // 
            // mainSplitContainer.Panel1
            // 
            this.mainSplitContainer.Panel1.Controls.Add(this.connectionsList);
            // 
            // mainSplitContainer.Panel2
            // 
            this.mainSplitContainer.Panel2.Controls.Add(this.msgBox);
            this.mainSplitContainer.Size = new System.Drawing.Size(584, 304);
            this.mainSplitContainer.SplitterDistance = 194;
            this.mainSplitContainer.TabIndex = 1;
            // 
            // connectionsList
            // 
            this.connectionsList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.connectionsList.Location = new System.Drawing.Point(0, 0);
            this.connectionsList.Name = "connectionsList";
            this.connectionsList.Size = new System.Drawing.Size(194, 304);
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
            this.msgBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.msgBox.Size = new System.Drawing.Size(386, 304);
            this.msgBox.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.mainSplitContainer);
            this.Controls.Add(this.headerPnl);
            this.MainMenuStrip = this.menuStrip;
            this.MinimumSize = new System.Drawing.Size(600, 200);
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "VATSIM ATC Training Simulator";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.headerPnl.ResumeLayout(false);
            this.headerPnl.PerformLayout();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.mainSplitContainer.Panel1.ResumeLayout(false);
            this.mainSplitContainer.Panel2.ResumeLayout(false);
            this.mainSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).EndInit();
            this.mainSplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Panel headerPnl;
        private Button settingsBtn;
        private SplitContainer mainSplitContainer;
        private ListView connectionsList;
        private TextBox msgBox;
        private Button pauseAllBtn;
        private MenuStrip menuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openScenarioToolStripMenuItem;
        private ToolStripMenuItem euroscopeToolStripMenuItem;
    }
}

