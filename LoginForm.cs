using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinForms_RTSP_Player
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            ValidateLogin();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ValidateLogin();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void ValidateLogin()
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            // Kullanıcı adı: admin, Şifre: Park evleri
            if (username.Equals("a", StringComparison.OrdinalIgnoreCase) && 
                password.Equals("p", StringComparison.OrdinalIgnoreCase))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Kullanıcı adı veya şifre hatalı!\n\nKullanıcı Adı: admin\nŞifre: Park evleri", 
                    "Giriş Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            txtUsername.Focus();
        }
    }
} 