using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinForms_RTSP_Player.Data;
using WinForms_RTSP_Player.Utilities;
using static WinForms_RTSP_Player.Utilities.PlateRecognitionHelper;

namespace WinForms_RTSP_Player.Business
{
    /// <summary>
    /// Bağımsız kamera işleyicisi - Her kamera kendi yaşam döngüsüne sahip
    /// </summary>
    public class CameraWorker : IDisposable
    {
        // Public properties
        public string CameraId { get; private set; }
        public string Direction { get; private set; }
        public bool IsRunning { get; private set; }

        // Events
        public event EventHandler<PlateDetectedEventArgs> PlateDetected;

        // VLC components
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private readonly VideoView _videoView; // Nullable - OUT kamerasında olmayabilir

        // Timers
        private System.Windows.Forms.Timer _frameCaptureTimer;
        private System.Windows.Forms.Timer _streamHealthTimer;
        private System.Windows.Forms.Timer _heartbeatTimer;
        private System.Windows.Forms.Timer _periodicResetTimer;

        // State tracking
        private int _framesSinceLastReset = 0;
        private DateTime _lastVideoUpdateTime;
        private readonly string _rtspUrl;
        private bool _disposed = false;

        public CameraWorker(string cameraId, string rtspUrl, string direction, VideoView videoView = null)
        {
            CameraId = cameraId ?? throw new ArgumentNullException(nameof(cameraId));
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
            _rtspUrl = rtspUrl ?? throw new ArgumentNullException(nameof(rtspUrl));
            _videoView = videoView;

            DatabaseManager.Instance.LogSystem("INFO", 
                $"CameraWorker oluşturuldu: {CameraId} ({Direction})", 
                $"CameraWorker.{CameraId}.Constructor");
        }

        /// <summary>
        /// Kamerayı başlat
        /// </summary>
        public void Start()
        {
            if (IsRunning)
            {
                DatabaseManager.Instance.LogSystem("WARNING", 
                    $"Kamera zaten çalışıyor: {CameraId}", 
                    $"CameraWorker.{CameraId}.Start");
                return;
            }

            try
            {
                // VLC başlat
                InitializeVLC();

                // Timers başlat
                InitializeTimers();

                // RTSP stream başlat
                _mediaPlayer.Play(new Media(_libVLC, _rtspUrl, FromType.FromLocation));
                _lastVideoUpdateTime = DateTime.Now;

                IsRunning = true;

                // Plaka okuma timer'ını 3 saniye gecikmeli başlat (Isınma süresi - Hataları önlemek için)
                Task.Delay(3000).ContinueWith(t => {
                    if (IsRunning && _frameCaptureTimer != null && _videoView != null) {
                        _videoView.BeginInvoke(new Action(() => {
                            if (_frameCaptureTimer != null) {
                                _frameCaptureTimer.Start();
                                Console.WriteLine($"[INFO] Kamera ısınma süresi tamamlandı. Plaka okuma aktif: {CameraId}");
                            }
                        }));
                    }
                });

                DatabaseManager.Instance.LogSystem("INFO", 
                    $"Kamera başlatıldı: {CameraId} ({Direction}) - {_rtspUrl}", 
                    $"CameraWorker.{CameraId}.Start");
            }
            catch (Exception ex)
            {
                IsRunning = false;
                DatabaseManager.Instance.LogSystem("ERROR", 
                    $"Kamera başlatma hatası: {CameraId}", 
                    $"CameraWorker.{CameraId}.Start", 
                    ex.ToString());
            }
        }

        /// <summary>
        /// Kamerayı durdur
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
                return;

