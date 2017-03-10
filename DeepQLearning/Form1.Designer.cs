namespace DeepQLearning
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea7 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend7 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series7 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.startLearning = new System.Windows.Forms.Button();
            this.displayBox = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.goSlow = new System.Windows.Forms.Button();
            this.goNormal = new System.Windows.Forms.Button();
            this.goFast = new System.Windows.Forms.Button();
            this.goVeryFast = new System.Windows.Forms.Button();
            this.StopLearning = new System.Windows.Forms.Button();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.canvas = new System.Windows.Forms.Panel();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.PauseBtn = new System.Windows.Forms.Button();
            this.saveNet = new System.Windows.Forms.Button();
            this.loadNet = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // startLearning
            // 
            this.startLearning.Location = new System.Drawing.Point(8, 21);
            this.startLearning.Name = "startLearning";
            this.startLearning.Size = new System.Drawing.Size(134, 27);
            this.startLearning.TabIndex = 0;
            this.startLearning.Text = "Start Learning";
            this.startLearning.UseVisualStyleBackColor = true;
            this.startLearning.Click += new System.EventHandler(this.startLearning_Click);
            // 
            // displayBox
            // 
            this.displayBox.Location = new System.Drawing.Point(8, 305);
            this.displayBox.Multiline = true;
            this.displayBox.Name = "displayBox";
            this.displayBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.displayBox.Size = new System.Drawing.Size(360, 204);
            this.displayBox.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.loadNet);
            this.groupBox1.Controls.Add(this.saveNet);
            this.groupBox1.Controls.Add(this.PauseBtn);
            this.groupBox1.Controls.Add(this.goSlow);
            this.groupBox1.Controls.Add(this.goNormal);
            this.groupBox1.Controls.Add(this.goFast);
            this.groupBox1.Controls.Add(this.goVeryFast);
            this.groupBox1.Controls.Add(this.StopLearning);
            this.groupBox1.Controls.Add(this.startLearning);
            this.groupBox1.Location = new System.Drawing.Point(976, 534);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(374, 127);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Controls";
            // 
            // goSlow
            // 
            this.goSlow.Location = new System.Drawing.Point(282, 54);
            this.goSlow.Name = "goSlow";
            this.goSlow.Size = new System.Drawing.Size(86, 27);
            this.goSlow.TabIndex = 5;
            this.goSlow.Text = "Go slow";
            this.goSlow.UseVisualStyleBackColor = true;
            this.goSlow.Click += new System.EventHandler(this.goSlow_Click);
            // 
            // goNormal
            // 
            this.goNormal.Location = new System.Drawing.Point(188, 54);
            this.goNormal.Name = "goNormal";
            this.goNormal.Size = new System.Drawing.Size(88, 27);
            this.goNormal.TabIndex = 4;
            this.goNormal.Text = "Go normal";
            this.goNormal.UseVisualStyleBackColor = true;
            this.goNormal.Click += new System.EventHandler(this.goNormal_Click);
            // 
            // goFast
            // 
            this.goFast.Location = new System.Drawing.Point(112, 54);
            this.goFast.Name = "goFast";
            this.goFast.Size = new System.Drawing.Size(70, 27);
            this.goFast.TabIndex = 3;
            this.goFast.Text = "Go fast";
            this.goFast.UseVisualStyleBackColor = true;
            this.goFast.Click += new System.EventHandler(this.goFast_Click);
            // 
            // goVeryFast
            // 
            this.goVeryFast.Location = new System.Drawing.Point(8, 54);
            this.goVeryFast.Name = "goVeryFast";
            this.goVeryFast.Size = new System.Drawing.Size(98, 27);
            this.goVeryFast.TabIndex = 2;
            this.goVeryFast.Text = "Go very fast";
            this.goVeryFast.UseVisualStyleBackColor = true;
            this.goVeryFast.Click += new System.EventHandler(this.goVeryFast_Click);
            // 
            // StopLearning
            // 
            this.StopLearning.Location = new System.Drawing.Point(235, 21);
            this.StopLearning.Name = "StopLearning";
            this.StopLearning.Size = new System.Drawing.Size(133, 27);
            this.StopLearning.TabIndex = 1;
            this.StopLearning.Text = "Stop Learning";
            this.StopLearning.UseVisualStyleBackColor = true;
            this.StopLearning.Click += new System.EventHandler(this.StopLearning_Click);
            // 
            // chart1
            // 
            chartArea7.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea7);
            legend7.Name = "Legend1";
            this.chart1.Legends.Add(legend7);
            this.chart1.Location = new System.Drawing.Point(8, 21);
            this.chart1.Name = "chart1";
            series7.ChartArea = "ChartArea1";
            series7.Legend = "Legend1";
            series7.Name = "Series1";
            this.chart1.Series.Add(series7);
            this.chart1.Size = new System.Drawing.Size(360, 278);
            this.chart1.TabIndex = 4;
            this.chart1.Text = "chart1";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.canvas);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(958, 649);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Visualization";
            // 
            // canvas
            // 
            this.canvas.BackColor = System.Drawing.SystemColors.Info;
            this.canvas.Location = new System.Drawing.Point(6, 21);
            this.canvas.Name = "canvas";
            this.canvas.Size = new System.Drawing.Size(946, 618);
            this.canvas.TabIndex = 0;
            this.canvas.Paint += new System.Windows.Forms.PaintEventHandler(this.canvas_Paint);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.chart1);
            this.groupBox3.Controls.Add(this.displayBox);
            this.groupBox3.Location = new System.Drawing.Point(976, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(374, 516);
            this.groupBox3.TabIndex = 0;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Output";
            // 
            // PauseBtn
            // 
            this.PauseBtn.Location = new System.Drawing.Point(148, 21);
            this.PauseBtn.Name = "PauseBtn";
            this.PauseBtn.Size = new System.Drawing.Size(81, 27);
            this.PauseBtn.TabIndex = 6;
            this.PauseBtn.Text = "Pause";
            this.PauseBtn.UseVisualStyleBackColor = true;
            this.PauseBtn.Click += new System.EventHandler(this.PauseBtn_Click);
            // 
            // saveNet
            // 
            this.saveNet.Location = new System.Drawing.Point(8, 88);
            this.saveNet.Name = "saveNet";
            this.saveNet.Size = new System.Drawing.Size(174, 29);
            this.saveNet.TabIndex = 7;
            this.saveNet.Text = "Save QNetwork";
            this.saveNet.UseVisualStyleBackColor = true;
            this.saveNet.Click += new System.EventHandler(this.saveNet_Click);
            // 
            // loadNet
            // 
            this.loadNet.Location = new System.Drawing.Point(188, 88);
            this.loadNet.Name = "loadNet";
            this.loadNet.Size = new System.Drawing.Size(180, 29);
            this.loadNet.TabIndex = 8;
            this.loadNet.Text = "Load QNetwork";
            this.loadNet.UseVisualStyleBackColor = true;
            this.loadNet.Click += new System.EventHandler(this.loadNet_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1362, 673);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Deep Q Learning Demo";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button startLearning;
        private System.Windows.Forms.TextBox displayBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button StopLearning;
        private System.Windows.Forms.Button goSlow;
        private System.Windows.Forms.Button goNormal;
        private System.Windows.Forms.Button goFast;
        private System.Windows.Forms.Button goVeryFast;
        private System.Windows.Forms.Panel canvas;
        private System.Windows.Forms.Button loadNet;
        private System.Windows.Forms.Button saveNet;
        private System.Windows.Forms.Button PauseBtn;
    }
}

