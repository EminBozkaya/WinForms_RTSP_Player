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
        private bool _firstFrameReceived = false;
        private DateTime _startTime;

        // Timer intervals (ms)
        private int _frameCaptureInterval;
        private int _streamHealthInterval;
        private int _heartbeatInterval;
        private int _periodicResetInterval;

        private int _reconnectAttempts = 0;
        private const int MAX_RECONNECT_ATTEMPTS = 3;
        private DateTime _lastReconnectTime = DateTime.MinValue;

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

            // OCR Worker'dan gelen sonuçları dinle
            OcrWorker.Instance.PlateDetected += OcrWorker_PlateDetected;

            DatabaseManager.Instance.LogSystem("INFO", 
                $"CameraWorker oluşturuldu: {CameraId} ({Direction})", 
                $"CameraWorker.{CameraId}.Constructor");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] CameraWorker oluşturuldu: {CameraId} ({Direction})");
#endif
        }

        // OCR Worker'dan gelen sonucu yakala ve bu kameraya aitse işle
        private void OcrWorker_PlateDetected(object sender, PlateDetectedEventArgs e)
        {
            if (e.CameraId == this.CameraId)
            {
                // 1. TIMESTAMP DRIFT GUARD
                // Eğer OCR sonucu çok geç geldiyse (örn: 5 sn), bu bilgi artık bayattır.
                // Kapı önündeki araç gitmiş olabilir.
                double latency = (DateTime.Now - e.DetectedAt).TotalSeconds;
                if (latency > 5.0)
                {
                    DatabaseManager.Instance.LogSystem("WARNING", 
                        $"OCR Sonucu Gecikmeli (Drift: {latency:F1}s) - İŞLENMEDİ: {e.Plate}", 
                        $"CameraWorker.{CameraId}.OcrWorker_PlateDetected");
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [WARNING] OCR Sonucu Gecikmeli (Drift: {latency:F1}s) - İŞLENMEDİ: {e.Plate} - CameraWorker.{CameraId}.OcrWorker_PlateDetected");
#endif
                    return;
                }

                OnPlateDetected(e);
            }
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
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [WARNING] Kamera zaten çalışıyor: {CameraId} - CameraWorker.{CameraId}.Start");
#endif
                return;
            }

            try
            {
                _firstFrameReceived = false;
                _startTime = DateTime.Now;

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
                // ESKİ KAMERA İÇİN İYİLEŞTİRME (CAM_OUT genelde eski olandır)
                int warmUpDelay = (CameraId == "CAM_OUT") ? 6000 : 3000;

                if (_frameCaptureTimer != null)
                {
                    _frameCaptureTimer.Change(warmUpDelay, Timeout.Infinite); // One-shot başlat
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [INFO] Kamera ısınma süresi ({warmUpDelay}ms) başladı. {CameraId}");
#endif
                }
                
                // Diğer timerları başlat
                _streamHealthTimer?.Change(_streamHealthInterval, _streamHealthInterval);
                _heartbeatTimer?.Change(_heartbeatInterval, _heartbeatInterval);
                _periodicResetTimer?.Change(_periodicResetInterval, _periodicResetInterval);

                DatabaseManager.Instance.LogSystem("INFO", 
                    $"Kamera başlatıldı: {CameraId} ({Direction})", 
                    $"CameraWorker.{CameraId}.Start");
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [INFO] Kamera başlatıldı: {CameraId} ({Direction}) - CameraWorker.{CameraId}.Start");
#endif
            }
            catch (Exception ex)
            {
                IsRunning = false;
                DatabaseManager.Instance.LogSystem("ERROR", 
                    $"Kamera başlatma hatası: {CameraId}", 
                    $"CameraWorker.{CameraId}.Start", 
                    ex.ToString());
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [ERROR] Kamera başlatma hatası: {CameraId} - CameraWorker.{CameraId}.Start - {ex.Message}");
#endif
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
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [INFO] Kamera durduruldu: {CameraId} - CameraWorker.{CameraId}.Stop");
#endif
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    $"Kamera durdurma hatası: {CameraId}", 
                    $"CameraWorker.{CameraId}.Stop", 
                    ex.ToString());
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [ERROR] Kamera durdurma hatası: {CameraId} - CameraWorker.{CameraId}.Stop - {ex.Message}");
#endif
            }
        }

        private void InitializeVLC()
        {
            // ESKİ KAMERA İÇİN ÖZEL AYARLAR (Dynamic Tuning)
            // Eğer kamera "CAM_OUT" ise, caching'i artırıyoruz.
            // "pts_delay increased" hatası, buffer yetmediğini gösterir.
            int networkCaching = (CameraId == "CAM_OUT") ? 3000 : 1000;

            var libvlcOptions = new[]
            {
                $"--network-caching={networkCaching}", // Frame buffer süresi
                "--rtsp-tcp",                 // UDP paket kaybını önlemek için TCP zorla
                "--no-video-title-show",
                "--no-osd",
                "--no-snapshot-preview",
                "--vout=gdi", 
                "--avcodec-hw=none",
                "--clock-jitter=0",           // Jitter kontrolünü VLC'ye bırak
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
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] {logMsg}");
#endif
                    
                    DatabaseManager.Instance.LogSystem("ERROR", 
                        $"VLC Kritik Hata: {e.Message}", 
                        $"CameraWorker.{CameraId}.VLCInternal", 
                        e.Module);
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [ERROR] VLC Kritik Hata: {e.Message} - CameraWorker.{CameraId}.VLCInternal - Module: {e.Module}");
#endif
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
                _firstFrameReceived = true;
            };

            DatabaseManager.Instance.LogSystem("INFO", 
                $"VLC başlatıldı: {CameraId}", 
                $"CameraWorker.{CameraId}.InitializeVLC");
