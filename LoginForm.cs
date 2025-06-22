using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinForms_RTSP_Player
{
    public partial class LoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnCancel;
        private Label lblTitle;
        private Label lblUsername;
        private Label lblPassword;

        public LoginForm()
        {
            InitializeComponent();
            SetupForm();
        }

        private void SetupForm()
        {
            // Form ayarları
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "Yönetim Paneli Girişi";
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            this.AcceptButton = btnLogin;
            this.CancelButton = btnCancel;

            // Başlık
            lblTitle = new Label
            {
                Text = "🔐 YÖNETİM PANELİ GİRİŞİ",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(350, 40),
                Location = new Point(25, 30)
            };

            // Kullanıcı adı label
            lblUsername = new Label
            {
                Text = "Kullanıcı Adı:",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.White,
                Size = new Size(100, 20),
                Location = new Point(50, 100)
            };

            // Kullanıcı adı textbox
            txtUsername = new TextBox
            {
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Size = new Size(200, 25),
                Location = new Point(50, 125),
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Şifre label
            lblPassword = new Label
            {
                Text = "Şifre:",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.White,
                Size = new Size(100, 20),
                Location = new Point(50, 160)
            };

            // Şifre textbox
            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Size = new Size(200, 25),
                Location = new Point(50, 185),
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };

            // Giriş butonu
            btnLogin = new Button
            {
                Text = "Giriş Yap",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(100, 35),
                Location = new Point(50, 230),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            // İptal butonu
            btnCancel = new Button
            {
                Text = "İptal",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Size = new Size(100, 35),
                Location = new Point(160, 230),
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += BtnCancel_Click;

            // Kontrolleri forma ekle
            this.Controls.AddRange(new Control[] 
            { 
                lblTitle, 
                lblUsername, 
                txtUsername, 
                lblPassword, 
                txtPassword, 
                btnLogin, 
                btnCancel 
            });

            // Enter tuşu ile giriş
            this.KeyPreview = true;
            this.KeyDown += LoginForm_KeyDown;
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
            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) && 
                password.Equals("Park evleri", StringComparison.OrdinalIgnoreCase))
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

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            txtUsername.Focus();
        }
    }
} 