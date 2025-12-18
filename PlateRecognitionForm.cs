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

        private string _lastProcessedPlate = "";
        private DateTime _lastGateTriggerTime = DateTime.MinValue;

        private System.Windows.Forms.Timer _uiResetTimer;
        private bool _gateOpenedByAuthorizedPlate = false;

        private string _lastUnauthorizedPlate = "";
        private DateTime _lastUnauthorizedLogTime = DateTime.MinValue;

        private const int UNAUTHORIZED_COOLDOWN_SECONDS = 60;



        public PlateRecognitionForm()
        {
            try
            {
                _uiResetTimer = new System.Windows.Forms.Timer();
                _uiResetTimer.Tick += (s, e) => ResetUI(); // Timer dolunca ResetUI metodunu çalıştır

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
                    // 100ms çok sınır bir değerdir, 300ms yaparak ağdaki anlık dalgalanmaları tolere edin.
                    "--network-caching=300",
                    "--no-video-title-show",
                    "--no-osd",
                    "--no-snapshot-preview",
                    //"--avcodec-hw=dxva2",
                    "--avcodec-hw=none",
                    // clock-synchro=1 ve jitter=0 ayarları VLC'yi çok katı olmaya zorlar. 
                    // Bu da ağdaki 1ms'lik gecikmede bile hata basmasına neden olur.
                    "--clock-jitter=500",  // Jitter toleransını artırın
                    "--clock-synchro=0",    // Senkronizasyon kontrolünü VLC'nin esnek yönetimine bırakın
                    
                    // Gecikme birikmesini önlemek için:
                    "--drop-late-frames",
                    "--skip-frames"
                };


                ////Eski kamera ayarları -silme-:
                //var libvlcOptions = new[]
                //{
                //    "--network-caching=350", // 50ms çok düşük, eski kameralar için 300 - 500 ms yapın
                //    "--rtsp-tcp",             // UDP paket kaybını önlemek için TCP üzerinden bağlanmaya zorlayın
                //    "--avcodec-hw=none",      // Eski kameralarda donanım hızlandırma bazen çakışır, önce devre dışı deneyin
                //    "--no-video-title-show",
                //    "--no-snapshot-preview",  // Snapshot önizlemesini kapatır
                //    "--no-osd",
                //    "--clock-jitter=500",     // Zaman sapmalarını tolere etmesi için artırın
                //    "--clock-synchro=0",      // Senkronizasyonu biraz gevşetin
                //    "--drop-late-frames",     // Geç gelen kareleri beklemek yerine atmaya devam etsin ama donmasın
                //    "--skip-frames"           // Kare atlamaya izin ver
                //};

                _libVLC = new LibVLC(libvlcOptions);
                _mediaPlayer = new MediaPlayer(_libVLC);
                _mediaPlayer.Mute = true;
                videoView1.MediaPlayer = _mediaPlayer;

                // Video frame geldiğinde zaman damgasını güncelle
                _mediaPlayer.TimeChanged += (s, e) =>
                {
                    _lastVideoUpdateTime = DateTime.Now;
                };

                _frameCaptureTimer = new System.Windows.Forms.Timer { Interval = 1000 };
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

        private async void FrameCaptureTimer_Tick(object sender, EventArgs e)
        {
            // 1. Kural: Timer'ı durdur
            _frameCaptureTimer.Stop();

            try
            {
                // ---- GATE LOCK WINDOW ----
                // SADECE yetkili araç kapıyı açtıysa ve 45 sn dolmadıysa
                if (IsGateLockActive())
                {
                    Console.WriteLine("GATE LOCK AKTİF → OCR tamamen pas geçildi");
                    return;
                }

                string tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp.jpg");

                if (_mediaPlayer.TakeSnapshot(0, tempPath, 0, 0))
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            if (!File.Exists(tempPath)) return;

                            string result = PlateRecognitionHelper.RunOpenALPR(tempPath);
                            PlateResult plateResult = PlateRecognitionHelper.ExtractPlateFromJson(result);

                            if (plateResult != null &&
                                !string.IsNullOrEmpty(plateResult.Plate) &&
                                plateResult.Plate.Length >= 7)
                            {
                                string correctedPlate =
                                    PlateSanitizer.ValidateTurkishPlateFormat(plateResult.Plate);

                                bool isAuthorized =
                                    _databaseManager.IsPlateAuthorized(correctedPlate);

                                float confidenceThreshold = isAuthorized ? 70f : 75f;

                                Console.WriteLine(
                                    plateResult.Plate + " --- " + plateResult.Confidence
                                );

                                if (plateResult.Confidence >= confidenceThreshold)
                                {
                                    bool isSameAsLast =
                                        (correctedPlate == _lastProcessedPlate);

                                    double secondsSinceLastAction =
                                        (DateTime.Now - _lastGateTriggerTime).TotalSeconds;

                                    // -------- YETKİLİ ARAÇ --------
                                    if (isAuthorized)
                                    {
                                        if (isSameAsLast && secondsSinceLastAction < 45)
                                        {
                                            Console.WriteLine(
                                                "araç İZİNLİ ve AYNI plaka 45 saniye dolmadı kapı zaten açık"
                                            );
                                            return;
                                        }
                                    }
                                    // -------- İZİNSİZ ARAÇ --------
                                    else
                                    {
                                        if (IsUnauthorizedCooldownActive(correctedPlate))
                                        {
                                            Console.WriteLine(
                                                "Kayıtsız AYNI Araç → 60 sn cooldown aktif, LOG ATLANIYOR     " +
                                                correctedPlate
                                            );
                                            return;
                                        }

                                        Console.WriteLine(
                                            "Kayıtsız YENİ Araç LOG ATILIYOR     " +
                                            correctedPlate
                                        );

                                        _databaseManager.LogAccess(
                                            correctedPlate,
                                            "Yabancı/Tanımsız",
                                            "IN",
                                            false,
                                            plateResult.Confidence
                                        );

                                        // ---- UNAUTHORIZED COOLDOWN STATE ----
                                        _lastUnauthorizedPlate = correctedPlate;
                                        _lastUnauthorizedLogTime = DateTime.Now;
                                    }

                                    // --- UI Güncelle ---
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        UpdateUIResult(correctedPlate, isAuthorized);
                                    }));

                                    // ---- ORTAK TAKİP ----
                                    _lastProcessedPlate = correctedPlate;

                                    // ---- SADECE YETKİLİ ARAÇ KAPI AÇAR ----
                                    if (isAuthorized)
                                    {
                                        _lastGateTriggerTime = DateTime.Now;
                                        _gateOpenedByAuthorizedPlate = true;

                                        Console.WriteLine(
                                            "Kapı Açılıyooooooooooooooooooooor       --------" +
                                            correctedPlate + "  --- " + DateTime.Now
                                        );

                                        string plateOwner =
                                            _databaseManager.GetPlateOwner(correctedPlate);

                                        _databaseManager.LogAccess(
                                            correctedPlate,
                                            plateOwner,
                                            "IN",
                                            true,
                                            plateResult.Confidence
                                        );

                                        DatabaseManager.Instance.LogSystem(
                                            "INFO",
                                            $"Giriş İzni Verildi: {correctedPlate}",
                                            "Gate_Open"
                                        );
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (File.Exists(tempPath))
                                File.Delete(tempPath);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem(
                    "ERROR",
                    "OCR hatası",
                    "FrameCaptureTimer_Tick",
                    ex.ToString()
                );
            }
            finally
            {
                // 2. Kural: Timer'ı tekrar başlat
                _frameCaptureTimer.Start();
            }
        }



        // Yardımcı UI metodu (kodun okunabilirliği için)
        private void UpdateUIResult(string plate, bool authorized)
        {
            // Önce çalışan bir temizleme zamanlayıcısı varsa durdur
            _uiResetTimer.Stop();

            lblResult.Text = $"Tespit Edilen Plaka: {plate}";
            lblResult.ForeColor = authorized ? Color.FromArgb(0, 200, 83) : Color.FromArgb(244, 67, 54);
            lblStatus.Text = authorized ? "✅ İZİNLİ" : "❌ İZİNSİZ";
            lblStatus.ForeColor = authorized ? Color.FromArgb(0, 200, 83) : Color.FromArgb(244, 67, 54);

            // Süre kuralını uygula
            // İzinli ise 45 saniye (45000 ms), İzinsiz ise 10 saniye (10000 ms) sonra temizle
            _uiResetTimer.Interval = authorized ? 45000 : 10000;
            _uiResetTimer.Start();
        }

        private void ResetUI()
        {
            _uiResetTimer.Stop();
            lblResult.Text = "Tespit Edilen Plaka: ---";
            lblResult.ForeColor = Color.Silver;
            lblStatus.Text = "Sistem Durumu: Bekleniyor...";
            lblStatus.ForeColor = Color.Silver;
        }

        private bool IsGateLockActive()
        {
            if (!_gateOpenedByAuthorizedPlate)
                return false;

            double secondsSinceGateOpened =
                (DateTime.Now - _lastGateTriggerTime).TotalSeconds;

            if (secondsSinceGateOpened >= 45)
            {
                _gateOpenedByAuthorizedPlate = false;
                return false;
            }

            return true;
        }

        private bool IsUnauthorizedCooldownActive(string plate)
        {
            if (plate != _lastUnauthorizedPlate)
                return false;

            double seconds =
                (DateTime.Now - _lastUnauthorizedLogTime).TotalSeconds;

            return seconds < UNAUTHORIZED_COOLDOWN_SECONDS;
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
