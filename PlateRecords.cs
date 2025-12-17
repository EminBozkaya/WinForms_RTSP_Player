using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using WinForms_RTSP_Player.Data;

namespace WinForms_RTSP_Player
{
    public partial class PlateRecords : Form
    {
        private DatabaseManager _dbManager;

        public PlateRecords()
        {
            try
            {
                InitializeComponent();
                _dbManager = DatabaseManager.Instance;
                LoadPlates();
                
                // Add spacing between buttons manually if Dock doesn't handle margin perfectly in some versions
                btnEdit.Top = btnAdd.Bottom + 10;
                btnDelete.Top = btnEdit.Bottom + 10;
                
                DatabaseManager.Instance.LogSystem("INFO", "Plaka kayıtları ekranı açıldı", "PlateRecords.Constructor");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Plaka kayıtları ekranı yükleme hatası", "PlateRecords.Constructor", ex.ToString());
            }
        }

        private void LoadPlates()
        {
            try
            {
                DataTable dt = _dbManager.GetPlates();
                dataGridViewPlates.DataSource = dt;
                FormatGrid();
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Plaka listesi yükleme hatası", "PlateRecords.LoadPlates", ex.ToString());
            }
        }

        private void FormatGrid()
        {
            try
            {
                if (dataGridViewPlates.Columns.Count == 0) return;

                // Rename headers
                if (dataGridViewPlates.Columns.Contains("PlateNumber"))
                    dataGridViewPlates.Columns["PlateNumber"].HeaderText = "Plaka";
                
                if (dataGridViewPlates.Columns.Contains("OwnerName"))
                    dataGridViewPlates.Columns["OwnerName"].HeaderText = "Araç Sahibi";
                
                if (dataGridViewPlates.Columns.Contains("VehicleType"))
                    dataGridViewPlates.Columns["VehicleType"].HeaderText = "Araç Tipi";
                
                if (dataGridViewPlates.Columns.Contains("CreatedDate"))
                    dataGridViewPlates.Columns["CreatedDate"].HeaderText = "Kayıt Tarihi";
                    
                if (dataGridViewPlates.Columns.Contains("UpdatedDate"))
                    dataGridViewPlates.Columns["UpdatedDate"].HeaderText = "Güncelleme Tarihi";

                if (dataGridViewPlates.Columns.Contains("IsActive"))
                {
                    dataGridViewPlates.Columns["IsActive"].Visible = false; // Hide or format
                }
                
                if (dataGridViewPlates.Columns.Contains("Id"))
                {
                    dataGridViewPlates.Columns["Id"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Grid formatlama hatası", "PlateRecords.FormatGrid", ex.ToString());
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                AddPlateForm addForm = new AddPlateForm();
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    LoadPlates(); // Refresh grid
                    DatabaseManager.Instance.LogSystem("INFO", "Yeni plaka eklendi (Dialog OK)", "PlateRecords.btnAdd_Click");
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Plaka ekleme butonu hatası", "PlateRecords.btnAdd_Click", ex.ToString());
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridViewPlates.SelectedRows.Count > 0)
                {
                    var row = dataGridViewPlates.SelectedRows[0];
                    int id = Convert.ToInt32(row.Cells["Id"].Value);
                    string plate = row.Cells["PlateNumber"].Value.ToString();
                    string owner = row.Cells["OwnerName"].Value.ToString();
                    string type = row.Cells["VehicleType"].Value.ToString();

                    AddPlateForm editForm = new AddPlateForm(id, plate, owner, type);
                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        LoadPlates(); // Refresh grid
                        DatabaseManager.Instance.LogSystem("INFO", $"Plaka düzenlendi: {plate}", "PlateRecords.btnEdit_Click");
                    }
                }
                else
                {
                    MessageBox.Show("Lütfen düzenlemek için bir satır seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Plaka düzenleme butonu hatası", "PlateRecords.btnEdit_Click", ex.ToString());
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridViewPlates.SelectedRows.Count > 0)
                {
                    var row = dataGridViewPlates.SelectedRows[0];
                    string plate = row.Cells["PlateNumber"].Value.ToString();
                    DialogResult result = MessageBox.Show($"'{plate}' plakalı aracı silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        int id = Convert.ToInt32(row.Cells["Id"].Value);
                        if (_dbManager.SoftDeletePlate(id))
                        {
                            MessageBox.Show("Kayıt başarıyla silindi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            DatabaseManager.Instance.LogSystem("INFO", $"Plaka silindi: {plate}", "PlateRecords.btnDelete_Click");
                            LoadPlates();
                        }
                        else
                        {
                            MessageBox.Show("Silme işlemi sırasında hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            DatabaseManager.Instance.LogSystem("ERROR", $"Plaka silinemedi: {plate}", "PlateRecords.btnDelete_Click");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Lütfen silmek için bir satır seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Plaka silme butonu hatası", "PlateRecords.btnDelete_Click", ex.ToString());
            }
        }
    }
}
