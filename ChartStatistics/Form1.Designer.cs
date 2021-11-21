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
        private void InitializeComponent() {
            this.chartViewPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // chartViewPanel
            // 
            this.chartViewPanel.AutoSize = true;
            this.chartViewPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.chartViewPanel.BackColor = System.Drawing.SystemColors.Desktop;
            this.chartViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chartViewPanel.Location = new System.Drawing.Point(0, 0);
            this.chartViewPanel.Name = "chartViewPanel";
            this.chartViewPanel.Size = new System.Drawing.Size(1994, 471);
            this.chartViewPanel.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1994, 471);
            this.Controls.Add(this.chartViewPanel);
            this.Name = "Form1";
            this.Text = "ChartStatistics";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Panel chartViewPanel;

        #endregion
    }
}