using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WinForms_RTSP_Player.Data;

namespace WinForms_RTSP_Player.Business
{
    /// <summary>
    /// Connection state for RTSP stream
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Error
    }

    /// <summary>
    /// Event args for frame received event
    /// </summary>
    public class FrameReceivedEventArgs : EventArgs
    {
        public Mat Frame { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Event args for connection state changed event
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionState State { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// OpenCV-based RTSP stream capture with automatic reconnection
    /// Replaces LibVLC snapshot mechanism with direct frame access
    /// </summary>
    public class OpenCvStreamCapture : IDisposable
    {
        // Events
        public event EventHandler<FrameReceivedEventArgs> FrameReceived;
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        // Public properties
        public string CameraId { get; private set; }
        public bool IsConnected => _state == ConnectionState.Connected;
        public double CurrentFps { get; private set; }
        public ConnectionState State => _state;

        // RTSP configuration
        private readonly string _rtspUrl;
        private VideoCapture _capture;

        // State management
        private ConnectionState _state = ConnectionState.Disconnected;
        private volatile bool _isRunning = false;
        private volatile bool _disposed = false;

        // Threading
        private Thread _captureThread;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        // Frame buffer (removed - using direct event streaming)
        // private readonly ConcurrentQueue<Mat> _frameBuffer = new ConcurrentQueue<Mat>();
        // private const int MAX_BUFFER_SIZE = 5;

        // FPS calculation
        private DateTime _lastFrameTime = DateTime.MinValue;
        private int _frameCount = 0;
        private DateTime _fpsCalculationStart = DateTime.Now;

        // Reconnection logic
        private int _reconnectAttempts = 0;
        private const int MAX_RECONNECT_ATTEMPTS = 5;
        private DateTime _lastReconnectTime = DateTime.MinValue;

        // Locks
        private readonly object _stateLock = new object();
        private readonly object _captureLock = new object();

        public OpenCvStreamCapture(string cameraId, string rtspUrl)
        {
            CameraId = cameraId ?? throw new ArgumentNullException(nameof(cameraId));
            _rtspUrl = rtspUrl ?? throw new ArgumentNullException(nameof(rtspUrl));

            DatabaseManager.Instance.LogSystem("INFO",
                $"OpenCvStreamCapture oluşturuldu: {CameraId}",
                $"OpenCvStreamCapture.{CameraId}.Constructor");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] OpenCvStreamCapture oluşturuldu: {CameraId}");
#endif
        }

        /// <summary>
        /// Start RTSP stream capture
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                DatabaseManager.Instance.LogSystem("WARNING",
                    $"Stream zaten çalışıyor: {CameraId}",
                    $"OpenCvStreamCapture.{CameraId}.Start");
                return;
            }

            _isRunning = true;
            UpdateState(ConnectionState.Connecting, "RTSP bağlantısı başlatılıyor...");

            // REFRESH CANCELLATION TOKEN
            // If it was cancelled before, we must create a new one.
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                 // Dispose old one and create new
                 _cancellationTokenSource.Dispose();
                 _cancellationTokenSource = new CancellationTokenSource();
            }

            // Start capture thread
            _captureThread = new Thread(CaptureLoop)
            {
                IsBackground = true,
                Name = $"OpenCV_Capture_{CameraId}",
                Priority = ThreadPriority.Normal // Balanced priority to allow OCR to run smoothly
            };
            _captureThread.Start();

            DatabaseManager.Instance.LogSystem("INFO",
                $"Stream başlatıldı: {CameraId}",
                $"OpenCvStreamCapture.{CameraId}.Start");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] Stream başlatıldı: {CameraId}");
#endif
        }

        /// <summary>
        /// Stop RTSP stream capture
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _cancellationTokenSource.Cancel();

            // Wait for capture thread to finish (max 3 seconds)
            if (_captureThread != null && _captureThread.IsAlive)
            {
                _captureThread.Join(3000);
            }

            // Release capture
            lock (_captureLock)
            {
                _capture?.Release();
                _capture?.Dispose();
                _capture = null;
            }

            // Clear frame buffer
            ClearFrameBuffer();

            UpdateState(ConnectionState.Disconnected, "Stream durduruldu");

            DatabaseManager.Instance.LogSystem("INFO",
                $"Stream durduruldu: {CameraId}",
                $"OpenCvStreamCapture.{CameraId}.Stop");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] Stream durduruldu: {CameraId}");