            try
            {
                // UI bağlantısını kes (D3D11 hatalarını önlemek için en kritik adım)
                if (_videoView != null && _videoView.InvokeRequired)
                {
                    _videoView.Invoke(new Action(() => { _videoView.MediaPlayer = null; }));
                }
                else if (_videoView != null)
                {
                    _videoView.MediaPlayer = null;
                }

                _frameCaptureTimer?.Stop();
                _streamHealthTimer?.Stop();
                _heartbeatTimer?.Stop();
                _periodicResetTimer?.Stop();

                _mediaPlayer?.Stop();
                
                // Kaynakları tamamen temizle (Memory leak ve D3D11 çakışmalarını önlemek için)
                _mediaPlayer?.Dispose();
                _libVLC?.Dispose();
                _mediaPlayer = null;
                _libVLC = null;

                IsRunning = false;

                DatabaseManager.Instance.LogSystem("INFO", 
                    $"Kamera durduruldu: {CameraId}", 
                    $"CameraWorker.{CameraId}.Stop");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    $"Kamera durdurma hatası: {CameraId}", 
                    $"CameraWorker.{CameraId}.Stop", 
                    ex.ToString());
            }
        }

        private void InitializeVLC()
        {
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

            // Temizlik yap (leak önleme)
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();

            _libVLC = new LibVLC(libvlcOptions);
            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.Mute = true;

            // VideoView varsa bağla (IN kamerasında var, OUT'ta yok)
            if (_videoView != null)
            {
                _videoView.MediaPlayer = _mediaPlayer;
            }

            // Video frame geldiğinde zaman damgasını güncelle
            _mediaPlayer.TimeChanged += (s, e) =>
            {
                _lastVideoUpdateTime = DateTime.Now;
            };

            DatabaseManager.Instance.LogSystem("INFO", 
                $"VLC başlatıldı: {CameraId}", 
                $"CameraWorker.{CameraId}.InitializeVLC");
        }

        private void InitializeTimers()
        {
            // Eski timerları temizle
            _frameCaptureTimer?.Dispose();
            _streamHealthTimer?.Dispose();
            _heartbeatTimer?.Dispose();
            // Not: _periodicResetTimer reset sırasında yenilenir, disposal Start() içinde yapılabilir
            // Ancak InitializeTimers her Start() çağrıldığında tetiklenir.
            
            // Frame capture timer (1 saniye) - Hemen başlatmıyoruz, 3 sn ısınma süresi vereceğiz
            _frameCaptureTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _frameCaptureTimer.Tick += FrameCaptureTimer_Tick;
            // _frameCaptureTimer.Start(); // Start() metodunda gecikmeli başlatılacak

            // Stream health timer (30 saniye) - Daha sık kontrol
            _streamHealthTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            _streamHealthTimer.Tick += (s, e) => CheckStreamHealth();
            _streamHealthTimer.Start();

            // Heartbeat timer (5 dakika) - Akıllı heartbeat
            if (_heartbeatTimer != null) _heartbeatTimer.Dispose();
            _heartbeatTimer = new System.Windows.Forms.Timer();
            _heartbeatTimer.Interval = 300000; // 5 dakika (300.000 ms)
            _heartbeatTimer.Tick += HeartbeatTimer_Tick;
            _heartbeatTimer.Start();

            // Periyodik Reset Timer (Sadece ilk kez oluşturulur, her Start'ta kontrol edilip başlatılır)
            if (_periodicResetTimer == null)
            {
                _periodicResetTimer = new System.Windows.Forms.Timer();
                _periodicResetTimer.Interval = 600000; // 10 dakika (600.000 ms)
                _periodicResetTimer.Tick += (s, e) => Restart();
            }
            
            if (!_periodicResetTimer.Enabled)
                _periodicResetTimer.Start();

            DatabaseManager.Instance.LogSystem("INFO", 
                $"Timers başlatıldı: {CameraId}", 
                $"CameraWorker.{CameraId}.InitializeTimers");
        }

        public void Restart()
        {
            Console.WriteLine($"[RESET] Kamera periyodik olarak yeniden başlatılıyor: {CameraId} ({DateTime.Now})");
            
            DatabaseManager.Instance.LogSystem("INFO", 
                $"Kamera periyodik olarak resetleniyor (Gecikme önleme)", 
                $"CameraWorker.{CameraId}.Restart");

            Stop();

            // VLC'nin arka planda kaynakları serbest bırakması için kısa bir bekleme
            System.Threading.Thread.Sleep(2000); 

            Start();
        }

        private async void FrameCaptureTimer_Tick(object sender, EventArgs e)
        {
            // Timer'ı durdur (re-entrancy önleme)
            _frameCaptureTimer.Stop();

            try
            {
                if (!IsRunning || _mediaPlayer == null)
                    return;

                var state = _mediaPlayer.State;
                if (state != VLCState.Playing)
                {
                    return;
                }

                string tempPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, 
                    $"temp_{CameraId}.jpg"
                );

                if (_mediaPlayer.TakeSnapshot(0, tempPath, 0, 0))
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            if (!File.Exists(tempPath))
                                return;

                            // OCR işlemi
                            string result = PlateRecognitionHelper.RunOpenALPR(tempPath);
                            PlateResult plateResult = PlateRecognitionHelper.ExtractPlateFromJson(result);

                            if (plateResult != null &&
                                !string.IsNullOrEmpty(plateResult.Plate) &&
                                plateResult.Plate.Length >= 7)
                            {
                                // Event fırlat
                                OnPlateDetected(new PlateDetectedEventArgs
                                {
                                    CameraId = this.CameraId,
                                    Direction = this.Direction,
                                    Plate = plateResult.Plate,
                                    Confidence = plateResult.Confidence,
                                    DetectedAt = DateTime.Now
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            DatabaseManager.Instance.LogSystem("ERROR", 
                                $"OCR hatası: {CameraId}", 
                                $"CameraWorker.{CameraId}.FrameCaptureTimer_Tick", 
                                ex.ToString());
                        }
                        finally
                        {
                            if (File.Exists(tempPath))
                                File.Delete(tempPath);
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"[WARNING] Snapshot ALINAMADI: {CameraId} (VLC State: {_mediaPlayer.State})");
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    $"Frame capture hatası: {CameraId}", 
                    $"CameraWorker.{CameraId}.FrameCaptureTimer_Tick", 
                    ex.ToString());
            }
            finally
            {
                // Timer'ı tekrar başlat
                if (IsRunning)
                    _frameCaptureTimer?.Start();
            }
        }

        private void CheckStreamHealth()
        {
            try
            {
                if (_mediaPlayer == null || !IsRunning)
                    return;

                // 1. MediaPlayer state kontrolü
                var state = _mediaPlayer.State;
                
                // Eğer MediaPlayer durmuş veya hata durumundaysa
                if (state == VLCState.Stopped || state == VLCState.Error || state == VLCState.Ended)
                {
                    DatabaseManager.Instance.LogSystem("WARNING", 
                        $"Kamera bağlantısı kesildi (State: {state}). Yeniden bağlanılıyor: {CameraId}", 
                        $"CameraWorker.{CameraId}.CheckStreamHealth");

                    AttemptReconnect();
                    return;
                }

                // 2. Frame akışı kontrolü
                var secondsSinceLastFrame = (DateTime.Now - _lastVideoUpdateTime).TotalSeconds;

                if (secondsSinceLastFrame > 10) // 10 saniye boyunca yeni frame gelmediyse
                {
                    DatabaseManager.Instance.LogSystem("WARNING", 
                        $"Frame akışı {secondsSinceLastFrame:F1} sn durdu (State: {state}). Yeniden bağlanılıyor: {CameraId}", 
                        $"CameraWorker.{CameraId}.CheckStreamHealth");

                    AttemptReconnect();
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    $"Sağlık kontrolü hatası: {CameraId}", 
                    $"CameraWorker.{CameraId}.CheckStreamHealth", 
                    ex.ToString());
            }
        }

        private void AttemptReconnect()
        {
            try
            {
                _mediaPlayer.Stop();
                System.Threading.Thread.Sleep(500); // Kısa bir bekleme
                _mediaPlayer.Play(new Media(_libVLC, _rtspUrl, FromType.FromLocation));
                _lastVideoUpdateTime = DateTime.Now;

                DatabaseManager.Instance.LogSystem("INFO", 
                    $"RTSP yeniden başlatıldı: {CameraId}", 
                    $"CameraWorker.{CameraId}.AttemptReconnect");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    $"Yeniden bağlantı hatası: {CameraId}", 
                    $"CameraWorker.{CameraId}.AttemptReconnect", 
                    ex.ToString());
            }
        }

        private void HeartbeatTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_mediaPlayer == null)
                {
                    DatabaseManager.Instance.LogSystem("ERROR", 
                        $"Heartbeat: {CameraId} - MediaPlayer NULL!", 
                        $"CameraWorker.{CameraId}.Heartbeat");
                    return;
                }

                var state = _mediaPlayer.State;
                var secondsSinceLastFrame = (DateTime.Now - _lastVideoUpdateTime).TotalSeconds;
                
                string status = state == VLCState.Playing ? "✅ ÇALIŞIYOR" : $"❌ SORUN ({state})";
                string frameInfo = secondsSinceLastFrame < 10 ? 
                    $"Frame: {secondsSinceLastFrame:F1}s önce" : 
                    $"⚠️ Frame: {secondsSinceLastFrame:F1}s önce (DONMUŞ)";

                DatabaseManager.Instance.LogSystem("INFO", 
                    $"Heartbeat: {CameraId} ({Direction}) - {status} - {frameInfo}", 
                    $"CameraWorker.{CameraId}.Heartbeat");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    $"Heartbeat hatası: {CameraId}", 
                    $"CameraWorker.{CameraId}.Heartbeat", 
                    ex.ToString());
            }
        }

        protected virtual void OnPlateDetected(PlateDetectedEventArgs e)
        {
            PlateDetected?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                Stop();

                _frameCaptureTimer?.Dispose();
                _streamHealthTimer?.Dispose();
                _heartbeatTimer?.Dispose();
                _periodicResetTimer?.Dispose();

                _mediaPlayer?.Dispose();
                _libVLC?.Dispose();

                _disposed = true;

                DatabaseManager.Instance.LogSystem("INFO", 
                    $"CameraWorker dispose edildi: {CameraId}", 
                    $"CameraWorker.{CameraId}.Dispose");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    $"Dispose hatası: {CameraId}", 
                    $"CameraWorker.{CameraId}.Dispose", 
                    ex.ToString());
            }
        }
    }

    /// <summary>
    /// Plaka tespit event arguments
    /// </summary>
    public class PlateDetectedEventArgs : EventArgs
    {
        public string CameraId { get; set; }
        public string Direction { get; set; }
        public string Plate { get; set; }
        public double Confidence { get; set; }
        public DateTime DetectedAt { get; set; }
    }
}