#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] VLC başlatıldı: {CameraId} - CameraWorker.{CameraId}.InitializeVLC");
#endif
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
#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] Threading Timers başlatıldı: {CameraId} - CameraWorker.{CameraId}.InitializeTimers");
#endif
        }

        private void PeriodicResetTimer_Tick(object state)
        {
            // ====== YENİ: CONDITIONAL RESET ======

            // Eğer son 5 dakikada hiç reconnect yoksa, reset'e gerek yok
            var timeSinceLastReconnect = (DateTime.Now - _lastReconnectTime).TotalMinutes;

            if (timeSinceLastReconnect > 30) // 30 dakika sorunsuz çalıştıysa
            {
                DatabaseManager.Instance.LogSystem("INFO",
                    $"Sistem {timeSinceLastReconnect:F0} dakikadır stabil. Periodic reset atlanıyor: {CameraId}",
                    $"CameraWorker.{CameraId}.PeriodicResetTimer_Tick");

                return; // Reset yapma
            }

            // Eğer son 30 dakikada reconnect olduysa, reset yap
            DatabaseManager.Instance.LogSystem("INFO",
                $"Son {timeSinceLastReconnect:F0} dakikada reconnect oldu. Preventive reset: {CameraId}",
                $"CameraWorker.{CameraId}.PeriodicResetTimer_Tick");

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
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [RESET] Kamera periyodik olarak yeniden başlatılıyor: {CameraId}");
#endif

                // Restart sırasında kuyrukta bekleyen eski işleri temizle
                // Bu sayede eski frame'lerin kapı açmasını engelleriz.
                OcrWorker.Instance.ClearQueue();

                DatabaseManager.Instance.LogSystem("INFO",
                    $"Kamera periyodik olarak resetleniyor (Gecikme önleme)",
                    $"CameraWorker.{CameraId}.Restart");
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [INFO] Kamera periyodik olarak resetleniyor (Gecikme önleme) - CameraWorker.{CameraId}.Restart");
#endif

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
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [ERROR] Restart hatası: {CameraId} - CameraWorker.{CameraId}.Restart - {ex.Message}");
#endif
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

            // OCR İŞLEMİNİ KUYRUĞA AT (Hız ve disk bağımsızlığı)
            if (snapshotTaken && File.Exists(tempPath))
            {
                try
                {
                    // Resmi belleğe al (Disk lock'tan kurtulmak için)
                    byte[] imageBytes = File.ReadAllBytes(tempPath);
                    
                    // Temp dosyayı hemen sil
                    File.Delete(tempPath);

                    // İşi kuyruğa at
                    OcrWorker.Instance.Enqueue(new OcrJob
                    {
                        CameraId = this.CameraId,
                        Direction = this.Direction,
                        ImageBytes = imageBytes,
                        CapturedAt = DateTime.Now
                    });
                }
                catch (Exception ex)
                {
                    DatabaseManager.Instance.LogSystem("ERROR", 
                        $"Snapshot Kuyruklama Hatası: {CameraId}", 
                        $"CameraWorker.{CameraId}.FrameCaptureTimer_Tick", 
                        ex.ToString());
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [ERROR] Snapshot Kuyruklama Hatası: {CameraId} - CameraWorker.{CameraId}.FrameCaptureTimer_Tick - {ex.Message}");
#endif
                }
                // Finally bloğuna gerek yok, delete işlemi try içinde yapıldı.
                // Eğer hata olursa dosya kalabilir, ama unique isim olduğu için sorun olmaz.
                // Temizlik için delete'i catch'e de ekleyebiliriz ama byte okuma hatası nadirdir.
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

                // ====== YENİ: STATE-SPECIFIC HANDLING ======

                if (state == VLCState.Error)
                {
                    // Error state: Hemen restart
                    DatabaseManager.Instance.LogSystem("ERROR",
                        $"VLC Error State tespit edildi. Hemen restart: {CameraId}",
                        $"CameraWorker.{CameraId}.CheckStreamHealth");
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [ERROR] VLC Error State tespit edildi. Hemen restart: {CameraId} - CameraWorker.{CameraId}.CheckStreamHealth");
#endif

                    Restart(); // AttemptReconnect yerine direkt Restart
                    return;
                }

                if (state == VLCState.Ended)
                {
                    // Ended state: Stream kapandı, hemen reconnect
                    DatabaseManager.Instance.LogSystem("WARNING",
                        $"Stream Ended (Kamera bağlantısı koptu). Reconnect: {CameraId}",
                        $"CameraWorker.{CameraId}.CheckStreamHealth");
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [WARNING] Stream Ended (Kamera bağlantısı koptu). Reconnect: {CameraId} - CameraWorker.{CameraId}.CheckStreamHealth");
#endif

                    AttemptReconnect();
                    return;
                }

                if (state == VLCState.Stopped)
                {
                    // Stopped state: Ne zamandır stopped?
                    var stoppedDuration = (DateTime.Now - _lastVideoUpdateTime).TotalSeconds;

                    if (stoppedDuration > 5)
                    {
                        DatabaseManager.Instance.LogSystem("WARNING",
                            $"Stream Stopped {stoppedDuration:F1}s. Reconnect: {CameraId}",
                            $"CameraWorker.{CameraId}.CheckStreamHealth");
#if DEBUG
                        Console.WriteLine($"[{DateTime.Now}] [WARNING] Stream Stopped {stoppedDuration:F1}s. Reconnect: {CameraId} - CameraWorker.{CameraId}.CheckStreamHealth");
#endif

                        AttemptReconnect();
                        return;
                    }
                }

                // ====== FIRST FRAME GUARD (İYİLEŞTİRİLMİŞ) ======
                if (!_firstFrameReceived && (DateTime.Now - _startTime).TotalSeconds > 10) // 8 -> 10
                {
                    DatabaseManager.Instance.LogSystem("ERROR",
                        $"10 saniye içinde ilk frame gelmedi. FULL RESTART: {CameraId}",
                        $"CameraWorker.{CameraId}.CheckStreamHealth");
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [ERROR] 10 saniye içinde ilk frame gelmedi. FULL RESTART: {CameraId} - CameraWorker.{CameraId}.CheckStreamHealth");
#endif

                    Restart(); // AttemptReconnect yerine Restart
                    return;
                }

                // ====== FRAME FREEZE GUARD (İYİLEŞTİRİLMİŞ) ======
                var secondsSinceLastFrame = (DateTime.Now - _lastVideoUpdateTime).TotalSeconds;

                if (secondsSinceLastFrame > SystemParameters.FrameKontrolInterval)
                {
                    // ====== YENİ: PROGRESSIVE RECOVERY ======

                    if (secondsSinceLastFrame > 20)
                    {
                        // 20+ saniye donma: Ciddi sorun, full restart
                        DatabaseManager.Instance.LogSystem("ERROR",
                            $"Frame akışı {secondsSinceLastFrame:F1}s durdu (CRİTİCAL). FULL RESTART: {CameraId}",
                            $"CameraWorker.{CameraId}.CheckStreamHealth");
#if DEBUG
                        Console.WriteLine($"[{DateTime.Now}] [ERROR] Frame akışı {secondsSinceLastFrame:F1}s durdu (CRİTİCAL). FULL RESTART: {CameraId} - CameraWorker.{CameraId}.CheckStreamHealth");
#endif

                        Restart();
                    }
                    else if (secondsSinceLastFrame > SystemParameters.FrameKontrolInterval)
                    {
                        // 6-20 saniye arası: Önce reconnect dene
                        DatabaseManager.Instance.LogSystem("WARNING",
                            $"Frame akışı {secondsSinceLastFrame:F1}s durdu. Reconnect: {CameraId}",
                            $"CameraWorker.{CameraId}.CheckStreamHealth");
#if DEBUG
                        Console.WriteLine($"[{DateTime.Now}] [WARNING] Frame akışı {secondsSinceLastFrame:F1}s durdu. Reconnect: {CameraId} - CameraWorker.{CameraId}.CheckStreamHealth");
#endif

                        AttemptReconnect();
                    }
                }

                // ====== YENİ: PROACTIVE HEALTH CHECK ======
                // Playing ama frame rate düşükse uyar
                if (state == VLCState.Playing && secondsSinceLastFrame > 3 && secondsSinceLastFrame < SystemParameters.FrameKontrolInterval)
                {
                    //DatabaseManager.Instance.LogSystem("INFO",
                    //    $"Frame rate düşük: {secondsSinceLastFrame:F1}s (Playing ama yavaş) - {CameraId}",
                    //    $"CameraWorker.{CameraId}.CheckStreamHealth");

#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [INFO] Frame rate düşük: {secondsSinceLastFrame:F1}s (Playing ama yavaş) - {CameraId} - CameraWorker.{CameraId}.CheckStreamHealth");
#endif
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR",
                    $"Sağlık kontrolü hatası: {CameraId}",
                    $"CameraWorker.{CameraId}.CheckStreamHealth",
                    ex.ToString());
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [ERROR] Sağlık kontrolü hatası: {CameraId} - CameraWorker.{CameraId}.CheckStreamHealth - {ex.Message}");
#endif
            }
        }

        private void AttemptReconnect()
        {
            if (_isResetting) return;
            // ====== YENİ: RETRY LIMITER ======
            var timeSinceLastReconnect = (DateTime.Now - _lastReconnectTime).TotalSeconds;

            if (timeSinceLastReconnect < 5)
            {
                _reconnectAttempts++;

                if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
                {
                    DatabaseManager.Instance.LogSystem("WARNING",
                        $"3 kez reconnect başarısız. Full restart yapılıyor: {CameraId}",
                        $"CameraWorker.{CameraId}.AttemptReconnect");

                    _reconnectAttempts = 0;
                    Restart();
                    return;
                }
            }
            else
            {
                _reconnectAttempts = 0; // Reset counter
            }

            _lastReconnectTime = DateTime.Now;

            lock (_playerLock)
            {
                if (_isResetting) return;

                try
                {
                    if (_mediaPlayer == null) return;

                    // ====== ESKİ KOD ======
                    // _mediaPlayer.Stop();
                    // Thread.Sleep(500);
                    // _mediaPlayer.Play(new Media(_libVLC, _rtspUrl, FromType.FromLocation, ":avcodec-hw=none"));

                    // ====== YENİ KOD: DAHA AGRESIF RECONNECT ======

                    // 1. Önce media'yı temizle
                    var currentMedia = _mediaPlayer.Media;
                    currentMedia?.Dispose();

                    // 2. Stop çağır
                    _mediaPlayer.Stop();

                    // 3. Kameraya nefes aldır (CAM_OUT için daha uzun)
                    int waitTime = (CameraId == "CAM_OUT") ? 2000 : 1000;
                    Thread.Sleep(waitTime);

                    // 4. Yeni media oluştur
                    var newMedia = new Media(_libVLC, _rtspUrl, FromType.FromLocation,
                        ":avcodec-hw=none",
                        ":network-caching=" + ((CameraId == "CAM_OUT") ? "3000" : "1000")
                    );

                    // 5. Play
                    _mediaPlayer.Play(newMedia);
                    _lastVideoUpdateTime = DateTime.Now;

                    DatabaseManager.Instance.LogSystem("INFO",
                        $"RTSP yeniden başlatıldı (Enhanced): {CameraId}",
                        $"CameraWorker.{CameraId}.AttemptReconnect");
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [INFO] RTSP yeniden başlatıldı (Enhanced): {CameraId} - CameraWorker.{CameraId}.AttemptReconnect");
#endif
                }
                catch (Exception ex)
                {
                    DatabaseManager.Instance.LogSystem("ERROR",
                        $"Yeniden bağlantı hatası: {CameraId}",
                        $"CameraWorker.{CameraId}.AttemptReconnect",
                        ex.ToString());
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [ERROR] Yeniden bağlantı hatası: {CameraId} - CameraWorker.{CameraId}.AttemptReconnect - {ex.Message}");
#endif

                    // ====== YENİ: FALLBACK MEKANIZMASI ======
                    // Eğer reconnect de başarısız olursa, tam restart yap
                    Task.Run(() =>
                    {
                        Thread.Sleep(3000);
                        if (!IsRunning) return;

                        DatabaseManager.Instance.LogSystem("WARNING",
                            $"Reconnect başarısız, full restart başlatılıyor: {CameraId}",
                            $"CameraWorker.{CameraId}.AttemptReconnect");
#if DEBUG
                        Console.WriteLine($"[{DateTime.Now}] [WARNING] Reconnect başarısız, full restart başlatılıyor: {CameraId} - CameraWorker.{CameraId}.AttemptReconnect");
#endif

                        Restart();
                    });
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
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [INFO] Heartbeat: {CameraId} ({Direction}) - {status} - {frameInfo} - CameraWorker.{CameraId}.Heartbeat");
#endif
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    $"Heartbeat hatası: {CameraId}", 
                    $"CameraWorker.{CameraId}.Heartbeat", 
                    ex.ToString());
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [ERROR] Heartbeat hatası: {CameraId} - CameraWorker.{CameraId}.Heartbeat - {ex.Message}");
#endif
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
                // OCR dinlemeyi bırak
                OcrWorker.Instance.PlateDetected -= OcrWorker_PlateDetected;

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
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [INFO] CameraWorker dispose edildi: {CameraId} - CameraWorker.{CameraId}.Dispose");
#endif
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    $"Dispose hatası: {CameraId}", 
                    $"CameraWorker.{CameraId}.Dispose", 
                    ex.ToString());
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [ERROR] Dispose hatası: {CameraId} - CameraWorker.{CameraId}.Dispose - {ex.Message}");
#endif
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
