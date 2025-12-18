namespace WinForms_RTSP_Player
{
    partial class PlateRecognitionForm
    {
        private System.ComponentModel.IContainer components = null;
        private LibVLCSharp.WinForms.VideoView videoViewIN;
        private LibVLCSharp.WinForms.VideoView videoViewOUT;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.TableLayoutPanel tableLayoutCameras;
        private System.Windows.Forms.Panel panelIN;
        private System.Windows.Forms.Panel panelOUT;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Label lblResultIN;
        private System.Windows.Forms.Label lblStatusIN;
        private System.Windows.Forms.Label lblResultOUT;
        private System.Windows.Forms.Label lblStatusOUT;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Label lblCamTitleIN;
        private System.Windows.Forms.Label lblCamTitleOUT;
        private System.Windows.Forms.Panel panelResultsIN;
        private System.Windows.Forms.Panel panelResultsOUT;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            videoViewIN = new LibVLCSharp.WinForms.VideoView();
            videoViewOUT = new LibVLCSharp.WinForms.VideoView();
            btnStart = new Button();
            btnTest = new Button();
            panelTop = new Panel();
            lblTitle = new Label();
            tableLayoutCameras = new TableLayoutPanel();
            panelIN = new Panel();
            lblCamTitleIN = new Label();
            panelResultsIN = new Panel();
            lblStatusIN = new Label();
            lblResultIN = new Label();
            panelOUT = new Panel();
            lblCamTitleOUT = new Label();
            panelResultsOUT = new Panel();
            lblStatusOUT = new Label();
            lblResultOUT = new Label();
            btnBack = new Button();
            panelBottom = new Panel();
            ((System.ComponentModel.ISupportInitialize)videoViewIN).BeginInit();
            ((System.ComponentModel.ISupportInitialize)videoViewOUT).BeginInit();
            panelTop.SuspendLayout();
            tableLayoutCameras.SuspendLayout();
            panelIN.SuspendLayout();
            panelResultsIN.SuspendLayout();
            panelOUT.SuspendLayout();
            panelResultsOUT.SuspendLayout();
            panelBottom.SuspendLayout();
            SuspendLayout();
            // 
            // videoViewIN
            // 
            videoViewIN.BackColor = Color.Black;
            videoViewIN.Dock = DockStyle.Fill;
            videoViewIN.Location = new Point(0, 0);
            videoViewIN.MediaPlayer = null;
            videoViewIN.Name = "videoViewIN";
            videoViewIN.Size = new Size(566, 635);
            videoViewIN.TabIndex = 0;
            // 
            // videoViewOUT
            // 
            videoViewOUT.BackColor = Color.Black;
            videoViewOUT.Dock = DockStyle.Fill;
            videoViewOUT.Location = new Point(0, 0);
            videoViewOUT.MediaPlayer = null;
            videoViewOUT.Name = "videoViewOUT";
            videoViewOUT.Size = new Size(566, 635);
            videoViewOUT.TabIndex = 0;
            // 
            // btnStart
            // 
            btnStart.BackColor = Color.FromArgb(0, 122, 204);
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnStart.ForeColor = Color.White;
            btnStart.Location = new Point(18, 15);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(150, 50);
            btnStart.TabIndex = 0;
            btnStart.Text = "▶ SİSTEMİ BAŞLAT";
            btnStart.UseVisualStyleBackColor = false;
            btnStart.Click += btnStart_Click;
            // 
            // btnTest
            // 
            btnTest.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnTest.BackColor = Color.FromArgb(0, 150, 136);
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.FlatStyle = FlatStyle.Flat;
            btnTest.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnTest.ForeColor = Color.White;
            btnTest.Location = new Point(993, 15);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(143, 50);
            btnTest.TabIndex = 1;
            btnTest.Text = "\U0001f9ea TEST";
            btnTest.UseVisualStyleBackColor = false;
            btnTest.Click += btnTest_Click;
            // 
            // panelTop
            // 
            panelTop.BackColor = Color.FromArgb(45, 45, 48);
            panelTop.Controls.Add(lblTitle);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(1148, 60);
            panelTop.TabIndex = 2;
            // 
            // lblTitle
            // 
            lblTitle.Dock = DockStyle.Left;
            lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(0, 122, 204);
            lblTitle.Location = new Point(0, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Padding = new Padding(15, 12, 0, 0);
            lblTitle.Size = new Size(357, 60);
            lblTitle.TabIndex = 3;
            lblTitle.Text = "📷 PES Plaka Tanıma Sistemi";
            // 
            // tableLayoutCameras
            // 
            tableLayoutCameras.ColumnCount = 2;
            tableLayoutCameras.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutCameras.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutCameras.Controls.Add(panelIN, 0, 0);
            tableLayoutCameras.Controls.Add(panelOUT, 1, 0);
            tableLayoutCameras.Dock = DockStyle.Fill;
            tableLayoutCameras.Location = new Point(0, 60);
            tableLayoutCameras.Name = "tableLayoutCameras";
            tableLayoutCameras.RowCount = 1;
            tableLayoutCameras.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutCameras.Size = new Size(1148, 643);
            tableLayoutCameras.TabIndex = 0;
            // 
            // panelIN
            // 
            panelIN.BorderStyle = BorderStyle.FixedSingle;
            panelIN.Controls.Add(lblCamTitleIN);
            panelIN.Controls.Add(panelResultsIN);
            panelIN.Controls.Add(videoViewIN);
            panelIN.Dock = DockStyle.Fill;
            panelIN.Location = new Point(3, 3);
            panelIN.Name = "panelIN";
            panelIN.Size = new Size(568, 637);
            panelIN.TabIndex = 0;
            // 
            // lblCamTitleIN
            // 
            lblCamTitleIN.AutoSize = true;
            lblCamTitleIN.Dock = DockStyle.Top;
            lblCamTitleIN.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblCamTitleIN.ForeColor = Color.FromArgb(0, 122, 204);
            lblCamTitleIN.Location = new Point(0, 0);
            lblCamTitleIN.Name = "lblCamTitleIN";
            lblCamTitleIN.Padding = new Padding(10);
            lblCamTitleIN.Size = new Size(181, 41);
            lblCamTitleIN.TabIndex = 1;
            lblCamTitleIN.Text = "🚪 GİRİŞ KAMERASI";
            // 
            // panelResultsIN
            // 
            panelResultsIN.BackColor = Color.FromArgb(35, 35, 35);
            panelResultsIN.Controls.Add(lblStatusIN);
            panelResultsIN.Controls.Add(lblResultIN);
            panelResultsIN.Dock = DockStyle.Bottom;
            panelResultsIN.Location = new Point(0, 507);
            panelResultsIN.Name = "panelResultsIN";
            panelResultsIN.Size = new Size(566, 128);
            panelResultsIN.TabIndex = 4;
            // 
            // lblStatusIN
            // 
            lblStatusIN.AutoSize = true;
            lblStatusIN.Font = new Font("Segoe UI", 12F);
            lblStatusIN.ForeColor = Color.Silver;
            lblStatusIN.Location = new Point(10, 55);
            lblStatusIN.Name = "lblStatusIN";
            lblStatusIN.Size = new Size(208, 21);
            lblStatusIN.TabIndex = 3;
            lblStatusIN.Text = "Sistem Durumu: Bekleniyor...";
            // 
            // lblResultIN
            // 
            lblResultIN.AutoSize = true;
            lblResultIN.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblResultIN.ForeColor = Color.White;
            lblResultIN.Location = new Point(10, 15);
            lblResultIN.Name = "lblResultIN";
            lblResultIN.Size = new Size(245, 30);
            lblResultIN.TabIndex = 2;
            lblResultIN.Text = "Tespit Edilen Plaka: ---";
            // 
            // panelOUT
            // 
            panelOUT.BorderStyle = BorderStyle.FixedSingle;
            panelOUT.Controls.Add(lblCamTitleOUT);
            panelOUT.Controls.Add(panelResultsOUT);
            panelOUT.Controls.Add(videoViewOUT);
            panelOUT.Dock = DockStyle.Fill;
            panelOUT.Location = new Point(577, 3);
            panelOUT.Name = "panelOUT";
            panelOUT.Size = new Size(568, 637);
            panelOUT.TabIndex = 1;
            // 
            // lblCamTitleOUT
            // 
            lblCamTitleOUT.AutoSize = true;
            lblCamTitleOUT.Dock = DockStyle.Top;
            lblCamTitleOUT.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblCamTitleOUT.ForeColor = Color.FromArgb(0, 150, 136);
            lblCamTitleOUT.Location = new Point(0, 0);
            lblCamTitleOUT.Name = "lblCamTitleOUT";
            lblCamTitleOUT.Padding = new Padding(10);
            lblCamTitleOUT.Size = new Size(180, 41);
            lblCamTitleOUT.TabIndex = 1;
            lblCamTitleOUT.Text = "🛣️ ÇIKIŞ KAMERASI";
            // 
            // panelResultsOUT
            // 
            panelResultsOUT.BackColor = Color.FromArgb(35, 35, 35);
            panelResultsOUT.Controls.Add(lblStatusOUT);
            panelResultsOUT.Controls.Add(lblResultOUT);
            panelResultsOUT.Dock = DockStyle.Bottom;
            panelResultsOUT.Location = new Point(0, 507);
            panelResultsOUT.Name = "panelResultsOUT";
            panelResultsOUT.Size = new Size(566, 128);
            panelResultsOUT.TabIndex = 4;
            // 
            // lblStatusOUT
            // 
            lblStatusOUT.AutoSize = true;
            lblStatusOUT.Font = new Font("Segoe UI", 12F);
            lblStatusOUT.ForeColor = Color.Silver;
            lblStatusOUT.Location = new Point(10, 55);
            lblStatusOUT.Name = "lblStatusOUT";
            lblStatusOUT.Size = new Size(208, 21);
            lblStatusOUT.TabIndex = 3;
            lblStatusOUT.Text = "Sistem Durumu: Bekleniyor...";
            // 
            // lblResultOUT
            // 
            lblResultOUT.AutoSize = true;
            lblResultOUT.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblResultOUT.ForeColor = Color.White;
            lblResultOUT.Location = new Point(10, 15);
            lblResultOUT.Name = "lblResultOUT";
            lblResultOUT.Size = new Size(245, 30);
            lblResultOUT.TabIndex = 2;
            lblResultOUT.Text = "Tespit Edilen Plaka: ---";
            // 
            // btnBack
            // 
            btnBack.Anchor = AnchorStyles.Bottom;
            btnBack.BackColor = Color.FromArgb(64, 64, 64);
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnBack.ForeColor = Color.White;
            btnBack.Location = new Point(473, 23);
            btnBack.Name = "btnBack";
            btnBack.Size = new Size(202, 35);
            btnBack.TabIndex = 4;
            btnBack.Text = "🏠 Ana Sayfaya Dön";
            btnBack.UseVisualStyleBackColor = false;
            btnBack.Click += btnBack_Click;
            // 
            // panelBottom
            // 
            panelBottom.BackColor = Color.FromArgb(45, 45, 48);
            panelBottom.Controls.Add(btnStart);
            panelBottom.Controls.Add(btnBack);
            panelBottom.Controls.Add(btnTest);
            panelBottom.Dock = DockStyle.Bottom;
            panelBottom.Location = new Point(0, 703);
            panelBottom.Name = "panelBottom";
            panelBottom.Size = new Size(1148, 80);
            panelBottom.TabIndex = 1;
            // 
            // PlateRecognitionForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(1148, 783);
            Controls.Add(tableLayoutCameras);
            Controls.Add(panelTop);
            Controls.Add(panelBottom);
            ForeColor = Color.White;
            Name = "PlateRecognitionForm";
            Text = "PES Plaka Tanıma Sistemi - ÇOKLU KAMERA";
            FormClosing += MainForm_FormClosing;
            ((System.ComponentModel.ISupportInitialize)videoViewIN).EndInit();
            ((System.ComponentModel.ISupportInitialize)videoViewOUT).EndInit();
            panelTop.ResumeLayout(false);
            tableLayoutCameras.ResumeLayout(false);
            panelIN.ResumeLayout(false);
            panelIN.PerformLayout();
            panelResultsIN.ResumeLayout(false);
            panelResultsIN.PerformLayout();
            panelOUT.ResumeLayout(false);
            panelOUT.PerformLayout();
            panelResultsOUT.ResumeLayout(false);
            panelResultsOUT.PerformLayout();
            panelBottom.ResumeLayout(false);
            ResumeLayout(false);
        }

    }
}
