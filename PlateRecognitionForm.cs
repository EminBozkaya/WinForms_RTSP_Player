using System;
//using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using Newtonsoft.Json.Linq;
using WinForms_RTSP_Player.Utilities;
using WinForms_RTSP_Player.Data;
using System.Configuration;
using System.Drawing;

namespace WinForms_RTSP_Player
{
    public partial class PlateRecognitionForm : Form
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private System.Windows.Forms.Timer _frameCaptureTimer;

        private System.Windows.Forms.Timer _streamHealthTimer;          // Stream sağlığı için timer
        private DateTime _lastVideoUpdateTime;     // Son video frame zaman damgası

        //private string _rtspUrl = "rtsp://192.168.0.101/user=admin_password=tlJwpbo6_channel=1_stream=0.sdp?real_stream";//Eski kamera
        private string _rtspUrl = string.Empty; // RTSP URL'si App.config'den alınacak

        private DatabaseManager _databaseManager; // Veri tabanı yöneticisi

        public PlateRecognitionForm()
        {
            InitializeComponent();
            Core.Initialize(@"libvlc\win-x64");

            _rtspUrl = ConfigurationManager.AppSettings["RtspUrl"];
            if (string.IsNullOrEmpty(_rtspUrl))
            {
                MessageBox.Show("RTSP bağlantı adresi App.config dosyasında bulunamadı!");
                return;
            }

            var libvlcOptions = new[]
            {
                "--network-caching=50",
                //"--rtsp-tcp",
                //"--no-skip-frames",
                "--no-video-title-show",
                //"--video-title=0",
                "--no-osd",
                //"--no-overlay",
                "--no-snapshot-preview",


                //"--rtsp-timeout=60",
                //"--rtsp-frame-buffer-size=100000",
                //"--avcodec-hw=auto",
                "--avcodec-hw=dxva2",
                "--clock-synchro=1",
                "--clock-jitter=0",
            };

            _libVLC = new LibVLC(libvlcOptions);
            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.Mute = true;
            videoView1.MediaPlayer = _mediaPlayer;

            // Video frame geldiğinde zaman damgasını güncelle
            _mediaPlayer.TimeChanged += (s, e) =>
            {
                _lastVideoUpdateTime = DateTime.Now;
            };

            _frameCaptureTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            _frameCaptureTimer.Tick += FrameCaptureTimer_Tick;

            // Stream sağlık kontrol timer
            _streamHealthTimer = new System.Windows.Forms.Timer { Interval = 900000 }; // 15 dakikada bir kontrol
            _streamHealthTimer.Tick += (s, e) => CheckStreamHealth();

            // Veri tabanı yöneticisini başlat
            _databaseManager = new DatabaseManager();
            Console.WriteLine("Veri tabanı başlatıldı.");
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            _mediaPlayer.Play(new Media(_libVLC, _rtspUrl, FromType.FromLocation));
            Console.WriteLine($"Media oynatıcısı başlatıldı: {DateTime.Now}");
            _frameCaptureTimer.Start();

            _lastVideoUpdateTime = DateTime.Now;
            _streamHealthTimer.Start();
        }

        private void FrameCaptureTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                string tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp.jpg");
                bool success = _mediaPlayer.TakeSnapshot(0, tempPath, 0, 0);

                if (success && File.Exists(tempPath))
                {
                    string result = PlateRecognitionHelper.RunOpenALPR(tempPath);
                    string plate = PlateRecognitionHelper.ExtractPlateFromJson(result);
                    
                    if (!string.IsNullOrEmpty(plate) && plate != "Plaka geçersiz veya okunamadı.")
                    {
                        // Plakayı düzelt (Türk formatına uygun hale getir)
                        string correctedPlate = PlateSanitizer.ValidateTurkishPlateFormat(plate);
                        
                        // Veri tabanında kontrol et
                        bool isAuthorized = _databaseManager.IsPlateAuthorized(correctedPlate);
                        
                        // Sonucu ekranda göster
                        string status = isAuthorized ? "✅ İZİNLİ" : "❌ İZİNSİZ";
                        Color statusColor = isAuthorized ? Color.FromArgb(0, 200, 83) : Color.FromArgb(244, 67, 54);
                        
                        lblResult.Text = $"Tespit Edilen Plaka: {correctedPlate}";
                        lblResult.ForeColor = statusColor;
                        
                        // Durum etiketini güncelle
                        lblStatus.Text = $"Sistem Durumu: {status}";
                        lblStatus.ForeColor = statusColor;
                        
                        // Erişim logunu kaydet
                        _databaseManager.LogAccess(correctedPlate, "IN", isAuthorized);
                        
                        // Sistem logunu kaydet
                        string logMessage = isAuthorized ? 
                            $"İzinli araç girişi: {correctedPlate}" : 
                            $"İzinsiz araç girişi: {correctedPlate}";
                        _databaseManager.LogSystem("INFO", logMessage);
                        
                        Console.WriteLine($"*******************{DateTime.Now}****************************");
                        Console.WriteLine($"OCR okunan: {result}");
                        Console.WriteLine($"-----");
                        Console.WriteLine($"Düzeltilmiş Plaka: {correctedPlate}");
                        Console.WriteLine($"Yetki Durumu: {status}");
                        
                        // Eğer izinliyse kapıyı aç (bu kısmı daha sonra ekleyeceğiz)
                        if (isAuthorized)
                        {
                            Console.WriteLine("🚪 Kapı açılıyor...");
                            // TODO: Kapı açma kodu buraya gelecek
                        }
                    }
                    else
                    {
                        lblResult.Text = "Tespit Edilen Plaka: ---";
                        lblResult.ForeColor = Color.Silver;
                        lblStatus.Text = "Sistem Durumu: Bekleniyor...";
                        lblStatus.ForeColor = Color.Silver;
                        Console.WriteLine("🎯 Plaka okunamadı veya geçersiz.");
                    }
                    
                    File.Delete(tempPath);
                }
                else Console.WriteLine("🎯 Ekran görüntüsü alınamadı veya dosya bulunamadı.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata: " + ex.Message);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _frameCaptureTimer.Stop();
            _frameCaptureTimer.Dispose();

            _streamHealthTimer.Stop();
            _streamHealthTimer.Dispose();

            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            var testForm = new TestForm();
            testForm.Show();
        }

        private void CheckStreamHealth()
        {
            var secondsSinceLastFrame = (DateTime.Now - _lastVideoUpdateTime).TotalSeconds;

            if (secondsSinceLastFrame > 3) // 3 saniye boyunca yeni frame gelmediyse
            {
                Console.WriteLine($"⚠ Frame akışı {secondsSinceLastFrame:F1} sn durdu. RTSP yeniden başlatılıyor...{DateTime.Now}");
                try
                {
                    _mediaPlayer.Stop();
                    _mediaPlayer.Play(new Media(_libVLC, _rtspUrl, FromType.FromLocation));
                    Console.WriteLine($"Media oynatıcısı başlatıldı: {DateTime.Now}");
                    _lastVideoUpdateTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("🎯 Yeniden bağlantı hatası: " + ex.Message);
                }
            }
        }

        private void UpdateStatus(string status, string plate = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStatus(status, plate)));
                return;
            }

            lblStatus.Text = $"Sistem Durumu: {status}";
            if (!string.IsNullOrEmpty(plate))
            {
                lblResult.Text = $"Tespit Edilen Plaka: {plate}";
            }
        }
    }
}
