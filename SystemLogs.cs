using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows.Forms;
using WinForms_RTSP_Player.Data;
using WinForms_RTSP_Player.Utilities;

namespace WinForms_RTSP_Player
{
    public partial class SystemLogs : Form
    {
        private DatabaseManager _dbManager;

        public SystemLogs()
        {
            try
            {
                InitializeComponent();
                _dbManager = DatabaseManager.Instance;
                LoadSystemLogs();
                
                // Allow multiple row selection
                dataGridViewLogs.MultiSelect = true;
                
                DatabaseManager.Instance.LogSystem("INFO", "Sistem genel kayıtları ekranı açıldı", "SystemLogs.Constructor");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Sistem genel kayıtları ekranı yükleme hatası", "SystemLogs.Constructor", ex.ToString());
            }
        }

        private void LoadSystemLogs()
        {
            try
            {
                DataTable dt = _dbManager.GetSystemLog(SystemParameters.GetSystemLogLimit); // Get last N records from parameters
                dataGridViewLogs.DataSource = dt;
                FormatGrid();
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Sistem kayıtları yükleme hatası", "SystemLogs.LoadSystemLogs", ex.ToString());
            }
        }

        private void FormatGrid()
        {
            try
            {
                if (dataGridViewLogs.Columns.Count == 0) return;

                // Hide Id column
                if (dataGridViewLogs.Columns.Contains("Id"))
                    dataGridViewLogs.Columns["Id"].Visible = false;

                // Rename headers
                if (dataGridViewLogs.Columns.Contains("LogLevel"))
                    dataGridViewLogs.Columns["LogLevel"].HeaderText = "Seviye";

                if (dataGridViewLogs.Columns.Contains("Message"))
                    dataGridViewLogs.Columns["Message"].HeaderText = "Mesaj";

                if (dataGridViewLogs.Columns.Contains("LogTime"))
                    dataGridViewLogs.Columns["LogTime"].HeaderText = "Zaman";

                if (dataGridViewLogs.Columns.Contains("Component"))
                    dataGridViewLogs.Columns["Component"].HeaderText = "Bileşen";

                if (dataGridViewLogs.Columns.Contains("Details"))
                    dataGridViewLogs.Columns["Details"].HeaderText = "Detaylar";
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Grid formatlama hatası", "SystemLogs.FormatGrid", ex.ToString());
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridViewLogs.SelectedRows.Count > 0)
                {
                    // Prompt for password
                    string password = PromptForPassword();
                    
                    if (password == null)
                    {
                        // User cancelled
                        return;
                    }

                    // Verify password from User.config
                    string correctPassword = ConfigurationManager.AppSettings["SuperAdminPassword"];
                    
                    if (password != correctPassword)
                    {
                        MessageBox.Show("Şifreyi yanlış girdiniz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        DatabaseManager.Instance.LogSystem("WARNING", "Yanlış şifre ile sistem log silme denemesi", "SystemLogs.btnDelete_Click");
                        return;
                    }

                    // Collect selected IDs
                    List<int> idsToDelete = new List<int>();
                    foreach (DataGridViewRow row in dataGridViewLogs.SelectedRows)
                    {
                        int id = Convert.ToInt32(row.Cells["Id"].Value);
                        idsToDelete.Add(id);
                    }

                    // Confirm deletion
                    DialogResult result = MessageBox.Show(
                        $"{idsToDelete.Count} adet sistem kaydı silinecek. Emin misiniz?",
                        "Silme Onayı",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        if (_dbManager.DeleteSystemLogs(idsToDelete))
                        {
                            MessageBox.Show("Kayıtlar başarıyla silindi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            DatabaseManager.Instance.LogSystem("INFO", $"{idsToDelete.Count} adet sistem kaydı silindi", "SystemLogs.btnDelete_Click");
                            LoadSystemLogs(); // Refresh grid
                        }
                        else
                        {
                            MessageBox.Show("Silme işlemi sırasında hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Lütfen silmek için en az bir satır seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Sistem kayıt silme butonu hatası", "SystemLogs.btnDelete_Click", ex.ToString());
                MessageBox.Show("Bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string PromptForPassword()
        {
            // Create a simple password prompt dialog
            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Şifre Gerekli",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label textLabel = new Label() { Left = 20, Top = 20, Width = 350, Text = "Silme işlemi için süper admin şifresini giriniz:" };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 350, PasswordChar = '*' };
            Button confirmation = new Button() { Text = "Tamam", Left = 200, Width = 80, Top = 80, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "İptal", Left = 290, Width = 80, Top = 80, DialogResult = DialogResult.Cancel };

            confirmation.Click += (sender, e) => { prompt.Close(); };
            cancel.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : null;
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                LoadSystemLogs();
                DatabaseManager.Instance.LogSystem("INFO", "Sistem kayıtları yenilendi", "SystemLogs.btnRefresh_Click");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Sistem kayıtları yenileme hatası", "SystemLogs.btnRefresh_Click", ex.ToString());
            }
        }
    }
}
