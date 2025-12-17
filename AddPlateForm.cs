using System;
using System.Windows.Forms;
using WinForms_RTSP_Player.Data;

namespace WinForms_RTSP_Player
{
    public partial class AddPlateForm : Form
    {
        private DatabaseManager _dbManager;

        private int _recordId = 0;

        public AddPlateForm()
        {
            InitializeComponent();
            _dbManager = new DatabaseManager();
        }

        public AddPlateForm(int id, string plate, string owner, string type)
        {
            InitializeComponent();
            _dbManager = new DatabaseManager();
            
            _recordId = id;
            txtPlate.Text = plate;
            txtOwner.Text = owner;
            cmbType.Text = type;
            
            this.Text = "Araç Düzenle";
            btnSave.Text = "Kaydet";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string plate = txtPlate.Text.Trim().ToUpper();
            string owner = txtOwner.Text.Trim();
            string type = cmbType.Text.Trim();

            // Validation
            if (string.IsNullOrEmpty(plate))
            {
                MessageBox.Show("Plaka bilgisi olmadan kayıt işlemi yapılamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPlate.Focus();
                return;
            }

            if (string.IsNullOrEmpty(owner))
            {
                MessageBox.Show("Lütfen araç sahibinin bilgilerini girin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtOwner.Focus();
                return;
            }

            if (string.IsNullOrEmpty(type))
            {
                MessageBox.Show("Lütfen araç tipini seçin veya girin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbType.Focus();
                return;
            }

            bool success;
            if (_recordId > 0)
            {
                // Update
                success = _dbManager.UpdatePlate(_recordId, plate, owner, type);
            }
            else
            {
                // Insert
                success = _dbManager.AddPlate(plate, owner, type);
            }

            if (success)
            {
                MessageBox.Show("İşlem başarıyla tamamlandı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("İşlem sırasında bir hata oluştu. (Plaka tekrarı vb.)", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
