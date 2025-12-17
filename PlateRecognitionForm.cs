using LibVLCSharp.Shared;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Drawing;
//using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using WinForms_RTSP_Player.Data;
using WinForms_RTSP_Player.Utilities;
using static WinForms_RTSP_Player.Utilities.PlateRecognitionHelper;

namespace WinForms_RTSP_Player
{
    public partial class PlateRecognitionForm : Form
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private System.Windows.Forms.Timer _frameCaptureTimer;
        private System.Windows.Forms.Timer _heartbeatTimer;

        private System.Windows.Forms.Timer _streamHealthTimer;          // Stream sağlığı için timer
        private DateTime _lastVideoUpdateTime;     // Son video frame zaman damgası

        private string _rtspUrl = string.Empty; // RTSP URL'si App.config'den alınacak

        private DatabaseManager _databaseManager; // Veri tabanı yöneticisi

        public PlateRecognitionForm()
        {
            try
            {
                InitializeComponent();
                Core.Initialize(@"libvlc\win-x64");

                _rtspUrl = ConfigurationManager.AppSettings["RtspUrl"];
                if (string.IsNullOrEmpty(_rtspUrl))
                {
                    MessageBox.Show("RTSP bağlantı adresi App.config dosyasında bulunamadı!");
                    DatabaseManager.Instance.LogSystem("ERROR", "RTSP URL bulunamadı", "PlateRecognitionForm.Constructor");
                    return;
                }

                var libvlcOptions = new[]
                {
                    "--network-caching=50",
                    "--no-video-title-show",
                    "--no-osd",
                    "--no-snapshot-preview",
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

                // Heartbeat timer (5 dakika)
                _heartbeatTimer = new System.Windows.Forms.Timer { Interval = 300000 };
                _heartbeatTimer.Tick += (s, e) => DatabaseManager.Instance.LogSystem("INFO", "System Alive", "PlateRecognitionForm.Heartbeat");

                // Veri tabanı yöneticisini başlat - Singleton kullanılıyor ama form içinde field olarak tutuluyordu, yine field'a atayabiliriz veya direkt Instance kullanabiliriz.
                // Mevcut kod field kullanıyor, uyumlu olması için atama yapıyoruz.
                _databaseManager = DatabaseManager.Instance;
                DatabaseManager.Instance.LogSystem("INFO", "Plaka tanıma formu başlatıldı", "PlateRecognitionForm.Constructor");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Plaka tanıma formu başlatma hatası", "PlateRecognitionForm.Constructor", ex.ToString());
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                _mediaPlayer.Play(new Media(_libVLC, _rtspUrl, FromType.FromLocation));
                DatabaseManager.Instance.LogSystem("INFO", "Medya oynatıcı başlatıldı", "PlateRecognitionForm.btnStart_Click");
                _frameCaptureTimer.Start();

                _lastVideoUpdateTime = DateTime.Now;
                _streamHealthTimer.Start();
                _heartbeatTimer.Start();
                DatabaseManager.Instance.LogSystem("INFO", "Heartbeat timer başlatıldı (5 dk aralıkla)", "PlateRecognitionForm.btnStart_Click");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Yayın başlatma hatası", "PlateRecognitionForm.btnStart_Click", ex.ToString());
            }
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
                    PlateResult plateResult = PlateRecognitionHelper.ExtractPlateFromJson(result);
                    
                    if (plateResult != null && !string.IsNullOrEmpty(plateResult.Plate) && plateResult.Plate != "Plaka geçersiz veya okunamadı.")
                    {
                        // Plakayı düzelt (Türk formatına uygun hale getir)
                        string correctedPlate = PlateSanitizer.ValidateTurkishPlateFormat(plateResult.Plate);
                        
                        // Veri tabanında kontrol et
                        string plateOwner = "";
                        bool isAuthorized = _databaseManager.IsPlateAuthorized(correctedPlate);
                        
                        if (isAuthorized)
                        {
                             plateOwner = _databaseManager.GetPlateOwner(correctedPlate);
                        }
                        
                        // Sonucu ekranda göster
                        string status = isAuthorized ? "✅ İZİNLİ" : "❌ İZİNSİZ";
                        Color statusColor = isAuthorized ? Color.FromArgb(0, 200, 83) : Color.FromArgb(244, 67, 54);
                        
                        lblResult.Text = $"Tespit Edilen Plaka: {correctedPlate}";
                        lblResult.ForeColor = statusColor;
                        
                        // Durum etiketini güncelle
                        lblStatus.Text = $"Sistem Durumu: {status}";
                        lblStatus.ForeColor = statusColor;
                        
                        // Erişim logunu kaydet
                        _databaseManager.LogAccess(correctedPlate, plateOwner, "IN", isAuthorized, plateResult.Confidence);
                        
                        // Detaylı loglama (Debug amaçlı konsol yerine INFO log)
                        // Çok sık log oluşabileceği için burayı sadece access log yeterli olabilir ama debugging için konsol yerine log istenmiş.
                        // Ancak sürekli her frame için log basmak DB'yi şişirebilir. Sadece tanıma olduğunda AccessLog yetiyor.
                        // Konsol çıktılarını kaldırdık veya çok gerekliyse debug level (ama user level istemedi).
                        
                        // Eğer izinliyse kapıyı aç (bu kısmı daha sonra ekleyeceğiz)
                        if (isAuthorized)
                        {
                            // Console.WriteLine("🚪 Kapı açılıyor...");
                            // TODO: Kapı açma kodu buraya gelecek
                            DatabaseManager.Instance.LogSystem("INFO", $"Kapı açma tetiklendi: {correctedPlate}", "PlateRecognitionForm.FrameCaptureTimer_Tick");
                        }
                    }
                    else
                    {
                        lblResult.Text = "Tespit Edilen Plaka: ---";
                        lblResult.ForeColor = Color.Silver;
                        lblStatus.Text = "Sistem Durumu: Bekleniyor...";
                        lblStatus.ForeColor = Color.Silver;
                    }
                    
                    File.Delete(tempPath);
                }
                else 
                {
                    // Console.WriteLine("🎯 Ekran görüntüsü alınamadı veya dosya bulunamadı.");
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "OCR işlem hatası", "PlateRecognitionForm.FrameCaptureTimer_Tick", ex.ToString());
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _frameCaptureTimer.Stop();
                _frameCaptureTimer.Dispose();

                _streamHealthTimer.Stop();
                _streamHealthTimer.Dispose();

                _heartbeatTimer.Stop();
                _heartbeatTimer.Dispose();

                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
                _libVLC.Dispose();
                DatabaseManager.Instance.LogSystem("INFO", "Plaka tanıma ekranı kapatıldı", "PlateRecognitionForm.MainForm_FormClosing");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Form kapatma hatası", "PlateRecognitionForm.MainForm_FormClosing", ex.ToString());
            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            try
            {
                var testForm = new TestForm();
                testForm.Show();
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Test formu açma hatası", "PlateRecognitionForm.btnTest_Click", ex.ToString());
            }
        }

