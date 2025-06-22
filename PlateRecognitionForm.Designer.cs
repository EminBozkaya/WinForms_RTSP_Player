namespace WinForms_RTSP_Player
{
    partial class PlateRecognitionForm
    {
        private System.ComponentModel.IContainer components = null;
        private LibVLCSharp.WinForms.VideoView videoView1;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label lblPlate;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.Panel panelVideo;
        private System.Windows.Forms.Panel panelControls;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label lblResult;
        private System.Windows.Forms.Button btnSelectFolder;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            videoView1 = new LibVLCSharp.WinForms.VideoView();
            btnStart = new Button();
            lblPlate = new Label();
            btnTest = new Button();
            panelVideo = new Panel();
            panelControls = new Panel();
            lblResult = new Label();
            btnSelectFolder = new Button();
            lblTitle = new Label();
            panelStatus = new Panel();
            lblStatus = new Label();
            ((System.ComponentModel.ISupportInitialize)videoView1).BeginInit();
            panelVideo.SuspendLayout();
            panelControls.SuspendLayout();
            SuspendLayout();
            // 
            // videoView1
            // 
            videoView1.BackColor = Color.Black;
            videoView1.Dock = DockStyle.Fill;
            videoView1.Location = new Point(0, 0);
            videoView1.Margin = new Padding(4, 3, 4, 3);
            videoView1.MediaPlayer = null;
            videoView1.Name = "videoView1";
            videoView1.Size = new Size(1118, 621);
            videoView1.TabIndex = 0;
            videoView1.Text = "videoView1";
            // 
            // btnStart
            // 
            btnStart.Location = new Point(0, 0);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(75, 23);
            btnStart.TabIndex = 0;
            // 
            // lblPlate
            // 
            lblPlate.Location = new Point(0, 0);
            lblPlate.Name = "lblPlate";
            lblPlate.Size = new Size(100, 23);
            lblPlate.TabIndex = 0;
            // 
            // btnTest
            // 
            btnTest.BackColor = Color.FromArgb(0, 150, 136);
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.FlatStyle = FlatStyle.Flat;
            btnTest.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 162);
            btnTest.ForeColor = Color.White;
            btnTest.Location = new Point(997, 17);
            btnTest.Margin = new Padding(4, 3, 4, 3);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(105, 35);
            btnTest.TabIndex = 1;
            btnTest.Text = "\U0001f9ea TEST";
            btnTest.UseVisualStyleBackColor = false;
            btnTest.Click += btnTest_Click;
            // 
            // panelVideo
            // 
            panelVideo.BackColor = Color.FromArgb(30, 30, 30);
            panelVideo.BorderStyle = BorderStyle.FixedSingle;
            panelVideo.Controls.Add(videoView1);
            panelVideo.Location = new Point(18, 69);
            panelVideo.Margin = new Padding(4, 3, 4, 3);
            panelVideo.Name = "panelVideo";
            panelVideo.Size = new Size(1120, 623);
            panelVideo.TabIndex = 0;
            // 
            // panelControls
            // 
            panelControls.BackColor = Color.FromArgb(45, 45, 48);
            panelControls.Controls.Add(lblStatus);
            panelControls.Controls.Add(lblResult);
            panelControls.Controls.Add(btnTest);
            panelControls.Controls.Add(btnSelectFolder);
            panelControls.Location = new Point(18, 710);
            panelControls.Margin = new Padding(4, 3, 4, 3);
            panelControls.Name = "panelControls";
            panelControls.Size = new Size(1120, 66);
            panelControls.TabIndex = 1;
            // 
            // lblResult
            // 
            lblResult.AutoSize = true;
            lblResult.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 162);
            lblResult.ForeColor = Color.White;
            lblResult.Location = new Point(254, 24);
            lblResult.Margin = new Padding(4, 0, 4, 0);
            lblResult.Name = "lblResult";
            lblResult.Size = new Size(270, 32);
            lblResult.TabIndex = 2;
            lblResult.Text = "Tespit Edilen Plaka: ---";
            // 
            // btnSelectFolder
            // 
            btnSelectFolder.BackColor = Color.FromArgb(0, 122, 204);
            btnSelectFolder.FlatAppearance.BorderSize = 0;
            btnSelectFolder.FlatStyle = FlatStyle.Flat;
            btnSelectFolder.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 162);
            btnSelectFolder.ForeColor = Color.White;
            btnSelectFolder.Location = new Point(18, 17);
            btnSelectFolder.Margin = new Padding(4, 3, 4, 3);
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(105, 35);
            btnSelectFolder.TabIndex = 1;
            btnSelectFolder.Text = "▶ BAŞLAT";
            btnSelectFolder.UseVisualStyleBackColor = false;
            btnSelectFolder.Click += btnStart_Click;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point, 162);
            lblTitle.ForeColor = Color.FromArgb(0, 122, 204);
            lblTitle.Location = new Point(18, 17);
            lblTitle.Margin = new Padding(4, 0, 4, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(311, 30);
            lblTitle.TabIndex = 3;
            lblTitle.Text = "📷 PES Plaka Tanıma Sistemi";
            // 
            // panelStatus
            // 
            panelStatus.Location = new Point(0, 0);
            panelStatus.Name = "panelStatus";
            panelStatus.Size = new Size(200, 100);
            panelStatus.TabIndex = 0;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(532, 5);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(89, 15);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "Sistem Durumu";
            // 
            // PlateRecognitionForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(1148, 783);
            Controls.Add(lblTitle);
            Controls.Add(panelControls);
            Controls.Add(panelVideo);
            ForeColor = Color.White;
            Margin = new Padding(4, 3, 4, 3);
            Name = "PlateRecognitionForm";
            Text = "PES Plaka Tanıma Sistemi";
            FormClosing += MainForm_FormClosing;
            ((System.ComponentModel.ISupportInitialize)videoView1).EndInit();
            panelVideo.ResumeLayout(false);
            panelControls.ResumeLayout(false);
            panelControls.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private Label lblStatus;
    }
}
