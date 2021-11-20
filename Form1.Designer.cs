namespace ChartStatistics
{
    partial class Form1
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
            this.chartViewPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // chartViewPanel
            // 
            this.chartViewPanel.BackColor = System.Drawing.SystemColors.Desktop;
            this.chartViewPanel.Location = new System.Drawing.Point(0, 0);
            this.chartViewPanel.Name = "chartViewPanel";
            this.chartViewPanel.Size = new System.Drawing.Size(1992, 470);
            this.chartViewPanel.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1994, 471);
            this.Controls.Add(this.chartViewPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel chartViewPanel;

        #endregion
    }
}