using LibVLCSharp.Shared;
using System;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using WinForms_RTSP_Player.Business;
using WinForms_RTSP_Player.Data;
using WinForms_RTSP_Player.Utilities;

namespace WinForms_RTSP_Player
{
    public partial class PlateRecognitionForm : Form
    {
        // Camera workers
        private CameraWorker _cameraWorkerIN;
        private CameraWorker _cameraWorkerOUT;

        // UI management
        private System.Windows.Forms.Timer _uiResetTimerIN;
        private System.Windows.Forms.Timer _uiResetTimerOUT;

        // Maintenance mode (Gece bakım modu - 03:00)
        private System.Threading.Timer _maintenanceTimer;

        // Database manager
        private DatabaseManager _databaseManager;

        public PlateRecognitionForm()
        {
            try
            {
                // IN Timer
                _uiResetTimerIN = new System.Windows.Forms.Timer();
                _uiResetTimerIN.Tick += (s, e) => ResetUI("IN");

                // OUT Timer
                _uiResetTimerOUT = new System.Windows.Forms.Timer();
                _uiResetTimerOUT.Tick += (s, e) => ResetUI("OUT");

                InitializeComponent();
                Core.Initialize(@"libvlc\win-x64");

                // Veri tabanı yöneticisini başlat
                _databaseManager = DatabaseManager.Instance;

                // IN kamerasını başlat (videoViewIN eklendi)
                string rtspUrlIN = ConfigurationManager.AppSettings["RtspUrl_IN"];
                if (!string.IsNullOrEmpty(rtspUrlIN))
                {
                    _cameraWorkerIN = new CameraWorker("CAM_IN", rtspUrlIN, "IN", videoViewIN);
                    _cameraWorkerIN.PlateDetected += OnPlateDetected;
                    DatabaseManager.Instance.LogSystem("INFO", 
                        "IN kamerası yapılandırıldı", 
                        "PlateRecognitionForm.Constructor");
                }
                else
                {
                    MessageBox.Show("RTSP bağlantı adresi (RtspUrl_IN) App.config dosyasında bulunamadı!");
                    DatabaseManager.Instance.LogSystem("ERROR", 
                        "RtspUrl_IN bulunamadı", 
                        "PlateRecognitionForm.Constructor");
                    return;
                }

                // OUT kamerasını başlat (videoViewOUT eklendi)
                string rtspUrlOUT = ConfigurationManager.AppSettings["RtspUrl_OUT"];
                if (!string.IsNullOrEmpty(rtspUrlOUT))
                {
                    _cameraWorkerOUT = new CameraWorker("CAM_OUT", rtspUrlOUT, "OUT", videoViewOUT);
                    _cameraWorkerOUT.PlateDetected += OnPlateDetected;
                    DatabaseManager.Instance.LogSystem("INFO", 
                        "OUT kamerası yapılandırıldı", 
                        "PlateRecognitionForm.Constructor");
                }
                else
                {
                    DatabaseManager.Instance.LogSystem("INFO", 
                        "RtspUrl_OUT bulunamadı - Tek kamera modunda çalışılıyor", 
                        "PlateRecognitionForm.Constructor");
                }

                DatabaseManager.Instance.LogSystem("INFO", 
                    "Plaka tanıma formu başlatıldı", 
                    "PlateRecognitionForm.Constructor");

                // Donanım kontrolcüsünü başlat
                string arduinoPort = ConfigurationManager.AppSettings["ArduinoPort"];
                int baudRate = int.Parse(ConfigurationManager.AppSettings["ArduinoBaudRate"] ?? "9600");
                if (!string.IsNullOrEmpty(arduinoPort))
                {
                    HardwareController.Instance.Initialize(arduinoPort, baudRate);
                }

                // Gece bakım modunu başlat
                InitializeMaintenanceMode();
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    "Plaka tanıma formu başlatma hatası", 
                    "PlateRecognitionForm.Constructor", 
                    ex.ToString());
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                _cameraWorkerIN?.Start();
                _cameraWorkerOUT?.Start();

                DatabaseManager.Instance.LogSystem("INFO", 
                    "Kamera sistemleri başlatıldı", 
                    "PlateRecognitionForm.btnStart_Click");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    "Kamera başlatma hatası", 
                    "PlateRecognitionForm.btnStart_Click", 
                    ex.ToString());
            }
        }


        private void OnPlateDetected(object sender, PlateDetectedEventArgs e)
        {
            try
            {
                // AccessDecisionManager'dan karar al
                var decision = AccessDecisionManager.Instance.ProcessPlateDetection(
                    e.Plate, 
                    e.Direction, 
                    e.Confidence
                );

                // Ignore ise hiçbir şey yapma
                if (decision.Action == AccessAction.Ignore)
                    return;

                // UI güncelle
                BeginInvoke(new Action(() => UpdateUIResult(decision)));

                // Database'e log at (Allow veya Deny için)
                if (decision.Action == AccessAction.Allow || decision.Action == AccessAction.Deny)
                {
                    _databaseManager.LogAccess(
                        decision.Plate,
                        decision.Owner,
                        decision.Direction,
                        decision.IsAuthorized,
                        decision.Confidence,
                        $"ID:{decision.GateOpId}"
                    );
                }

                // Kapı kontrolü (Sadece Allow aksiyonu için)
                if (decision.Action == AccessAction.Allow)
                {
                    string ocrInfo = decision.OcrPlate != decision.Plate ? $"OCR:{decision.OcrPlate}" : "";
                    string logMessage = decision.Direction == "IN" 
                        ? $"Giriş İzni Verildi | {decision.Plate} | {decision.Owner} | {ocrInfo}" 
                        : $"Çıkış İzni Verildi | {decision.Plate} | {decision.Owner} | {ocrInfo}";

                    DatabaseManager.Instance.LogSystem("INFO",
                        logMessage,
                        "Gate_Open",
                        $"ID:{decision.GateOpId}");

                    // Arduino'ya kapı açma komutu gönder
                    _ = HardwareController.Instance.OpenGateAsync();
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR",
                    $"Plaka tespit event hatası: {e.CameraId}",
                    "PlateRecognitionForm.OnPlateDetected",
                    ex.ToString());
            }
        }



        // Yardımcı UI metodu (kodun okunabilirliği için)
        private void UpdateUIResult(AccessDecision decision)
        {
            bool isIN = (decision.Direction == "IN");
            
            // İlgili kontrolleri seç
            Label lblResult = isIN ? lblResultIN : lblResultOUT;
            Label lblStatus = isIN ? lblStatusIN : lblStatusOUT;
            System.Windows.Forms.Timer timer = isIN ? _uiResetTimerIN : _uiResetTimerOUT;

            // Önce çalışan bir temizleme zamanlayıcısı varsa durdur
            timer.Stop();

            lblResult.Text = $"Tespit Edilen Plaka: {decision.Plate}";
            lblResult.ForeColor = decision.IsAuthorized ? Color.FromArgb(0, 200, 83) : Color.FromArgb(244, 67, 54);
            
            lblStatus.Text = decision.IsAuthorized ? "✅ KAYITLI" : "❌ KAYITSIZ";
            lblStatus.ForeColor = decision.IsAuthorized ? Color.FromArgb(0, 200, 83) : Color.FromArgb(244, 67, 54);

            // Süre kuralını uygula
            // İzinli ise AuthorizedPlateShowTime, İzinsiz ise UnAuthorizedPlateShowTime sonra temizle
            timer.Interval = decision.IsAuthorized 
                ? Utilities.SystemParameters.AuthorizedPlateShowTime 
                : Utilities.SystemParameters.UnAuthorizedPlateShowTime;
            timer.Start();
        }

        private void ResetUI(string direction)
        {
            if (direction == "IN")
            {
                _uiResetTimerIN.Stop();
                lblResultIN.Text = "Tespit Edilen Plaka: ---";
                lblResultIN.ForeColor = Color.Silver;
                lblStatusIN.Text = "Sistem Durumu: Bekleniyor...";
                lblStatusIN.ForeColor = Color.Silver;
            }
            else
            {
                _uiResetTimerOUT.Stop();
                lblResultOUT.Text = "Tespit Edilen Plaka: ---";
                lblResultOUT.ForeColor = Color.Silver;
                lblStatusOUT.Text = "Sistem Durumu: Bekleniyor...";
                lblStatusOUT.ForeColor = Color.Silver;
            }
        }




        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _cameraWorkerIN?.Stop();
                _cameraWorkerIN?.Dispose();

                _cameraWorkerOUT?.Stop();
                _cameraWorkerOUT?.Dispose();

                DatabaseManager.Instance.LogSystem("INFO", 
                    "Plaka tanıma ekranı kapatıldı", 
                    "PlateRecognitionForm.MainForm_FormClosing");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    "Form kapatma hatası", 
                    "PlateRecognitionForm.MainForm_FormClosing", 
                    ex.ToString());
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

        /// <summary>
        /// Gece bakım modunu başlatır (Her gece saat 03:00'da çalışır)
        /// </summary>
        private void InitializeMaintenanceMode()
        {
            try
            {
                var now = DateTime.Now;
                var scheduledTime = new DateTime(now.Year, now.Month, now.Day, 3, 0, 0);
                
                // Eğer bugünün 03:00'ı geçtiyse, yarının 03:00'ını ayarla
                if (now > scheduledTime)
                    scheduledTime = scheduledTime.AddDays(1);

                var timeUntil = scheduledTime - now;
                
                // Timer'ı ayarla (ilk çalışma zamanı ve 24 saat periyot)
                _maintenanceTimer = new System.Threading.Timer(
                    PerformMaintenanceCycle, 
                    null, 
                    (int)timeUntil.TotalMilliseconds, 
                    24 * 60 * 60 * 1000 // 24 saat
                );

                DatabaseManager.Instance.LogSystem("INFO", 
                    $"Gece bakım modu ayarlandı. İlk çalışma: {scheduledTime:yyyy-MM-dd HH:mm:ss}", 
                    "PlateRecognitionForm.InitializeMaintenanceMode");

#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [INFO] Gece bakım modu ayarlandı. İlk çalışma: {scheduledTime:yyyy-MM-dd HH:mm:ss}");
#endif
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    "Gece bakım modu başlatma hatası", 
                    "PlateRecognitionForm.InitializeMaintenanceMode", 
                    ex.ToString());

#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [ERROR] Gece bakım modu başlatma hatası: {ex.Message}");
#endif
            }
        }

        /// <summary>
        /// Gece bakım döngüsü - Kameraları durdur, GC yap, yeniden başlat
        /// </summary>
        private void PerformMaintenanceCycle(object state)
        {
            try
            {
                DatabaseManager.Instance.LogSystem("INFO", 
                    "Gece bakım modu başlatılıyor (03:00)", 
                    "PlateRecognitionForm.MaintenanceMode");

#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [MAINTENANCE] Gece bakım modu başlatılıyor (03:00)");
#endif

                // 1. Kameraları durdur
                _cameraWorkerIN?.Stop();
                _cameraWorkerOUT?.Stop();

                DatabaseManager.Instance.LogSystem("INFO", 
                    "Kameralar durduruldu (Bakım modu)", 
                    "PlateRecognitionForm.MaintenanceMode");

                System.Threading.Thread.Sleep(2000);

                // 2. Şimdi GC güvenli (Kameralar kapalı, OCR işlemi yok)
                DatabaseManager.Instance.LogSystem("INFO", 
                    "Bellek temizliği başlatılıyor (GC)", 
                    "PlateRecognitionForm.MaintenanceMode");

#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [MAINTENANCE] GC başlatılıyor...");
#endif

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                DatabaseManager.Instance.LogSystem("INFO", 
                    "Bellek temizliği tamamlandı", 
                    "PlateRecognitionForm.MaintenanceMode");

#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [MAINTENANCE] GC tamamlandı");
#endif

                System.Threading.Thread.Sleep(2000);

                // 3. Kameraları yeniden başlat
                _cameraWorkerIN?.Start();
                _cameraWorkerOUT?.Start();

                DatabaseManager.Instance.LogSystem("INFO", 
                    "Gece bakım modu tamamlandı - Kameralar yeniden başlatıldı", 
                    "PlateRecognitionForm.MaintenanceMode");

#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [MAINTENANCE] Gece bakım modu tamamlandı");
#endif
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    "Gece bakım modu hatası", 
                    "PlateRecognitionForm.MaintenanceMode", 
                    ex.ToString());

#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [ERROR] Gece bakım modu hatası: {ex.Message}");
#endif

                // Hata durumunda kameraları yeniden başlatmayı dene
                try
                {
                    _cameraWorkerIN?.Start();
                    _cameraWorkerOUT?.Start();
                }
                catch (Exception restartEx)
                {
                    DatabaseManager.Instance.LogSystem("ERROR", 
                        "Bakım modu sonrası kamera başlatma hatası", 
                        "PlateRecognitionForm.MaintenanceMode", 
                        restartEx.ToString());
                }
            }
        }
    }
}
