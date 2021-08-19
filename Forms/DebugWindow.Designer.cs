namespace VatsimAtcTrainingSimulator
{
    partial class DebugWindow
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
            this.debugConsoleBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // debugConsoleBox
            // 
            this.debugConsoleBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.debugConsoleBox.Location = new System.Drawing.Point(0, 0);
            this.debugConsoleBox.Multiline = true;
            this.debugConsoleBox.Name = "debugConsoleBox";
            this.debugConsoleBox.ReadOnly = true;
            this.debugConsoleBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.debugConsoleBox.Size = new System.Drawing.Size(510, 359);
            this.debugConsoleBox.TabIndex = 0;
            // 
            // DebugWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(510, 359);
            this.Controls.Add(this.debugConsoleBox);
            this.Name = "DebugWindow";
            this.ShowIcon = false;
            this.Text = "Debug Console";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DebugWindow_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox debugConsoleBox;
    }
}