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
                
                // Set custom icons
                string resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
                string logIconPath = Path.Combine(resourcesPath, "system_logs_v2_icon.png");
                string paramIconPath = Path.Combine(resourcesPath, "system_parameters_icon.png");

                if (File.Exists(logIconPath)) btnSystemLogs.Image = ResizeImage(Image.FromFile(logIconPath), 48, 48);
                if (File.Exists(paramIconPath)) btnSystemParameters.Image = ResizeImage(Image.FromFile(paramIconPath), 48, 48);

                this.Resize += (s, e) => CenterPanel();
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("INFO", "Yönetici paneli açıldı", "AdminForm.Constructor");
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Yönetici paneli başlatma hatası", "AdminForm.Constructor", ex.ToString());
            }
        }

        private Image ResizeImage(Image imgToResize, int width, int height)
        {
            Bitmap b = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage((Image)b))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(imgToResize, 0, 0, width, height);
            }
            return (Image)b;
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
                new VehicleIORecords().ShowDialog();
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Giriş/Çıkış kayıtları açma hatası", "AdminForm.btnEntryExit_Click", ex.ToString());
            }
        }

        private void btnSystemLogs_Click(object sender, EventArgs e)
        {
            try
            {
                new SystemLogs().ShowDialog();
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Sistem kayıtları açma hatası", "AdminForm.btnSystemLogs_Click", ex.ToString());
            }
        }
        
        private void btnSystemParameters_Click(object sender, EventArgs e)
        {
            try
            {
                new SystemParametersForm().ShowDialog();
            }
            catch (Exception ex)
            {
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Sistem parametreleri açma hatası", "AdminForm.btnSystemParameters_Click", ex.ToString());
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
