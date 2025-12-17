using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using WinForms_RTSP_Player.Data;

namespace WinForms_RTSP_Player
{
    public partial class VehicleIORecords : Form
    {
        private DatabaseManager _dbManager;

        public VehicleIORecords()
        {
            try
            {
                InitializeComponent();
                _dbManager = DatabaseManager.Instance;
                
                // Subscribe to CellFormatting event before loading data
                dataGridViewLogs.CellFormatting += DataGridViewLogs_CellFormatting;
                
                LoadAccessLogs();
                
                // Allow multiple row selection
                dataGridViewLogs.MultiSelect = true;
                
                DatabaseManager.Instance.LogSystem("INFO", "Araç giriş/çıkış kayıtları ekranı açıldı", "VehicleIORecords.Constructor");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Araç giriş/çıkış kayıtları ekranı yükleme hatası", "VehicleIORecords.Constructor", ex.ToString());
            }
        }

        private void LoadAccessLogs()
        {
            try
            {
                DataTable dt = _dbManager.GetAccessLog(1000); // Get last 1000 records
                dataGridViewLogs.DataSource = dt;
                FormatGrid();
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Giriş/çıkış kayıtları yükleme hatası", "VehicleIORecords.LoadAccessLogs", ex.ToString());
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
                if (dataGridViewLogs.Columns.Contains("PlateNumber"))
                    dataGridViewLogs.Columns["PlateNumber"].HeaderText = "Plaka";

                if (dataGridViewLogs.Columns.Contains("PlateOwner"))
                    dataGridViewLogs.Columns["PlateOwner"].HeaderText = "Araç Sahibi";

                if (dataGridViewLogs.Columns.Contains("AccessType"))
                    dataGridViewLogs.Columns["AccessType"].HeaderText = "Giriş/Çıkış";

                if (dataGridViewLogs.Columns.Contains("AccessTime"))
                    dataGridViewLogs.Columns["AccessTime"].HeaderText = "Zaman";

                if (dataGridViewLogs.Columns.Contains("IsAuthorized"))
                    dataGridViewLogs.Columns["IsAuthorized"].HeaderText = "Yetki_Durumu";

                if (dataGridViewLogs.Columns.Contains("Confidence"))
                    dataGridViewLogs.Columns["Confidence"].HeaderText = "Doğruluk_Oranı";

                if (dataGridViewLogs.Columns.Contains("Notes"))
                    dataGridViewLogs.Columns["Notes"].HeaderText = "Notlar";
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Grid formatlama hatası", "VehicleIORecords.FormatGrid", ex.ToString());
            }
        }

        private void DataGridViewLogs_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                if (dataGridViewLogs.Columns[e.ColumnIndex].Name == "AccessType" && e.Value != null)
                {
                    string accessType = e.Value.ToString();
                    if (accessType == "IN")
                        e.Value = "Giriş";
                    else if (accessType == "OUT")
                        e.Value = "Çıkış";
                }
                else if (dataGridViewLogs.Columns[e.ColumnIndex].Name == "IsAuthorized" && e.Value != null)
                {
                    int isAuthorized = Convert.ToInt32(e.Value);
                    e.Value = isAuthorized == 1 ? "Kayıtlı" : "Kayıtsız";
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Hücre formatlama hatası", "VehicleIORecords.DataGridViewLogs_CellFormatting", ex.ToString());
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
                        DatabaseManager.Instance.LogSystem("WARNING", "Yanlış şifre ile silme denemesi", "VehicleIORecords.btnDelete_Click");
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
                        $"{idsToDelete.Count} adet kayıt silinecek. Emin misiniz?",
                        "Silme Onayı",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        if (_dbManager.DeleteAccessLogs(idsToDelete))
                        {
                            MessageBox.Show("Kayıtlar başarıyla silindi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            DatabaseManager.Instance.LogSystem("INFO", $"{idsToDelete.Count} adet giriş/çıkış kaydı silindi", "VehicleIORecords.btnDelete_Click");
                            LoadAccessLogs(); // Refresh grid
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
                DatabaseManager.Instance.LogSystem("ERROR", "Kayıt silme butonu hatası", "VehicleIORecords.btnDelete_Click", ex.ToString());
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
    }
}
