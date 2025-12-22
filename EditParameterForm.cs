using System;
using System.Drawing;
using System.Windows.Forms;
using WinForms_RTSP_Player.Data;
using WinForms_RTSP_Player.Utilities;

namespace WinForms_RTSP_Player
{
    public partial class EditParameterForm : Form
    {
        private int _id;
        private string _name;
        private string _currentValue;

        public EditParameterForm(int id, string name, string detail, string currentValue)
        {
            InitializeComponent();
            _id = id;
            _name = name;
            _currentValue = currentValue;

            lblDetail.Text = detail;
            lblCurrentValue.Text = currentValue;
            txtNewValue.Text = currentValue;
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            string newValue = txtNewValue.Text.Trim();

            if (string.IsNullOrEmpty(newValue))
            {
                MessageBox.Show("Değer girmek zorunludur.", "Validasyon Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Type validation based on parameter name or current value type
            if (!ValidateType(_name, newValue))
            {
                return;
            }

            try
            {
                bool success = DatabaseManager.Instance.UpdateSystemParameter(_id, newValue);
                if (success)
                {
                    // Reload parameters in memory
                    SystemParameters.Load();

                    DatabaseManager.Instance.LogSystem("INFO", $"Parametre güncellendi: {_name}, Eski: {_currentValue}, Yeni: {newValue}", "EditParameterForm.btnUpdate_Click");
                    MessageBox.Show("Parametre başarıyla güncellendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Parametre güncellenirken bir hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Parametre güncelleme hatası", "EditParameterForm.btnUpdate_Click", ex.ToString());
                MessageBox.Show("Bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateType(string paramName, string value)
        {
            // Simple type check based on common patterns in parameter names
            // All current parameters seem to be numeric (int or double)
            
            if (paramName.EndsWith("Interval") || paramName.EndsWith("Limit") || paramName.EndsWith("SECONDS") || paramName.EndsWith("Length") || paramName.EndsWith("Time"))
            {
                if (!int.TryParse(value, out _))
                {
                    MessageBox.Show("Bu parametre tam sayı (integer) tipinde olmalıdır.", "Validasyon Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            else if (paramName.EndsWith("Threshold") || paramName.EndsWith("Confidence"))
            {
                if (!double.TryParse(value, out _) && !int.TryParse(value, out _))
                {
                    MessageBox.Show("Bu parametre sayısal (numeric) tipte olmalıdır.", "Validasyon Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            
            return true;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
