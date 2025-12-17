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
            try
            {
                InitializeComponent();
                _dbManager = DatabaseManager.Instance;
                DatabaseManager.Instance.LogSystem("INFO", "Plaka ekleme ekranı açıldı", "AddPlateForm.Constructor");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Plaka ekleme ekranı başlatma hatası", "AddPlateForm.Constructor", ex.ToString());
            }
        }

        public AddPlateForm(int id, string plate, string owner, string type)
        {
            try
            {
                InitializeComponent();
                _dbManager = DatabaseManager.Instance;
                
                _recordId = id;
                txtPlate.Text = plate;
                txtOwner.Text = owner;
                cmbType.Text = type;
                
                this.Text = "Araç Düzenle";
                btnSave.Text = "Kaydet";
                DatabaseManager.Instance.LogSystem("INFO", "Plaka düzenleme ekranı açıldı", "AddPlateForm.Constructor_Edit");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Plaka düzenleme ekranı başlatma hatası", "AddPlateForm.Constructor_Edit", ex.ToString());
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
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
                    if (success) DatabaseManager.Instance.LogSystem("INFO", $"Plaka güncellendi: {plate}", "AddPlateForm.btnSave_Click");
                }
                else
                {
                    // Insert
                    success = _dbManager.AddPlate(plate, owner, type);
                    if (success) DatabaseManager.Instance.LogSystem("INFO", $"Yeni plaka eklendi: {plate}", "AddPlateForm.btnSave_Click");
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
                    DatabaseManager.Instance.LogSystem("ERROR", $"Plaka kaydetme başarısız: {plate}", "AddPlateForm.btnSave_Click");
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Kaydet butonu hatası", "AddPlateForm.btnSave_Click", ex.ToString());
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "İptal butonu hatası", "AddPlateForm.btnCancel_Click", ex.ToString());
            }
        }
    }
}
