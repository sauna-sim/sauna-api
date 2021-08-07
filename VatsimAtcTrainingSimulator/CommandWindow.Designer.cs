namespace VatsimAtcTrainingSimulator
{
    partial class CommandWindow
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
            this.outputWindow = new System.Windows.Forms.TextBox();
            this.commandInputBx = new System.Windows.Forms.TextBox();
            this.sendBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // outputWindow
            // 
            this.outputWindow.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.outputWindow.Location = new System.Drawing.Point(12, 12);
            this.outputWindow.Multiline = true;
            this.outputWindow.Name = "outputWindow";
            this.outputWindow.ReadOnly = true;
            this.outputWindow.Size = new System.Drawing.Size(360, 411);
            this.outputWindow.TabIndex = 0;
            this.outputWindow.TabStop = false;
            // 
            // commandInputBx
            // 
            this.commandInputBx.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.commandInputBx.Location = new System.Drawing.Point(12, 429);
            this.commandInputBx.Name = "commandInputBx";
            this.commandInputBx.Size = new System.Drawing.Size(303, 20);
            this.commandInputBx.TabIndex = 0;
            this.commandInputBx.KeyDown += new System.Windows.Forms.KeyEventHandler(this.commandInputBx_KeyDown);
            // 
            // sendBtn
            // 
            this.sendBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.sendBtn.Location = new System.Drawing.Point(321, 429);
            this.sendBtn.Name = "sendBtn";
            this.sendBtn.Size = new System.Drawing.Size(51, 23);
            this.sendBtn.TabIndex = 1;
            this.sendBtn.Text = "Send";
            this.sendBtn.UseVisualStyleBackColor = true;
            this.sendBtn.Click += new System.EventHandler(this.sendBtn_Click);
            // 
            // CommandWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 461);
            this.Controls.Add(this.sendBtn);
            this.Controls.Add(this.commandInputBx);
            this.Controls.Add(this.outputWindow);
            this.MinimumSize = new System.Drawing.Size(300, 300);
            this.Name = "CommandWindow";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Command Window";
            this.LocationChanged += new System.EventHandler(this.CommandWindow_LocationChanged);
            this.SizeChanged += new System.EventHandler(this.CommandWindow_SizeChanged);
            this.Move += new System.EventHandler(this.CommandWindow_Move);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox outputWindow;
        private System.Windows.Forms.TextBox commandInputBx;
        private System.Windows.Forms.Button sendBtn;
    }
}