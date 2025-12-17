using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace WinForms_RTSP_Player
{
    public partial class AdminForm : Form
    {
        public AdminForm()
        {
            InitializeComponent();
            CenterPanel();
            this.Resize += (s, e) => CenterPanel();
        }

        private void CenterPanel()
        {
            if (centerPanel != null)
            {
                centerPanel.Location = new Point(
                    (this.ClientSize.Width - centerPanel.Width) / 2, 
                    (this.ClientSize.Height - centerPanel.Height) / 2
                );
            }
        }

        private void centerPanel_Paint(object sender, PaintEventArgs e)
        {
            // Optional: for any custom painting, otherwise unused.
        }

        private void btnPlates_Click(object sender, EventArgs e)
        {
            new PlateRecords().ShowDialog();
        }

        private void btnEntryExit_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Araç Giriş/Çıkış Kayıtları henüz aktif değil.");
        }

        private void btnSystemLogs_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Sistem Genel Kayıtları henüz aktif değil.");
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
