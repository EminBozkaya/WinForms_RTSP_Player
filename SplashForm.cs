using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinForms_RTSP_Player
{
    public partial class SplashForm : Form
    {
        public SplashForm()
        {
            InitializeComponent();
            try
            {
               WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("INFO", "Uygulama açıldı (SplashForm)", "SplashForm.Constructor");
            }
            catch { /* Log failure shouldn't crash app start */ }
        }

        private void BtnAdmin_Click(object sender, EventArgs e)
        {
            try
            {
                var loginForm = new LoginForm();
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    var adminForm = new AdminForm();
                    adminForm.FormClosed += (s, args) => this.Show();
                    adminForm.Show();
                    this.Hide();
                    WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("INFO", "Yönetici paneli geçişi", "SplashForm.BtnAdmin_Click");
                }
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Yönetici paneli açma hatası", "SplashForm.BtnAdmin_Click", ex.ToString());
            }
        }

        private void BtnPlateRecognition_Click(object sender, EventArgs e)
        {
            try
            {
                var plateRecognitionForm = new PlateRecognitionForm();
                plateRecognitionForm.FormClosed += (s, args) => this.Show();
                plateRecognitionForm.Show();
                this.Hide();
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("INFO", "Plaka tanıma ekranı geçişi", "SplashForm.BtnPlateRecognition_Click");
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Plaka tanıma ekranı açma hatası", "SplashForm.BtnPlateRecognition_Click", ex.ToString());
            }
        }

        private void SplashForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("INFO", "Uygulama kapatılıyor", "SplashForm.SplashForm_FormClosing");
                Application.Exit();
            }
        }
    }
} 