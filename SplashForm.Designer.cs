namespace WinForms_RTSP_Player
{
    partial class SplashForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Button btnAdmin;
        private System.Windows.Forms.Button btnPlateRecognition;
        private System.Windows.Forms.Label lblInfo;

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
            lblTitle = new Label();
            lblSubtitle = new Label();
            btnAdmin = new Button();
            btnPlateRecognition = new Button();
            lblInfo = new Label();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point, 162);
            lblTitle.ForeColor = Color.FromArgb(0, 122, 204);
            lblTitle.Location = new Point(219, 122);
            lblTitle.Margin = new Padding(4, 0, 4, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(502, 45);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "🚗 PLAKA TANIMA SİSTEMİ 🚗";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.Font = new Font("Segoe UI", 14F, FontStyle.Regular, GraphicsUnit.Point, 162);
            lblSubtitle.ForeColor = Color.Silver;
            lblSubtitle.Location = new Point(349, 203);
            lblSubtitle.Margin = new Padding(4, 0, 4, 0);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(264, 25);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Akıllı Site Giriş Kontrol Sistemi";
            lblSubtitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnAdmin
            // 
            btnAdmin.BackColor = Color.FromArgb(0, 122, 204);
            btnAdmin.FlatAppearance.BorderSize = 0;
            btnAdmin.FlatStyle = FlatStyle.Flat;
            btnAdmin.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 162);
            btnAdmin.ForeColor = Color.White;
            btnAdmin.Location = new Point(321, 301);
            btnAdmin.Margin = new Padding(4, 3, 4, 3);
            btnAdmin.Name = "btnAdmin";
            btnAdmin.Size = new Size(292, 58);
            btnAdmin.TabIndex = 2;
            btnAdmin.Text = "🔧 YÖNETİM PANELİ";
            btnAdmin.UseVisualStyleBackColor = false;
            btnAdmin.Click += BtnAdmin_Click;
            // 
            // btnPlateRecognition
            // 
            btnPlateRecognition.BackColor = Color.FromArgb(0, 150, 136);
            btnPlateRecognition.FlatAppearance.BorderSize = 0;
            btnPlateRecognition.FlatStyle = FlatStyle.Flat;
            btnPlateRecognition.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 162);
            btnPlateRecognition.ForeColor = Color.White;
            btnPlateRecognition.Location = new Point(321, 394);
            btnPlateRecognition.Margin = new Padding(4, 3, 4, 3);
            btnPlateRecognition.Name = "btnPlateRecognition";
            btnPlateRecognition.Size = new Size(292, 58);
            btnPlateRecognition.TabIndex = 3;
            btnPlateRecognition.Text = "📷 PES PLAKA TANIMA PROGRAMI";
            btnPlateRecognition.UseVisualStyleBackColor = false;
            btnPlateRecognition.Click += BtnPlateRecognition_Click;
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = true;
            lblInfo.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 162);
            lblInfo.ForeColor = Color.Gray;
            lblInfo.Location = new Point(288, 500);
            lblInfo.Margin = new Padding(4, 0, 4, 0);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(363, 15);
            lblInfo.TabIndex = 4;
            lblInfo.Text = "© PES (Park Evleri Sitesi) Plaka Tanıma Sistemi - Tüm hakları saklıdır";
            lblInfo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // SplashForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(45, 45, 48);
            ClientSize = new Size(933, 692);
            Controls.Add(lblInfo);
            Controls.Add(btnPlateRecognition);
            Controls.Add(btnAdmin);
            Controls.Add(lblSubtitle);
            Controls.Add(lblTitle);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            Name = "SplashForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Plaka Tanıma Sistemi - Ana Menü";
            FormClosing += SplashForm_FormClosing;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
} 