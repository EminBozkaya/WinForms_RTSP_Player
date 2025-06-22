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
            var plateRecognitionForm = new PlateRecognitionForm();
            plateRecognitionForm.Show();
            this.Hide();
        }

        private void SplashForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                Application.Exit();
            }
        }
    }
} 