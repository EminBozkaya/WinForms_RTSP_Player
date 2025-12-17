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
            InitializeComponent();
            _dbManager = new DatabaseManager();
            LoadPlates();
            
            // Add spacing between buttons manually if Dock doesn't handle margin perfectly in some versions
            btnEdit.Top = btnAdd.Bottom + 10;
            btnDelete.Top = btnEdit.Bottom + 10;
        }

        private void LoadPlates()
        {
            DataTable dt = _dbManager.GetPlates();
            dataGridViewPlates.DataSource = dt;
            FormatGrid();
        }

        private void FormatGrid()
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

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddPlateForm addForm = new AddPlateForm();
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadPlates(); // Refresh grid
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
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
                }
            }
            else
            {
                MessageBox.Show("Lütfen düzenlemek için bir satır seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
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
                        LoadPlates();
                    }
                    else
                    {
                        MessageBox.Show("Silme işlemi sırasında hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Lütfen silmek için bir satır seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