        private void CheckStreamHealth()
        {
            try
            {
                var secondsSinceLastFrame = (DateTime.Now - _lastVideoUpdateTime).TotalSeconds;

                if (secondsSinceLastFrame > 3) // 3 saniye boyunca yeni frame gelmediyse
                {
                    DatabaseManager.Instance.LogSystem("WARNING", $"Frame akışı {secondsSinceLastFrame:F1} sn durdu. RTSP yeniden başlatılıyor...", "PlateRecognitionForm.CheckStreamHealth");
                    try
                    {
                        _mediaPlayer.Stop();
                        _mediaPlayer.Play(new Media(_libVLC, _rtspUrl, FromType.FromLocation));
                        DatabaseManager.Instance.LogSystem("INFO", "Medya oynatıcısı yeniden başlatıldı", "PlateRecognitionForm.CheckStreamHealth");
                        _lastVideoUpdateTime = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        DatabaseManager.Instance.LogSystem("ERROR", "Yeniden bağlantı hatası", "PlateRecognitionForm.CheckStreamHealth", ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Sağlık kontrolü hatası", "PlateRecognitionForm.CheckStreamHealth", ex.ToString());
            }
        }

        private void UpdateStatus(string status, string plate = null)
        {
            try
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
            catch (Exception ex)
            {
               // Loglama burada recursion yaratabilir mi? Basit UI update hatası.
               // Yine de loglayalım ama dikkatli olalım.
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            try
            {
                // PlateRecognitionForm'u kapatma, sadece SplashForm'u göster
                // PlateRecognitionForm arka planda çalışmaya devam edecek
                var splashForm = Application.OpenForms["SplashForm"];
                if (splashForm != null)
                {
                    splashForm.Show();
                    splashForm.BringToFront();
                }
                else
                {
                    // SplashForm bulunamazsa yeni bir tane oluştur
                    var newSplashForm = new SplashForm();
                    newSplashForm.Show();
                }
                
                DatabaseManager.Instance.LogSystem("INFO", "Ana sayfaya dönüldü (PlateRecognitionForm arka planda çalışıyor)", "PlateRecognitionForm.btnBack_Click");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Geri dönme hatası", "PlateRecognitionForm.btnBack_Click", ex.ToString());
            }
        }
    }
}
