using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace WinForms_RTSP_Player
{
    public partial class SplashForm : Form
    {
        public SplashForm()
        {
            InitializeComponent();
            SetupForm();
        }

        private void SetupForm()
        {
            // Form ayarları
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Plaka Tanıma Sistemi - Ana Menü";
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            // Başlık
            Label lblTitle = new Label
            {
                Text = "🚗 PLAKA TANIMA SİSTEMİ 🚗",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(600, 50),
                Location = new Point(100, 50)
            };

            // Alt başlık
            Label lblSubtitle = new Label
            {
                Text = "Akıllı Site Giriş Kontrol Sistemi",
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = Color.Silver,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(400, 30),
                Location = new Point(200, 120)
            };

            // Yönetim Paneli Butonu
            Button btnAdmin = new Button
            {
                Text = "🔧 YÖNETİM PANELİ",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(250, 50),
                Location = new Point(275, 200),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAdmin.FlatAppearance.BorderSize = 0;
            btnAdmin.Click += BtnAdmin_Click;

            // Plaka Tanıma Butonu
            Button btnPlateRecognition = new Button
            {
                Text = "📷 PES PLAKA TANIMA PROGRAMI",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(250, 50),
                Location = new Point(275, 270),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPlateRecognition.FlatAppearance.BorderSize = 0;
            btnPlateRecognition.Click += BtnPlateRecognition_Click;

            // Alt bilgi
            Label lblInfo = new Label
            {
                Text = "© PES (Park Evleri Sitesi) Plaka Tanıma Sistemi - Tüm hakları saklıdır",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(400, 20),
                Location = new Point(200, 520)
            };

            // Kontrolleri forma ekle
            this.Controls.AddRange(new Control[] 
            { 
                lblTitle, 
                lblSubtitle, 
                btnAdmin, 
                btnPlateRecognition, 
                lblInfo 
            });
        }

        private void BtnAdmin_Click(object sender, EventArgs e)
        {
            var loginForm = new LoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                // TODO: AdminForm henüz oluşturulmadı
                MessageBox.Show("Yönetim Paneli henüz geliştirilme aşamasında!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                /*
                var adminForm = new AdminForm();
                adminForm.Show();
                this.Hide();
                */
            }
        }

        private void BtnPlateRecognition_Click(object sender, EventArgs e)
        {
            var mainForm = new MainForm();
            mainForm.Show();
            this.Hide();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                Application.Exit();
            }
            base.OnFormClosing(e);
        }
    }
} 