using LibVLCSharp.Shared;
using System;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using WinForms_RTSP_Player.Business;
using WinForms_RTSP_Player.Data;

namespace WinForms_RTSP_Player
{
    public partial class PlateRecognitionForm : Form
    {
        // Camera workers
        private CameraWorker _cameraWorkerIN;
        private CameraWorker _cameraWorkerOUT; // Optional

        // UI management
        private System.Windows.Forms.Timer _uiResetTimer;

        // Database manager
        private DatabaseManager _databaseManager;

        public PlateRecognitionForm()
        {
            try
            {
                _uiResetTimer = new System.Windows.Forms.Timer();
                _uiResetTimer.Tick += (s, e) => ResetUI();

                InitializeComponent();
                Core.Initialize(@"libvlc\win-x64");

                // Veri tabanı yöneticisini başlat
                _databaseManager = DatabaseManager.Instance;

                // IN kamerasını başlat (zorunlu)
                string rtspUrlIN = ConfigurationManager.AppSettings["RtspUrl_IN"];
                if (!string.IsNullOrEmpty(rtspUrlIN))
                {
                    _cameraWorkerIN = new CameraWorker("CAM_IN", rtspUrlIN, "IN", videoView1);
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

                // OUT kamerasını başlat (opsiyonel)
                string rtspUrlOUT = ConfigurationManager.AppSettings["RtspUrl_OUT"];
                if (!string.IsNullOrEmpty(rtspUrlOUT))
                {
                    _cameraWorkerOUT = new CameraWorker("CAM_OUT", rtspUrlOUT, "OUT", null!);
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
                        decision.Confidence
                    );
                }

                // Kapı kontrolü (sadece IN ve Allow için)
                if (decision.Direction == "IN" && decision.Action == AccessAction.Allow)
                {
                    // TODO: Kapı açma hardware kontrolü buraya eklenecek
                    DatabaseManager.Instance.LogSystem("INFO",
                        $"Giriş İzni Verildi: {decision.Plate}",
                        "Gate_Open");
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
            // Önce çalışan bir temizleme zamanlayıcısı varsa durdur
            _uiResetTimer.Stop();

            lblResult.Text = $"Tespit Edilen Plaka: {decision.Plate} ({decision.Direction})";
            lblResult.ForeColor = decision.IsAuthorized ? Color.FromArgb(0, 200, 83) : Color.FromArgb(244, 67, 54);
            lblStatus.Text = decision.IsAuthorized ? "✅ İZİNLİ" : "❌ İZİNSİZ";
            lblStatus.ForeColor = decision.IsAuthorized ? Color.FromArgb(0, 200, 83) : Color.FromArgb(244, 67, 54);

            // Süre kuralını uygula
            // İzinli ise 45 saniye (45000 ms), İzinsiz ise 10 saniye (10000 ms) sonra temizle
            _uiResetTimer.Interval = decision.IsAuthorized ? 45000 : 10000;
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
