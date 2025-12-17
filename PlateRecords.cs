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
            // Placeholder for Add logic
            MessageBox.Show("Yeni araç ekleme formu burada açılacak.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridViewPlates.SelectedRows.Count > 0)
            {
                var row = dataGridViewPlates.SelectedRows[0];
                string plate = row.Cells["PlateNumber"].Value.ToString();
                MessageBox.Show($"'{plate}' plakalı aracı düzenleme formu burada açılacak.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    // TODO: Implement actual delete in DatabaseManager
                    MessageBox.Show("Silme işlemi henüz aktif değil (Database update required).", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Lütfen silmek için bir satır seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
