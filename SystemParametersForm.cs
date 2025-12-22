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

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
