using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using WinForms_RTSP_Player.Utilities;
using System.Linq;
using System.Collections.Generic;

namespace WinForms_RTSP_Player
{
    public partial class TestForm : Form
    {
        private List<string> imageFiles;
        private int currentIndex = -1;
        private string selectedFolder = "";

        public TestForm()
        {
            InitializeComponent();
            
            // Form başlığı
            lblTitle.Text = "🧪 PES Plaka Tanıma Test Modülü";
            lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(0, 122, 204);

            // Plaka sonucu
            lblPlate.Text = "Tespit Edilen Plaka: ---";
            lblPlate.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblPlate.ForeColor = Color.White;

            // Butonları devre dışı bırak
            btnPrevious.Enabled = false;
            btnNext.Enabled = false;
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Plaka resimlerinin bulunduğu klasörü seçin";
                
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;
                    
                    // Desteklenen resim formatlarını al
                    string[] supportedExtensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.tiff" };
                    imageFiles = new List<string>();
                    
                    foreach (string extension in supportedExtensions)
                    {
                        imageFiles.AddRange(Directory.GetFiles(selectedPath, extension, SearchOption.TopDirectoryOnly));
                    }
                    
                    if (imageFiles.Count > 0)
                    {
                        currentIndex = 0;
                        LoadCurrentImage();
                        
                        // Butonları etkinleştir
                        btnPrevious.Enabled = true;
                        btnNext.Enabled = true;
                        
                        MessageBox.Show($"{imageFiles.Count} adet resim bulundu.", "Başarılı", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Seçilen klasörde desteklenen resim formatı bulunamadı.", "Uyarı", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                LoadCurrentImage();
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (currentIndex < imageFiles.Count - 1)
            {
                currentIndex++;
                LoadCurrentImage();
            }
        }

        private void LoadCurrentImage()
        {
            if (imageFiles.Count == 0 || currentIndex >= imageFiles.Count)
                return;

            try
            {
                string imagePath = imageFiles[currentIndex];
                using (var image = Image.FromFile(imagePath))
                {
                    pictureBox1.Image = new Bitmap(image);
                }

                // Plaka tanıma işlemi
                string detectedPlate = RecognizePlateFromImage(imagePath);
                lblPlate.Text = $"Tespit Edilen Plaka: {detectedPlate}";
                
                // Dosya adını başlıkta göster
                string fileName = Path.GetFileName(imagePath);
                this.Text = $"PES Plaka Tanıma Test Modülü - {fileName} ({currentIndex + 1}/{imageFiles.Count})";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Resim yüklenirken hata: {ex.Message}");
                lblPlate.Text = "Tespit Edilen Plaka: Hata!";
            }
        }

        private string RecognizePlateFromImage(string imagePath)
        {
            try
            {
                lblPlate.Text = "Tespit Edilen Plaka: İşleniyor...";
                lblPlate.ForeColor = Color.Yellow;

                // OpenALPR ile analiz et
                string result = PlateRecognitionHelper.RunOpenALPR(imagePath);
                string plate = PlateRecognitionHelper.ExtractPlateFromJson(result);
                
                if (!string.IsNullOrEmpty(plate) && plate != "Plaka geçersiz veya okunamadı.")
                {
                    // Plakayı düzelt (Türk formatına uygun hale getir)
                    string correctedPlate = PlateSanitizer.ValidateTurkishPlateFormat(plate);
                    
                    if (!string.IsNullOrEmpty(correctedPlate))
                    {
                        lblPlate.Text = $"Tespit Edilen Plaka: {correctedPlate}";
                        lblPlate.ForeColor = Color.FromArgb(0, 200, 83);
                        return correctedPlate;
                    }
                    else
                    {
                        lblPlate.Text = "Tespit Edilen Plaka: Geçersiz format";
                        lblPlate.ForeColor = Color.FromArgb(244, 67, 54);
                        return "Geçersiz format";
                    }
                }
                else
                {
                    lblPlate.Text = "Tespit Edilen Plaka: Bulunamadı";
                    lblPlate.ForeColor = Color.FromArgb(244, 67, 54);
                    return "Bulunamadı";
                }
            }
            catch (Exception ex)
            {
                lblPlate.Text = "Tespit Edilen Plaka: Hata";
                lblPlate.ForeColor = Color.FromArgb(244, 67, 54);
                Console.WriteLine($"Plaka tanıma hatası: {ex.Message}");
                return "Hata";
            }
        }

        private void TestForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Form kapatılırken temizlik işlemleri
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }
        }
    }
}
