using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System;
using System.IO;
using System.Threading;
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

        // Timers - System.Threading.Timer for stability
        private System.Threading.Timer _frameCaptureTimer;
        private System.Threading.Timer _streamHealthTimer;
        private System.Threading.Timer _heartbeatTimer;
        private System.Threading.Timer _periodicResetTimer;

        // State tracking
        private DateTime _lastVideoUpdateTime;
        private readonly string _rtspUrl;
        private bool _disposed = false;
        
        // Granular Locks
        private readonly object _frameLock = new object();     // OCR + snapshot
        private readonly object _playerLock = new object();    // VLC Stop/Play
        private readonly object _resetLock = new object();     // Restart state
        
        private bool _isResetting = false;

        // Timer intervals (ms)
        private int _frameCaptureInterval;
        private int _streamHealthInterval;
        private int _heartbeatInterval;
        private int _periodicResetInterval;

        public CameraWorker(string cameraId, string rtspUrl, string direction, VideoView videoView = null)
        {
            CameraId = cameraId ?? throw new ArgumentNullException(nameof(cameraId));
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
            _rtspUrl = rtspUrl ?? throw new ArgumentNullException(nameof(rtspUrl));
            _videoView = videoView;

            // Load intervals
            _frameCaptureInterval = SystemParameters.FrameCaptureTimerInterval;
            _streamHealthInterval = SystemParameters.StreamHealthTimerInterval;
            _heartbeatInterval = SystemParameters.HeartbeatTimerInterval;
            _periodicResetInterval = SystemParameters.PeriodicResetTimerInterval;

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
                lock (_playerLock)
                {
                    _mediaPlayer.Play(new Media(_libVLC, _rtspUrl, FromType.FromLocation, ":avcodec-hw=none"));
                }
                _lastVideoUpdateTime = DateTime.Now;

                IsRunning = true;

                // Plaka okuma timer'ını 3 saniye gecikmeli başlat
                if (_frameCaptureTimer != null)
                {
                    _frameCaptureTimer.Change(3000, Timeout.Infinite); // One-shot başlat (Tick içinde tekrar kurulacak)
                    Console.WriteLine($"[{DateTime.Now}] [INFO] Kamera ısınma süresi (3sn) başladı. {CameraId}");
                }
                
                // Diğer timerları başlat
                _streamHealthTimer?.Change(_streamHealthInterval, _streamHealthInterval);
                _heartbeatTimer?.Change(_heartbeatInterval, _heartbeatInterval);
                _periodicResetTimer?.Change(_periodicResetInterval, _periodicResetInterval);

                DatabaseManager.Instance.LogSystem("INFO", 
                    $"Kamera başlatıldı: {CameraId} ({Direction})", 
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
                // Timerları durdur
                _frameCaptureTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _streamHealthTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _heartbeatTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _periodicResetTimer?.Change(Timeout.Infinite, Timeout.Infinite);

                // UI bağlantısını kes
                if (_videoView != null && !_videoView.IsDisposed)
                {
                    if (_videoView.InvokeRequired)
                    {
                        try {
                            _videoView.Invoke(new Action(() => { 
                                if (!_videoView.IsDisposed) _videoView.MediaPlayer = null; 
                            }));
                        } catch { /* Ignored */ }
                    }
                    else
                    {
                         _videoView.MediaPlayer = null;
                    }
                }

                lock (_playerLock)
                {
                    _mediaPlayer?.Stop();
                    _mediaPlayer?.Dispose();
                    _mediaPlayer = null;
                }

                _libVLC?.Dispose();
                _libVLC = null;

                IsRunning = false;

                // Agresif hafıza temizliği
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

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
                "--network-caching=1000",
                "--no-video-title-show",
                "--no-osd",
                "--no-snapshot-preview",
                "--vout=gdi", 
                "--avcodec-hw=none",
                "--clock-jitter=1000",
                "--clock-synchro=0",
                "--drop-late-frames",
                "--skip-frames"
            };

            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();

            _libVLC = new LibVLC(libvlcOptions);
            
            _libVLC.Log += (s, e) => 
            {
                if (e.Level == LogLevel.Error && 
                    !e.Message.Contains("SetThumbNailClip") && 
                    !e.Message.Contains("computer too slow"))
                {
                    string logMsg = $"[VLC_ERROR] {e.Message}";
                    Console.WriteLine($"[{DateTime.Now}] {logMsg}");
                    
                    DatabaseManager.Instance.LogSystem("ERROR", 
                        $"VLC Kritik Hata: {e.Message}", 
                        $"CameraWorker.{CameraId}.VLCInternal", 
                        e.Module);
                }
            };

            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.Mute = true;

            if (_videoView != null)
            {
                if (_videoView.InvokeRequired)
                {
                     _videoView.Invoke(new Action(() => _videoView.MediaPlayer = _mediaPlayer));
                }
                else
                {
                    _videoView.MediaPlayer = _mediaPlayer;
                }
            }

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
            _frameCaptureTimer?.Dispose();
            _streamHealthTimer?.Dispose();
            _heartbeatTimer?.Dispose();
            _periodicResetTimer?.Dispose();
            
            _frameCaptureTimer = new System.Threading.Timer(FrameCaptureTimer_Tick, null, Timeout.Infinite, Timeout.Infinite);
            _streamHealthTimer = new System.Threading.Timer(CheckStreamHealth, null, Timeout.Infinite, Timeout.Infinite);
            _heartbeatTimer = new System.Threading.Timer(HeartbeatTimer_Tick, null, Timeout.Infinite, Timeout.Infinite);
            _periodicResetTimer = new System.Threading.Timer(PeriodicResetTimer_Tick, null, Timeout.Infinite, Timeout.Infinite);

            DatabaseManager.Instance.LogSystem("INFO", 
                $"Threading Timers başlatıldı: {CameraId}", 
                $"CameraWorker.{CameraId}.InitializeTimers");
        }

        private void PeriodicResetTimer_Tick(object state)
        {
            Restart();
        }

        public void Restart()
        {
            lock (_resetLock)
            {
                if (_isResetting) return;
                _isResetting = true;
            }

            try
            {
                Console.WriteLine($"[{DateTime.Now}] [RESET] Kamera periyodik olarak yeniden başlatılıyor: {CameraId}");

                DatabaseManager.Instance.LogSystem("INFO",
                    $"Kamera periyodik olarak resetleniyor (Gecikme önleme)",
                    $"CameraWorker.{CameraId}.Restart");

                Stop();

                System.Threading.Thread.Sleep(3000);

                Start();
            }
            catch (Exception ex)
            {
                    DatabaseManager.Instance.LogSystem("ERROR",
                    $"Restart hatası: {CameraId}",
                    $"CameraWorker.{CameraId}.Restart",
                    ex.ToString());
            }
            finally
            {
                lock (_resetLock)
                {
                    _isResetting = false;
                }
            }
        }

        private void FrameCaptureTimer_Tick(object stateInfo)
        {
            // One-Shot Timer Pattern: Timer otomatik tekrar etmez, biz manuel kurarız.
            
            string tempPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                $"temp_{CameraId}_{DateTime.UtcNow.Ticks}.jpg"
            );
            bool snapshotTaken = false;

            bool lockTaken = false;
            try 
            {
                // LOCK SADECE SNAPSHOT İÇİN
                Monitor.TryEnter(_frameLock, ref lockTaken);
                if (lockTaken) 
                {
                    if (!_isResetting && IsRunning && _mediaPlayer != null)
                    {
                        var state = VLCState.Stopped;
                        try { state = _mediaPlayer.State; } catch { }

                        if (state == VLCState.Playing)
                        {
                            if (_mediaPlayer.TakeSnapshot(0, tempPath, 0, 0))
                            {
                                snapshotTaken = true;
                            }
                        }
                    }
                }
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(_frameLock);
            }

            // OCR İŞLEMİ LOCK DIŞINDA (Uzun sürse de lock meşgul edilmez)
            if (snapshotTaken && File.Exists(tempPath))
            {
                try
                {
                    string result = PlateRecognitionHelper.RunOpenALPR(tempPath);
                    PlateResult plateResult = PlateRecognitionHelper.ExtractPlateFromJson(result);

                    if (plateResult != null &&
                        !string.IsNullOrEmpty(plateResult.Plate) &&
                        plateResult.Plate.Length >= SystemParameters.PlateMinimumLength)
                    {
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
            }

            // Timer'ı bir sonraki tur için yeniden kur
            if (IsRunning)
            {
                try {
                    _frameCaptureTimer?.Change(_frameCaptureInterval, Timeout.Infinite); 
                } catch { }
            }
        }

        private void CheckStreamHealth(object stateInfo)
        {
            try
            {
                if (_mediaPlayer == null || !IsRunning)
                    return;

                var state = _mediaPlayer.State;
                
                if (state == VLCState.Stopped || state == VLCState.Error || state == VLCState.Ended)
                {
                    DatabaseManager.Instance.LogSystem("WARNING", 
                        $"Kamera bağlantısı kesildi (State: {state}). Yeniden bağlanılıyor: {CameraId}", 
                        $"CameraWorker.{CameraId}.CheckStreamHealth");

                    AttemptReconnect();
                    return;
                }

                var secondsSinceLastFrame = (DateTime.Now - _lastVideoUpdateTime).TotalSeconds;

                if (secondsSinceLastFrame > SystemParameters.FrameKontrolInterval) 
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
            // Double-Check Locking (Paranoyak Seviye Güvenlik)
            if (_isResetting) return;

            lock (_playerLock)
            {
                if (_isResetting) return;
                
                try
                {
                    if (_mediaPlayer == null) return;

                    _mediaPlayer.Stop();
                    Thread.Sleep(500); 
                    _mediaPlayer.Play(new Media(_libVLC, _rtspUrl, FromType.FromLocation, ":avcodec-hw=none"));
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
        }

        private void HeartbeatTimer_Tick(object stateInfo)
        {
            try
            {
                if (_mediaPlayer == null)
                    return;

                var state = _mediaPlayer.State;
                var secondsSinceLastFrame = (DateTime.Now - _lastVideoUpdateTime).TotalSeconds;
                
                string status = state == VLCState.Playing ? "✅ ÇALIŞIYOR" : $"❌ SORUN ({state})";
                string frameInfo = secondsSinceLastFrame < SystemParameters.FrameKontrolInterval ? 
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
