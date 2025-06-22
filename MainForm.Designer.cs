namespace WinForms_RTSP_Player
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private LibVLCSharp.WinForms.VideoView videoView1;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label lblPlate;
        private System.Windows.Forms.Button btnTest;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.videoView1 = new LibVLCSharp.WinForms.VideoView();
            this.btnStart = new System.Windows.Forms.Button();
            this.lblPlate = new System.Windows.Forms.Label();
            this.btnTest = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.videoView1)).BeginInit();
            this.SuspendLayout();
            // 
            // videoView1
            // 
            this.videoView1.BackColor = System.Drawing.Color.Black;
            this.videoView1.Location = new System.Drawing.Point(10, 10);
            this.videoView1.MediaPlayer = null;
            this.videoView1.Name = "videoView1";
            this.videoView1.Size = new System.Drawing.Size(1280, 720);
            this.videoView1.TabIndex = 0;
            this.videoView1.Text = "videoView1";
            // 
            // btnStart
            // 
            this.btnStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnStart.Location = new System.Drawing.Point(10, 736);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(132, 63);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Başlat";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // lblPlate
            // 
            this.lblPlate.AutoSize = true;
            this.lblPlate.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.lblPlate.Location = new System.Drawing.Point(230, 736);
            this.lblPlate.Name = "lblPlate";
            this.lblPlate.Size = new System.Drawing.Size(532, 65);
            this.lblPlate.TabIndex = 2;
            this.lblPlate.Text = "Tespit Edilen Plaka: ---";
            // 
            // btnTest
            // 
            this.btnTest.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnTest.Location = new System.Drawing.Point(1162, 736);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(128, 63);
            this.btnTest.TabIndex = 1;
            this.btnTest.Text = "Plaka Test";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1307, 824);
            this.Controls.Add(this.lblPlate);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.videoView1);
            this.Name = "MainForm";
            this.Text = "RTSP Plaka Tanıma";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.videoView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

    }
}