#endif
        }

        // GetLatestFrame removed - use event subscription

        /// <summary>
        /// Main capture loop (runs in background thread)
        /// </summary>
        private void CaptureLoop()
        {
            while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Initialize capture if needed
                    if (_capture == null || !_capture.IsOpened())
                    {
                        if (!InitializeCapture())
                        {
                            // Failed to connect, wait before retry
                            Thread.Sleep(2000);
                            continue;
                        }
                    }

                    // Read frame
                    Mat frame = new Mat();
                    bool success = false;

                    lock (_captureLock)
                    {
                        if (_capture != null && _capture.IsOpened())
                        {
                            success = _capture.Read(frame);
                        }
                    }

                    if (success && !frame.Empty())
                    {
                        // Update state to connected if not already
                        if (_state != ConnectionState.Connected)
                        {
                            UpdateState(ConnectionState.Connected, "Stream bağlantısı başarılı");
                            _reconnectAttempts = 0; // Reset reconnect counter
                        }

                        // Update FPS
                        UpdateFps();

                        // Fire event - PASS OWNERSHIP of 'frame' to subscriber
                        // Subscriber (CameraWorker) is responsible for Dispose()
                        if (FrameReceived != null)
                        {
                            FrameReceived.Invoke(this, new FrameReceivedEventArgs
                            {
                                Frame = frame, // No Clone - Direct pass
                                Timestamp = DateTime.Now
                            });
                            
                            // DO NOT Dispose frame here, it's passed to subscriber
                        }
                        else
                        {
                            // If no one is listening, we must dispose
                            frame.Dispose();
                        }

                        _lastFrameTime = DateTime.Now;
                    }
                    else
                    {
                        // Frame read failed
                        frame?.Dispose();

                        // Check if stream is still alive
                        var timeSinceLastFrame = (DateTime.Now - _lastFrameTime).TotalSeconds;
                        if (timeSinceLastFrame > 5 && _state == ConnectionState.Connected)
                        {
                            DatabaseManager.Instance.LogSystem("WARNING",
                                $"Frame akışı durdu ({timeSinceLastFrame:F1}s). Reconnect başlatılıyor: {CameraId}",
                                $"OpenCvStreamCapture.{CameraId}.CaptureLoop");

                            AttemptReconnect();
                        }

                        // Small delay to prevent CPU spinning
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    DatabaseManager.Instance.LogSystem("ERROR",
                        $"Capture loop hatası: {CameraId}",
                        $"OpenCvStreamCapture.{CameraId}.CaptureLoop",
                        ex.ToString());

#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [ERROR] Capture loop hatası: {CameraId} - {ex.Message}");
#endif

                    AttemptReconnect();
                    Thread.Sleep(1000);
                }
            }

            DatabaseManager.Instance.LogSystem("INFO",
                $"Capture loop sonlandı: {CameraId}",
                $"OpenCvStreamCapture.{CameraId}.CaptureLoop");
        }

        /// <summary>
        /// Initialize VideoCapture with RTSP URL
        /// </summary>
        private bool InitializeCapture()
        {
            try
            {
                lock (_captureLock)
                {
                    _capture?.Release();
                    _capture?.Dispose();

                    // Create new capture
                    _capture = new VideoCapture(_rtspUrl, VideoCaptureAPIs.FFMPEG);

                    // Set buffer size (reduce latency)
                    _capture.Set(VideoCaptureProperties.BufferSize, 1);

                    // Try to open
                    if (!_capture.IsOpened())
                    {
                        DatabaseManager.Instance.LogSystem("ERROR",
                            $"RTSP bağlantısı başarısız: {CameraId}",
                            $"OpenCvStreamCapture.{CameraId}.InitializeCapture");

                        UpdateState(ConnectionState.Error, "RTSP bağlantısı başarısız");
                        return false;
                    }

                    DatabaseManager.Instance.LogSystem("INFO",
                        $"RTSP bağlantısı başarılı: {CameraId}",
                        $"OpenCvStreamCapture.{CameraId}.InitializeCapture");

                    _lastFrameTime = DateTime.Now;
                    return true;
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR",
                    $"Capture başlatma hatası: {CameraId}",
                    $"OpenCvStreamCapture.{CameraId}.InitializeCapture",
                    ex.ToString());

                UpdateState(ConnectionState.Error, $"Hata: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempt to reconnect to RTSP stream
        /// </summary>
        private void AttemptReconnect()
        {
            var timeSinceLastReconnect = (DateTime.Now - _lastReconnectTime).TotalSeconds;

            // Prevent reconnect spam
            if (timeSinceLastReconnect < 3)
            {
                _reconnectAttempts++;

                if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
                {
                    DatabaseManager.Instance.LogSystem("ERROR",
                        $"{MAX_RECONNECT_ATTEMPTS} kez reconnect başarısız. Bekleniyor: {CameraId}",
                        $"OpenCvStreamCapture.{CameraId}.AttemptReconnect");

                    UpdateState(ConnectionState.Error, "Çok fazla reconnect denemesi");

                    // Wait longer before next attempt
                    Thread.Sleep(10000);
                    _reconnectAttempts = 0;
                    return;
                }
            }
            else
            {
                _reconnectAttempts = 0; // Reset counter
            }

            _lastReconnectTime = DateTime.Now;
            UpdateState(ConnectionState.Reconnecting, "Yeniden bağlanılıyor...");

            lock (_captureLock)
            {
                _capture?.Release();
                _capture?.Dispose();
                _capture = null;
            }

            DatabaseManager.Instance.LogSystem("INFO",
                $"Reconnect başlatıldı (Deneme: {_reconnectAttempts + 1}): {CameraId}",
                $"OpenCvStreamCapture.{CameraId}.AttemptReconnect");
        }

        /// <summary>
        /// Update FPS calculation
        /// </summary>
        private void UpdateFps()
        {
            _frameCount++;

            var elapsed = (DateTime.Now - _fpsCalculationStart).TotalSeconds;
            if (elapsed >= 1.0)
            {
                CurrentFps = _frameCount / elapsed;
                _frameCount = 0;
                _fpsCalculationStart = DateTime.Now;
            }
        }

        /// <summary>
        /// Update connection state and fire event
        /// </summary>
        private void UpdateState(ConnectionState newState, string message)
        {
            lock (_stateLock)
            {
                if (_state != newState)
                {
                    _state = newState;

                    StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
                    {
                        State = newState,
                        Message = message
                    });

#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [STATE] {CameraId}: {newState} - {message}");
#endif
                }
            }
        }

        /// <summary>
        /// Clear frame buffer (Legacy)
        /// </summary>
        private void ClearFrameBuffer()
        {
            // Buffer removed
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            Stop();

            _cancellationTokenSource?.Dispose();

            DatabaseManager.Instance.LogSystem("INFO",
                $"OpenCvStreamCapture disposed: {CameraId}",
                $"OpenCvStreamCapture.{CameraId}.Dispose");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] OpenCvStreamCapture disposed: {CameraId}");
#endif
        }
    }
}
