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
            this.serverTxtBxLbl = new System.Windows.Forms.Label();
            this.cidTxtBxLbl = new System.Windows.Forms.Label();
            this.passTxtBxLbl = new System.Windows.Forms.Label();
            this.serverTxtBx = new System.Windows.Forms.TextBox();
            this.passTxtBx = new System.Windows.Forms.TextBox();
            this.cidTxtBx = new System.Windows.Forms.TextBox();
            this.vatsimServerChxBx = new System.Windows.Forms.CheckBox();
            this.portTxtBxLbl = new System.Windows.Forms.Label();
            this.portNumBx = new System.Windows.Forms.NumericUpDown();
            this.ntwkSetGrpBx.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.portNumBx)).BeginInit();
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
            this.ntwkSetGrpBx.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
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
            this.ntwkSetGrpBx.Size = new System.Drawing.Size(260, 208);
            this.ntwkSetGrpBx.TabIndex = 10;
            this.ntwkSetGrpBx.TabStop = false;
            this.ntwkSetGrpBx.Text = "Network Settings";
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
            // cidTxtBxLbl
            // 
            this.cidTxtBxLbl.AutoSize = true;
            this.cidTxtBxLbl.Location = new System.Drawing.Point(6, 45);
            this.cidTxtBxLbl.Name = "cidTxtBxLbl";
            this.cidTxtBxLbl.Size = new System.Drawing.Size(25, 13);
            this.cidTxtBxLbl.TabIndex = 8;
            this.cidTxtBxLbl.Text = "CID";
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
            // serverTxtBx
            // 
            this.serverTxtBx.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.serverTxtBx.Location = new System.Drawing.Point(65, 16);
            this.serverTxtBx.Name = "serverTxtBx";
            this.serverTxtBx.Size = new System.Drawing.Size(111, 20);
            this.serverTxtBx.TabIndex = 0;
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
            // cidTxtBx
            // 
            this.cidTxtBx.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cidTxtBx.Location = new System.Drawing.Point(65, 42);
            this.cidTxtBx.Name = "cidTxtBx";
            this.cidTxtBx.Size = new System.Drawing.Size(189, 20);
            this.cidTxtBx.TabIndex = 2;
            // 
            // vatsimServerChxBx
            // 
            this.vatsimServerChxBx.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.vatsimServerChxBx.AutoSize = true;
            this.vatsimServerChxBx.Location = new System.Drawing.Point(148, 94);
            this.vatsimServerChxBx.Name = "vatsimServerChxBx";
            this.vatsimServerChxBx.Size = new System.Drawing.Size(106, 17);
            this.vatsimServerChxBx.TabIndex = 4;
            this.vatsimServerChxBx.Text = "VATSIM Server?";
            this.vatsimServerChxBx.UseVisualStyleBackColor = true;
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
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.ControlBox = false;
            this.Controls.Add(this.ntwkSetGrpBx);
            this.Controls.Add(this.closeBtn);
            this.MinimumSize = new System.Drawing.Size(300, 300);
            this.Name = "SettingsForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Settings";
            this.ntwkSetGrpBx.ResumeLayout(false);
            this.ntwkSetGrpBx.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.portNumBx)).EndInit();
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
    }
}