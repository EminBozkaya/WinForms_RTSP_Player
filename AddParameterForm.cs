using System;
using System.Drawing;
using System.Windows.Forms;
using WinForms_RTSP_Player.Data;
using WinForms_RTSP_Player.Utilities;

namespace WinForms_RTSP_Player
{
    public partial class AddParameterForm : Form
    {
        public AddParameterForm()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            string value = txtValue.Text.Trim();
            string detail = txtDetail.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value) || string.IsNullOrEmpty(detail))
            {
                MessageBox.Show("Tüm alanları doldurmak zorunludur.", "Validasyon Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                bool success = DatabaseManager.Instance.AddSystemParameter(name, value, detail);
                if (success)
                {
                    // Reload parameters in memory
                    SystemParameters.Load();

                    DatabaseManager.Instance.LogSystem("INFO", $"Yeni parametre eklendi: {name}, Değer: {value}", "AddParameterForm.btnSave_Click");
                    MessageBox.Show("Parametre başarıyla eklendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Parametre eklenirken bir hata oluştu. Aynı isimde bir parametre zaten var olabilir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Parametre ekleme hatası", "AddParameterForm.btnSave_Click", ex.ToString());
                MessageBox.Show("Bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
