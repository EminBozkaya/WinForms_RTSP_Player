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
            try
            {
                InitializeComponent();
                CenterPanel();
                this.Resize += (s, e) => CenterPanel();
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("INFO", "Yönetici paneli açıldı", "AdminForm.Constructor");
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Yönetici paneli başlatma hatası", "AdminForm.Constructor", ex.ToString());
            }
        }

        private void CenterPanel()
        {
            try
            {
                if (centerPanel != null)
                {
                    centerPanel.Location = new Point(
                        (this.ClientSize.Width - centerPanel.Width) / 2, 
                        (this.ClientSize.Height - centerPanel.Height) / 2
                    );
                }
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Panel ortalama hatası", "AdminForm.CenterPanel", ex.ToString());
            }
        }

        private void centerPanel_Paint(object sender, PaintEventArgs e)
        {
            // Optional: for any custom painting, otherwise unused.
        }

        private void btnPlates_Click(object sender, EventArgs e)
        {
            try
            {
                new PlateRecords().ShowDialog();
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Plaka kayıtları açma hatası", "AdminForm.btnPlates_Click", ex.ToString());
            }
        }

        private void btnEntryExit_Click(object sender, EventArgs e)
        {
            try
            {
                MessageBox.Show("Araç Giriş/Çıkış Kayıtları henüz aktif değil.");
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Giriş/Çıkış buton hatası", "AdminForm.btnEntryExit_Click", ex.ToString());
            }
        }

        private void btnSystemLogs_Click(object sender, EventArgs e)
        {
            try
            {
                MessageBox.Show("Sistem Genel Kayıtları henüz aktif değil.");
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Sistem logları buton hatası", "AdminForm.btnSystemLogs_Click", ex.ToString());
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Çıkış hatası", "AdminForm.btnExit_Click", ex.ToString());
            }
        }
    }
}
