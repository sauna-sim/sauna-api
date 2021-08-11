namespace VatsimAtcTrainingSimulator
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.closeBtn = new System.Windows.Forms.Button();
            this.ntwkSetGrpBx = new System.Windows.Forms.GroupBox();
            this.posSendIntblBxLbl = new System.Windows.Forms.Label();
            this.posSendIntvlBx = new System.Windows.Forms.NumericUpDown();
            this.protocolVersionInputBx = new System.Windows.Forms.NumericUpDown();
            this.protocolVerInputBxLbl = new System.Windows.Forms.Label();
            this.portNumBx = new System.Windows.Forms.NumericUpDown();
            this.portTxtBxLbl = new System.Windows.Forms.Label();
            this.vatsimServerChxBx = new System.Windows.Forms.CheckBox();
            this.cidTxtBx = new System.Windows.Forms.TextBox();
            this.passTxtBx = new System.Windows.Forms.TextBox();
            this.serverTxtBx = new System.Windows.Forms.TextBox();
            this.passTxtBxLbl = new System.Windows.Forms.Label();
            this.cidTxtBxLbl = new System.Windows.Forms.Label();
            this.serverTxtBxLbl = new System.Windows.Forms.Label();
            this.cmdSettingsGrpBx = new System.Windows.Forms.GroupBox();
            this.cmdFreqLbl = new System.Windows.Forms.Label();
            this.cmdFreqInput = new System.Windows.Forms.NumericUpDown();
            this.sendIasChkBx = new System.Windows.Forms.CheckBox();
            this.ntwkSetGrpBx.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.posSendIntvlBx)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.protocolVersionInputBx)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.portNumBx)).BeginInit();
            this.cmdSettingsGrpBx.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cmdFreqInput)).BeginInit();
            this.SuspendLayout();
            // 
            // closeBtn
            // 
            this.closeBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeBtn.Location = new System.Drawing.Point(197, 226);
            this.closeBtn.Name = "closeBtn";
            this.closeBtn.Size = new System.Drawing.Size(75, 23);
            this.closeBtn.TabIndex = 5;
            this.closeBtn.Text = "Close";
            this.closeBtn.UseVisualStyleBackColor = true;
            this.closeBtn.Click += new System.EventHandler(this.closeBtn_Click);
            // 
            // ntwkSetGrpBx
            // 
            this.ntwkSetGrpBx.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ntwkSetGrpBx.Controls.Add(this.sendIasChkBx);
            this.ntwkSetGrpBx.Controls.Add(this.posSendIntblBxLbl);
            this.ntwkSetGrpBx.Controls.Add(this.posSendIntvlBx);
            this.ntwkSetGrpBx.Controls.Add(this.protocolVersionInputBx);
            this.ntwkSetGrpBx.Controls.Add(this.protocolVerInputBxLbl);
            this.ntwkSetGrpBx.Controls.Add(this.portNumBx);
            this.ntwkSetGrpBx.Controls.Add(this.portTxtBxLbl);
            this.ntwkSetGrpBx.Controls.Add(this.vatsimServerChxBx);
            this.ntwkSetGrpBx.Controls.Add(this.cidTxtBx);
            this.ntwkSetGrpBx.Controls.Add(this.passTxtBx);
            this.ntwkSetGrpBx.Controls.Add(this.serverTxtBx);
            this.ntwkSetGrpBx.Controls.Add(this.passTxtBxLbl);
            this.ntwkSetGrpBx.Controls.Add(this.cidTxtBxLbl);
            this.ntwkSetGrpBx.Controls.Add(this.serverTxtBxLbl);
            this.ntwkSetGrpBx.Location = new System.Drawing.Point(12, 12);
            this.ntwkSetGrpBx.Name = "ntwkSetGrpBx";
            this.ntwkSetGrpBx.Size = new System.Drawing.Size(260, 142);
            this.ntwkSetGrpBx.TabIndex = 10;
            this.ntwkSetGrpBx.TabStop = false;
            this.ntwkSetGrpBx.Text = "Network Settings";
            // 
            // posSendIntblBxLbl
            // 
            this.posSendIntblBxLbl.AutoSize = true;
            this.posSendIntblBxLbl.Location = new System.Drawing.Point(124, 96);
            this.posSendIntblBxLbl.Name = "posSendIntblBxLbl";
            this.posSendIntblBxLbl.Size = new System.Drawing.Size(68, 13);
            this.posSendIntblBxLbl.TabIndex = 12;
            this.posSendIntblBxLbl.Text = "Update Rate";
            // 
            // posSendIntvlBx
            // 
            this.posSendIntvlBx.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.posSendIntvlBx.Location = new System.Drawing.Point(196, 94);
            this.posSendIntvlBx.Maximum = new decimal(new int[] {
            20000,
            0,
            0,
            0});
            this.posSendIntvlBx.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.posSendIntvlBx.Name = "posSendIntvlBx";
            this.posSendIntvlBx.Size = new System.Drawing.Size(58, 20);
            this.posSendIntvlBx.TabIndex = 11;
            this.posSendIntvlBx.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // protocolVersionInputBx
            // 
            this.protocolVersionInputBx.Location = new System.Drawing.Point(65, 94);
            this.protocolVersionInputBx.Name = "protocolVersionInputBx";
            this.protocolVersionInputBx.Size = new System.Drawing.Size(53, 20);
            this.protocolVersionInputBx.TabIndex = 10;
            // 
            // protocolVerInputBxLbl
            // 
            this.protocolVerInputBxLbl.AutoSize = true;
            this.protocolVerInputBxLbl.Location = new System.Drawing.Point(6, 96);
            this.protocolVerInputBxLbl.Name = "protocolVerInputBxLbl";
            this.protocolVerInputBxLbl.Size = new System.Drawing.Size(46, 13);
            this.protocolVerInputBxLbl.TabIndex = 0;
            this.protocolVerInputBxLbl.Text = "Protocol";
            // 
            // portNumBx
            // 
            this.portNumBx.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.portNumBx.Location = new System.Drawing.Point(196, 16);
            this.portNumBx.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.portNumBx.Name = "portNumBx";
            this.portNumBx.Size = new System.Drawing.Size(58, 20);
            this.portNumBx.TabIndex = 1;
            // 
            // portTxtBxLbl
            // 
            this.portTxtBxLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.portTxtBxLbl.AutoSize = true;
            this.portTxtBxLbl.Location = new System.Drawing.Point(182, 19);
            this.portTxtBxLbl.Name = "portTxtBxLbl";
            this.portTxtBxLbl.Size = new System.Drawing.Size(10, 13);
            this.portTxtBxLbl.TabIndex = 6;
            this.portTxtBxLbl.Text = ":";
            // 
            // vatsimServerChxBx
            // 
            this.vatsimServerChxBx.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.vatsimServerChxBx.AutoSize = true;
            this.vatsimServerChxBx.Location = new System.Drawing.Point(148, 120);
            this.vatsimServerChxBx.Name = "vatsimServerChxBx";
            this.vatsimServerChxBx.Size = new System.Drawing.Size(106, 17);
            this.vatsimServerChxBx.TabIndex = 4;
            this.vatsimServerChxBx.Text = "VATSIM Server?";
            this.vatsimServerChxBx.UseVisualStyleBackColor = true;
            // 
            // cidTxtBx
            // 
            this.cidTxtBx.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cidTxtBx.Location = new System.Drawing.Point(65, 42);
            this.cidTxtBx.Name = "cidTxtBx";
            this.cidTxtBx.Size = new System.Drawing.Size(189, 20);
            this.cidTxtBx.TabIndex = 2;
            // 
            // passTxtBx
            // 
            this.passTxtBx.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.passTxtBx.Location = new System.Drawing.Point(65, 68);
            this.passTxtBx.Name = "passTxtBx";
            this.passTxtBx.Size = new System.Drawing.Size(189, 20);
            this.passTxtBx.TabIndex = 3;
            // 
            // serverTxtBx
            // 
            this.serverTxtBx.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.serverTxtBx.Location = new System.Drawing.Point(65, 16);
            this.serverTxtBx.Name = "serverTxtBx";
            this.serverTxtBx.Size = new System.Drawing.Size(111, 20);
            this.serverTxtBx.TabIndex = 0;
            // 
            // passTxtBxLbl
            // 
            this.passTxtBxLbl.AutoSize = true;
            this.passTxtBxLbl.Location = new System.Drawing.Point(6, 71);
            this.passTxtBxLbl.Name = "passTxtBxLbl";
            this.passTxtBxLbl.Size = new System.Drawing.Size(53, 13);
            this.passTxtBxLbl.TabIndex = 9;
            this.passTxtBxLbl.Text = "Password";
            // 
            // cidTxtBxLbl
            // 
            this.cidTxtBxLbl.AutoSize = true;
            this.cidTxtBxLbl.Location = new System.Drawing.Point(6, 45);
            this.cidTxtBxLbl.Name = "cidTxtBxLbl";
            this.cidTxtBxLbl.Size = new System.Drawing.Size(25, 13);
            this.cidTxtBxLbl.TabIndex = 8;
            this.cidTxtBxLbl.Text = "CID";
            // 
            // serverTxtBxLbl
            // 
            this.serverTxtBxLbl.AutoSize = true;
            this.serverTxtBxLbl.Location = new System.Drawing.Point(6, 19);
            this.serverTxtBxLbl.Name = "serverTxtBxLbl";
            this.serverTxtBxLbl.Size = new System.Drawing.Size(38, 13);
            this.serverTxtBxLbl.TabIndex = 7;
            this.serverTxtBxLbl.Text = "Server";
            // 
            // cmdSettingsGrpBx
            // 
            this.cmdSettingsGrpBx.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdSettingsGrpBx.Controls.Add(this.cmdFreqLbl);
            this.cmdSettingsGrpBx.Controls.Add(this.cmdFreqInput);
            this.cmdSettingsGrpBx.Location = new System.Drawing.Point(13, 160);
            this.cmdSettingsGrpBx.Name = "cmdSettingsGrpBx";
            this.cmdSettingsGrpBx.Size = new System.Drawing.Size(259, 51);
            this.cmdSettingsGrpBx.TabIndex = 11;
            this.cmdSettingsGrpBx.TabStop = false;
            this.cmdSettingsGrpBx.Text = "Command Input Settings";
            // 
            // cmdFreqLbl
            // 
            this.cmdFreqLbl.AutoSize = true;
            this.cmdFreqLbl.Location = new System.Drawing.Point(6, 21);
            this.cmdFreqLbl.Name = "cmdFreqLbl";
            this.cmdFreqLbl.Size = new System.Drawing.Size(107, 13);
            this.cmdFreqLbl.TabIndex = 1;
            this.cmdFreqLbl.Text = "Command Frequency";
            // 
            // cmdFreqInput
            // 
            this.cmdFreqInput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdFreqInput.DecimalPlaces = 3;
            this.cmdFreqInput.Location = new System.Drawing.Point(119, 19);
            this.cmdFreqInput.Maximum = new decimal(new int[] {
            199998,
            0,
            0,
            196608});
            this.cmdFreqInput.Minimum = new decimal(new int[] {
            118000,
            0,
            0,
            196608});
            this.cmdFreqInput.Name = "cmdFreqInput";
            this.cmdFreqInput.Size = new System.Drawing.Size(127, 20);
            this.cmdFreqInput.TabIndex = 0;
            this.cmdFreqInput.Value = new decimal(new int[] {
            118000,
            0,
            0,
            196608});
            // 
            // sendIasChkBx
            // 
            this.sendIasChkBx.AutoSize = true;
            this.sendIasChkBx.Location = new System.Drawing.Point(65, 119);
            this.sendIasChkBx.Name = "sendIasChkBx";
            this.sendIasChkBx.Size = new System.Drawing.Size(77, 17);
            this.sendIasChkBx.TabIndex = 13;
            this.sendIasChkBx.Text = "Send IAS?";
            this.sendIasChkBx.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 284);
            this.ControlBox = false;
            this.Controls.Add(this.cmdSettingsGrpBx);
            this.Controls.Add(this.ntwkSetGrpBx);
            this.Controls.Add(this.closeBtn);
            this.MinimumSize = new System.Drawing.Size(300, 300);
            this.Name = "SettingsForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Settings";
            this.ntwkSetGrpBx.ResumeLayout(false);
            this.ntwkSetGrpBx.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.posSendIntvlBx)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.protocolVersionInputBx)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.portNumBx)).EndInit();
            this.cmdSettingsGrpBx.ResumeLayout(false);
            this.cmdSettingsGrpBx.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cmdFreqInput)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button closeBtn;
        private System.Windows.Forms.GroupBox ntwkSetGrpBx;
        private System.Windows.Forms.TextBox cidTxtBx;
        private System.Windows.Forms.TextBox passTxtBx;
        private System.Windows.Forms.TextBox serverTxtBx;
        private System.Windows.Forms.Label passTxtBxLbl;
        private System.Windows.Forms.Label cidTxtBxLbl;
        private System.Windows.Forms.Label serverTxtBxLbl;
        private System.Windows.Forms.CheckBox vatsimServerChxBx;
        private System.Windows.Forms.NumericUpDown portNumBx;
        private System.Windows.Forms.Label portTxtBxLbl;
        private System.Windows.Forms.Label posSendIntblBxLbl;
        private System.Windows.Forms.NumericUpDown posSendIntvlBx;
        private System.Windows.Forms.NumericUpDown protocolVersionInputBx;
        private System.Windows.Forms.Label protocolVerInputBxLbl;
        private System.Windows.Forms.GroupBox cmdSettingsGrpBx;
        private System.Windows.Forms.Label cmdFreqLbl;
        private System.Windows.Forms.NumericUpDown cmdFreqInput;
        private System.Windows.Forms.CheckBox sendIasChkBx;
    }
}