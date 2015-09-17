namespace MarkerParser
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
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.TBInputFile = new System.Windows.Forms.TextBox();
            this.TBOutputFile = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.TBMarkerFile = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.ProgressBar = new System.Windows.Forms.ProgressBar();
            this.label6 = new System.Windows.Forms.Label();
            this.TBTime = new System.Windows.Forms.TextBox();
            this.btnGenTime = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(376, 59);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(108, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Select Input File";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(376, 268);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(110, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "Write Markers";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(376, 139);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(108, 23);
            this.button3.TabIndex = 2;
            this.button3.Text = "Select Output File";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(43, 273);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Status : ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(125, 273);
            this.label2.MaximumSize = new System.Drawing.Size(230, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(22, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "     ";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(55, 63);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Input File : ";
            // 
            // TBInputFile
            // 
            this.TBInputFile.Location = new System.Drawing.Point(128, 59);
            this.TBInputFile.Name = "TBInputFile";
            this.TBInputFile.Size = new System.Drawing.Size(228, 20);
            this.TBInputFile.TabIndex = 6;
            // 
            // TBOutputFile
            // 
            this.TBOutputFile.Location = new System.Drawing.Point(128, 142);
            this.TBOutputFile.Name = "TBOutputFile";
            this.TBOutputFile.Size = new System.Drawing.Size(228, 20);
            this.TBOutputFile.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(55, 146);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(67, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Output File : ";
            // 
            // TBMarkerFile
            // 
            this.TBMarkerFile.Location = new System.Drawing.Point(128, 168);
            this.TBMarkerFile.Name = "TBMarkerFile";
            this.TBMarkerFile.ReadOnly = true;
            this.TBMarkerFile.Size = new System.Drawing.Size(228, 20);
            this.TBMarkerFile.TabIndex = 10;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(55, 172);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(68, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Marker File : ";
            // 
            // ProgressBar
            // 
            this.ProgressBar.Location = new System.Drawing.Point(110, 336);
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(271, 23);
            this.ProgressBar.TabIndex = 11;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(55, 25);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(66, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "Config Time:";
            // 
            // TBTime
            // 
            this.TBTime.Location = new System.Drawing.Point(127, 22);
            this.TBTime.Name = "TBTime";
            this.TBTime.Size = new System.Drawing.Size(228, 20);
            this.TBTime.TabIndex = 13;
            // 
            // btnGenTime
            // 
            this.btnGenTime.Location = new System.Drawing.Point(376, 25);
            this.btnGenTime.Name = "btnGenTime";
            this.btnGenTime.Size = new System.Drawing.Size(108, 23);
            this.btnGenTime.TabIndex = 14;
            this.btnGenTime.Text = "Generate";
            this.btnGenTime.UseVisualStyleBackColor = true;
            this.btnGenTime.Click += new System.EventHandler(this.button4_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(507, 401);
            this.Controls.Add(this.btnGenTime);
            this.Controls.Add(this.TBTime);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.ProgressBar);
            this.Controls.Add(this.TBMarkerFile);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.TBOutputFile);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.TBInputFile);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "Shimmer Data Parser";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox TBInputFile;
        private System.Windows.Forms.TextBox TBOutputFile;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox TBMarkerFile;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ProgressBar ProgressBar;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox TBTime;
        private System.Windows.Forms.Button btnGenTime;
    }
}

