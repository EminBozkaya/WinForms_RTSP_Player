using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenCvSharp;
using WinForms_RTSP_Player.Business;
using WinForms_RTSP_Player.Utilities;
using static WinForms_RTSP_Player.Utilities.PlateRecognitionHelper;

namespace WinForms_RTSP_Player
{
    public partial class TestForm : Form
    {
        private List<string> imageFiles;
        private int currentIndex = -1;
        private string selectedFolder = "";
        private PictureBox pictureBoxPlate;

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

            // Kapı Test Butonu
            Button btnTestGate = new Button();
            btnTestGate.Text = "🚪 Kapıyı Test Et";
            btnTestGate.Size = new System.Drawing.Size(150, 40);
            btnTestGate.Location = new System.Drawing.Point(this.Width - 180, this.Height - 100);
            btnTestGate.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnTestGate.Click += (s, e) => {
                _ = HardwareController.Instance.OpenGateAsync();
                MessageBox.Show("Kapı açma komutu gönderildi.");
            };
            this.Controls.Add(btnTestGate);

            // Plate Crop Visualization
            pictureBoxPlate = new PictureBox();
            pictureBoxPlate.Size = new System.Drawing.Size(320, 60);
            pictureBoxPlate.Location = new System.Drawing.Point(this.Width - 350, 20);
            pictureBoxPlate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pictureBoxPlate.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxPlate.BorderStyle = BorderStyle.FixedSingle;
            pictureBoxPlate.BackColor = Color.Black;
            this.Controls.Add(pictureBoxPlate);
            
            Label lblCrop = new Label();
            lblCrop.Text = "Ocr Giriş (Kesilen Plaka):";
            lblCrop.ForeColor = Color.White;
            lblCrop.Location = new System.Drawing.Point(this.Width - 350, 5);
            lblCrop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.Controls.Add(lblCrop);
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
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Test resmi yükleme hatası", "TestForm.LoadCurrentImage", ex.ToString());
                lblPlate.Text = "Tespit Edilen Plaka: Hata!";
            }
        }

        private string RecognizePlateFromImage(string imagePath)
        {
            // Mat objects must be disposed
            Mat frame = null;
            Mat cropped = null;

            try
            {
                lblPlate.Text = "Tespit Edilen Plaka: İşleniyor...";
                lblPlate.ForeColor = Color.Yellow;

                // 1. Load image with OpenCV
                frame = Cv2.ImRead(imagePath);
                if (frame.Empty())
                {
                    lblPlate.Text = "Hata: Resim okunamadı";
                    return "Hata";
                }

                // 2. Detect Plate ROI (YOLO)
                var roi = PlateDetectionEngine.Instance.DetectPrimaryRoi(frame);

                string resultText = "";
                float confidence = 0f;

                if (roi.Width > 0 && roi.Height > 0)
                {
                    // 3. Crop ROI
                    cropped = new Mat(frame, roi);

                    // SHOW CROP IN UI
                    var oldImage = pictureBoxPlate.Image;
                    pictureBoxPlate.Image = cropped.ToBitmap();
                    oldImage?.Dispose();

                    // 4. Recognize Text (PaddleOCR)
                    var ocrResult = OcrEngine.Instance.RecognizeText(cropped);
                    
                    resultText = ocrResult.Text;
                    confidence = ocrResult.Confidence;
                }
                else
                {
                    // ROI not found - FALLBACK: Try whole frame (case for very zoomed in images)
                    lblPlate.Text = "Tespit: Başarısız. Fallback (Tüm Resim) deneniyor...";
                    
                    var ocrResult = OcrEngine.Instance.RecognizeText(frame);
                    resultText = ocrResult.Text;
                    confidence = ocrResult.Confidence;
                    
                    // Show frame as "crop" in UI to indicate fallback
                    var oldImage = pictureBoxPlate.Image;
                    pictureBoxPlate.Image = frame.ToBitmap();
                    oldImage?.Dispose();
                }        

                // 5. Validate and Clean Result
                if (!string.IsNullOrEmpty(resultText))
                {
                    // Plakayı düzelt (Türk formatına uygun hale getir)
                    string correctedPlate = PlateSanitizer.ValidateTurkishPlateFormat(resultText);
                    
                    if (!string.IsNullOrEmpty(correctedPlate))
                    {
                        lblPlate.Text = $"Tespit Edilen Plaka: {correctedPlate} (Conf: %{confidence * 100:F1})";
                        lblPlate.ForeColor = Color.FromArgb(0, 200, 83);
                        return correctedPlate;
                    }
                    else
                    {
                        lblPlate.Text = $"Plaka Formatı Geçersiz: {resultText} (Conf: %{confidence * 100:F1})";
                        lblPlate.ForeColor = Color.FromArgb(244, 67, 54);
                        return "Geçersiz format";
                    }
                }
                else
                {
                    lblPlate.Text = "Tespit Edilen Plaka: Okunamadı";
                    lblPlate.ForeColor = Color.FromArgb(244, 67, 54);
                    return "Okunamadı";
                }
            }
            catch (Exception ex)
            {
                lblPlate.Text = "Tespit Edilen Plaka: Hata";
                lblPlate.ForeColor = Color.FromArgb(244, 67, 54);
                WinForms_RTSP_Player.Data.DatabaseManager.Instance.LogSystem("ERROR", "Test OCR hatası", "TestForm.RecognizePlateFromImage", ex.ToString());
                return "Hata";
            }
            finally
            {
                // Dispose OpenCV resources
                frame?.Dispose();
                cropped?.Dispose();
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
            if (pictureBoxPlate.Image != null)
            {
                pictureBoxPlate.Image.Dispose();
                pictureBoxPlate.Image = null;
            }
        }
    }
}
