using System.Windows.Forms;

namespace GSharp.Samples
{
    partial class WorkerUI
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.trackbarSpeed = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.lblCycleCountLabel = new System.Windows.Forms.Label();
            this.lblCycleCount = new System.Windows.Forms.Label();
            this.verticalProgressBar3 = new GSharp.Samples.VerticalProgressBar();
            this.verticalProgressBar2 = new GSharp.Samples.VerticalProgressBar();
            this.verticalProgressBar1 = new GSharp.Samples.VerticalProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.trackbarSpeed)).BeginInit();
            this.SuspendLayout();
            // 
            // trackbarSpeed
            // 
            this.trackbarSpeed.Location = new System.Drawing.Point(18, 23);
            this.trackbarSpeed.Maximum = 20;
            this.trackbarSpeed.Name = "trackbarSpeed";
            this.trackbarSpeed.Size = new System.Drawing.Size(104, 45);
            this.trackbarSpeed.TabIndex = 1;
            this.toolTip1.SetToolTip(this.trackbarSpeed, "Changes the speed of the workerthread");
            this.trackbarSpeed.ValueChanged += new System.EventHandler(this.trackbarSpeed_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Change Work Speed";
            // 
            // lblCycleCountLabel
            // 
            this.lblCycleCountLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCycleCountLabel.AutoSize = true;
            this.lblCycleCountLabel.Location = new System.Drawing.Point(26, 156);
            this.lblCycleCountLabel.Name = "lblCycleCountLabel";
            this.lblCycleCountLabel.Size = new System.Drawing.Size(67, 13);
            this.lblCycleCountLabel.TabIndex = 6;
            this.lblCycleCountLabel.Text = "Cycle Count:";
            // 
            // lblCycleCount
            // 
            this.lblCycleCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCycleCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCycleCount.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblCycleCount.Location = new System.Drawing.Point(91, 152);
            this.lblCycleCount.Name = "lblCycleCount";
            this.lblCycleCount.Size = new System.Drawing.Size(36, 24);
            this.lblCycleCount.TabIndex = 7;
            this.lblCycleCount.Text = "0";
            this.lblCycleCount.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // verticalProgressBar3
            // 
            this.verticalProgressBar3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.verticalProgressBar3.BorderColor = System.Drawing.Color.Silver;
            this.verticalProgressBar3.BorderWidth = 1;
            this.verticalProgressBar3.FillColor = System.Drawing.Color.CornflowerBlue;
            this.verticalProgressBar3.Location = new System.Drawing.Point(99, 67);
            this.verticalProgressBar3.Name = "verticalProgressBar3";
            this.verticalProgressBar3.ProgressInPercentage = 40;
            this.verticalProgressBar3.Size = new System.Drawing.Size(23, 75);
            this.verticalProgressBar3.TabIndex = 5;
            this.verticalProgressBar3.Text = "verticalProgressBar3";
            // 
            // verticalProgressBar2
            // 
            this.verticalProgressBar2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.verticalProgressBar2.BorderColor = System.Drawing.Color.Silver;
            this.verticalProgressBar2.BorderWidth = 1;
            this.verticalProgressBar2.FillColor = System.Drawing.Color.CornflowerBlue;
            this.verticalProgressBar2.Location = new System.Drawing.Point(59, 67);
            this.verticalProgressBar2.Name = "verticalProgressBar2";
            this.verticalProgressBar2.ProgressInPercentage = 40;
            this.verticalProgressBar2.Size = new System.Drawing.Size(23, 75);
            this.verticalProgressBar2.TabIndex = 4;
            this.verticalProgressBar2.Text = "verticalProgressBar2";
            // 
            // verticalProgressBar1
            // 
            this.verticalProgressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.verticalProgressBar1.BorderColor = System.Drawing.Color.Silver;
            this.verticalProgressBar1.BorderWidth = 1;
            this.verticalProgressBar1.FillColor = System.Drawing.Color.CornflowerBlue;
            this.verticalProgressBar1.Location = new System.Drawing.Point(18, 67);
            this.verticalProgressBar1.Name = "verticalProgressBar1";
            this.verticalProgressBar1.ProgressInPercentage = 40;
            this.verticalProgressBar1.Size = new System.Drawing.Size(23, 75);
            this.verticalProgressBar1.TabIndex = 2;
            this.verticalProgressBar1.Text = "verticalProgressBar1";
            // 
            // WorkerUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblCycleCount);
            this.Controls.Add(this.lblCycleCountLabel);
            this.Controls.Add(this.verticalProgressBar3);
            this.Controls.Add(this.verticalProgressBar2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.verticalProgressBar1);
            this.Controls.Add(this.trackbarSpeed);
            this.DoubleBuffered = true;
            this.Location = new System.Drawing.Point(3, 3);
            this.Name = "WorkerUI";
            this.Size = new System.Drawing.Size(140, 183);
            this.Resize += new System.EventHandler(this.WorkerUI_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.trackbarSpeed)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TrackBar trackbarSpeed;
        private System.Windows.Forms.Label label1;
        private VerticalProgressBar verticalProgressBar1;
        private System.Windows.Forms.ToolTip toolTip1;
        private VerticalProgressBar verticalProgressBar2;
        private VerticalProgressBar verticalProgressBar3;
        private Label lblCycleCountLabel;
        private Label lblCycleCount;
    }
}
