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
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.commandWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadSectorFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.debugConsoleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clientsDataGridView = new System.Windows.Forms.DataGridView();
            this.headerPnl.SuspendLayout();
            this.menuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.clientsDataGridView)).BeginInit();
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
            this.pauseAllBtn.Location = new System.Drawing.Point(12, 27);
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
            this.settingsBtn.Location = new System.Drawing.Point(93, 27);
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
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.dataToolStripMenuItem});
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
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.commandWindowMenuItem,
            this.debugConsoleToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // commandWindowMenuItem
            // 
            this.commandWindowMenuItem.Checked = true;
            this.commandWindowMenuItem.CheckOnClick = true;
            this.commandWindowMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.commandWindowMenuItem.Name = "commandWindowMenuItem";
            this.commandWindowMenuItem.Size = new System.Drawing.Size(178, 22);
            this.commandWindowMenuItem.Text = "Command Window";
            this.commandWindowMenuItem.CheckedChanged += new System.EventHandler(this.commandWindowMenuItem_CheckedChanged);
            // 
            // dataToolStripMenuItem
            // 
            this.dataToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadSectorFileToolStripMenuItem});
            this.dataToolStripMenuItem.Name = "dataToolStripMenuItem";
            this.dataToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.dataToolStripMenuItem.Text = "Data";
            // 
            // loadSectorFileToolStripMenuItem
            // 
            this.loadSectorFileToolStripMenuItem.Name = "loadSectorFileToolStripMenuItem";
            this.loadSectorFileToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.loadSectorFileToolStripMenuItem.Text = "Load From Sector File";
            this.loadSectorFileToolStripMenuItem.Click += new System.EventHandler(this.loadSectorFileToolStripMenuItem_Click);
            // 
            // debugConsoleToolStripMenuItem
            // 
            this.debugConsoleToolStripMenuItem.Checked = true;
            this.debugConsoleToolStripMenuItem.CheckOnClick = true;
            this.debugConsoleToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.debugConsoleToolStripMenuItem.Name = "debugConsoleToolStripMenuItem";
            this.debugConsoleToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.debugConsoleToolStripMenuItem.Text = "Debug Console";
            this.debugConsoleToolStripMenuItem.CheckedChanged += new System.EventHandler(this.debugConsoleToolStripMenuItem_CheckedChanged);
            // 
            // clientsDataGridView
            // 
            this.clientsDataGridView.AllowUserToAddRows = false;
            this.clientsDataGridView.AllowUserToDeleteRows = false;
            this.clientsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.clientsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clientsDataGridView.Location = new System.Drawing.Point(0, 57);
            this.clientsDataGridView.Name = "clientsDataGridView";
            this.clientsDataGridView.ReadOnly = true;
            this.clientsDataGridView.Size = new System.Drawing.Size(584, 304);
            this.clientsDataGridView.TabIndex = 1;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.clientsDataGridView);
            this.Controls.Add(this.headerPnl);
            this.MainMenuStrip = this.menuStrip;
            this.MinimumSize = new System.Drawing.Size(600, 300);
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "VATSIM ATC Training Simulator";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.LocationChanged += new System.EventHandler(this.MainForm_LocationChanged);
            this.SizeChanged += new System.EventHandler(this.MainForm_SizeChanged);
            this.headerPnl.ResumeLayout(false);
            this.headerPnl.PerformLayout();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.clientsDataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Panel headerPnl;
        private Button settingsBtn;
        private Button pauseAllBtn;
        private MenuStrip menuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openScenarioToolStripMenuItem;
        private ToolStripMenuItem euroscopeToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem commandWindowMenuItem;
        private ToolStripMenuItem dataToolStripMenuItem;
        private ToolStripMenuItem loadSectorFileToolStripMenuItem;
        private ToolStripMenuItem debugConsoleToolStripMenuItem;
        private DataGridView clientsDataGridView;
    }
}

