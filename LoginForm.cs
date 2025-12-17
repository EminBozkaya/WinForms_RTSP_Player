using System;
using System.Drawing;
using System.Windows.Forms;
using System.Configuration;

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
            try
            {
                ValidateLogin();
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Giriş işlemi hatası", "LoginForm.BtnLogin_Click", ex.ToString());
                MessageBox.Show("Bir hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Giriş iptal hatası", "LoginForm.BtnCancel_Click", ex.ToString());
            }
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Klavye olayı hatası", "LoginForm.LoginForm_KeyDown", ex.ToString());
            }
        }

        private void ValidateLogin()
        {
            try
            {
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Text.Trim();

                // Kullanıcı adı: admin, Şifre: Park evleri
                string configUsername = ConfigurationManager.AppSettings["Username"];
                string configPassword = ConfigurationManager.AppSettings["Password"];

                // Kullanıcı adı: admin, Şifre: Park evleri
                if (username.Equals(configUsername, StringComparison.OrdinalIgnoreCase) && 
                    password.Equals(configPassword, StringComparison.OrdinalIgnoreCase))
                {
                    WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("INFO", $"Başarılı giriş: {username}", "LoginForm.ValidateLogin");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("WARNING", $"Hatalı giriş denemesi: {username}", "LoginForm.ValidateLogin", "Kullanıcı adı veya şifre yanlış");
                    MessageBox.Show("Kullanıcı adı veya şifre hatalı!", 
                        "Giriş Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Giriş doğrulama hatası", "LoginForm.ValidateLogin", ex.ToString());
                throw; // Rethrow to be caught by caller
            }
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtUsername.Focus();
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("INFO", "Giriş ekranı yüklendi", "LoginForm.LoginForm_Load");
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Giriş ekranı yükleme hatası", "LoginForm.LoginForm_Load", ex.ToString());
            }
        }
    }
} 