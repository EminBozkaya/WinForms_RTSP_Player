using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using WinForms_RTSP_Player.Data;

namespace WinForms_RTSP_Player
{
    public partial class SystemParametersForm : Form
    {
        public SystemParametersForm()
        {
            InitializeComponent();
        }

        private void SystemParametersForm_Load(object sender, EventArgs e)
        {
            LoadParameters();
        }

        private void LoadParameters()
        {
            try
            {
                DataTable dt = DatabaseManager.Instance.GetAllSystemParameters();
                dataGridView1.DataSource = dt;

                // Adjust column headers
                if (dataGridView1.Columns.Count > 0)
                {
                    if (dataGridView1.Columns.Contains("Id")) dataGridView1.Columns["Id"].Visible = false;
                    if (dataGridView1.Columns.Contains("Name")) dataGridView1.Columns["Name"].Visible = false;
                    if (dataGridView1.Columns.Contains("CreatedDate")) dataGridView1.Columns["CreatedDate"].Visible = false;
                    if (dataGridView1.Columns.Contains("UpdatedDate")) dataGridView1.Columns["UpdatedDate"].Visible = false;

                    if (dataGridView1.Columns.Contains("Detail")) dataGridView1.Columns["Detail"].HeaderText = "Parametre Açıklaması";
                    if (dataGridView1.Columns.Contains("Value")) dataGridView1.Columns["Value"].HeaderText = "Değer";
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Parametre yükleme hatası", "SystemParametersForm.LoadParameters", ex.ToString());
                MessageBox.Show("Parametreler yüklenirken bir hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                try
                {
                    DataRow row = ((DataRowView)dataGridView1.SelectedRows[0].DataBoundItem).Row;
                    int id = Convert.ToInt32(row["Id"]);
                    string name = row["Name"].ToString();
                    string detail = row["Detail"].ToString();
                    string value = row["Value"].ToString();

                    // LogRetentionDays hariç tüm parametreler için şifre sor
                    if (name != "LogRetentionDays")
                    {
                        string password = PromptForPassword();
                        if (password == null) return; // İptal edildi

                        if (password != "emin")
                        {
                            MessageBox.Show("Hatalı şifre. Düzenleme yetkiniz yok.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    using (var editForm = new EditParameterForm(id, name, detail, value))
                    {
                        if (editForm.ShowDialog() == DialogResult.OK)
                        {
                            LoadParameters();
                        }
                    }
                }
                catch (Exception ex)
                {
                    DatabaseManager.Instance.LogSystem("ERROR", "Düzenleme formu açma hatası", "SystemParametersForm.btnEdit_Click", ex.ToString());
                }
            }
            else
            {
                MessageBox.Show("Lütfen düzenlemek istediğiniz bir parametre seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                // Yeni parametre ekleme için her zaman şifre sor
                string password = PromptForPassword();
                if (password == null) return; // İptal edildi

                if (password != "emin")
                {
                    MessageBox.Show("Hatalı şifre. Parametre ekleme yetkiniz yok.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using (var addForm = new AddParameterForm())
                {
                    if (addForm.ShowDialog() == DialogResult.OK)
                    {
                        LoadParameters();
                    }
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Ekleme formu açma hatası", "SystemParametersForm.btnAdd_Click", ex.ToString());
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
                Text = "Yönetici Şifresi",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(40, 44, 52),
                ForeColor = Color.White
            };

            Label textLabel = new Label() { Left = 20, Top = 20, Width = 350, Text = "Bu işlem için yönetici şifresini giriniz:", Font = new Font("Segoe UI", 10) };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 350, PasswordChar = '*', Font = new Font("Segoe UI", 10) };
            Button confirmation = new Button() { Text = "Tamam", Left = 190, Width = 85, Top = 85, DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 65, 75) };
            Button cancel = new Button() { Text = "İptal", Left = 285, Width = 85, Top = 85, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat, BackColor = Color.Maroon };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : null;
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
