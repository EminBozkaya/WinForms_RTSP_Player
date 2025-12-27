using OpenCvSharp;
using System;
using System.Threading;
using System.Windows.Forms;
using WinForms_RTSP_Player.Data;
using WinForms_RTSP_Player.Utilities;

namespace WinForms_RTSP_Player.Business
{
    /// <summary>
    /// Refactored camera worker - OpenCV + ONNX based
    /// Replaces LibVLC with OpenCvStreamCapture
    /// </summary>
    public class CameraWorkerV2 : IDisposable
    {
        // Public properties
        public string CameraId { get; private set; }
        public string Direction { get; private set; }
        public bool IsRunning { get; private set; }
        
        // Expose stream capture properties for UI
        public double CurrentFps => _streamCapture?.CurrentFps ?? 0.0;
        public ConnectionState State => _streamCapture?.State ?? ConnectionState.Disconnected;

        // Events
        public event EventHandler<PlateDetectedEventArgs> PlateDetected;
        /// <summary>
        /// Frame received event for external UI or processing.
        /// IMPORTANT: The 'Frame' property in EventArgs is disposed immediately after this event returns.
        /// Consumers MUST clone the frame (e.g., e.Frame.Clone()) if they intend to keep it or use it asynchronously.
        /// </summary>
        public event EventHandler<FrameReceivedEventArgs> FrameReceived; // For UI rendering

        // Core components
        private OpenCvStreamCapture _streamCapture;
        private MotionDetector _motionDetector;

        // Configuration
        private readonly string _rtspUrl;
        private readonly PictureBox _pictureBox; // For UI rendering (optional)

        // State
        private bool _disposed = false;
        private DateTime _lastMotionTime = DateTime.MinValue;
        private DateTime _restartInitiatedAt = DateTime.MinValue;
        
        // Burst OCR mode (triggered by motion)
        private DateTime _lastContinuousOcrTime = DateTime.MinValue;
        private const double CONTINUOUS_OCR_INTERVAL_SEC = 0.5;  // Every 0.5 seconds
        
        private const int MAX_OCR_ATTEMPTS = 5;
        private int _ocrAttempts = MAX_OCR_ATTEMPTS; // Start as max to prevent initial firing
        private volatile bool _stopBurst = false;
        private DateTime _burstStartedAt = DateTime.MinValue; // Fail-safe for infinite loops

        // Timers
        private System.Threading.Timer _heartbeatTimer;
        private int _heartbeatInterval;

        public CameraWorkerV2(string cameraId, string rtspUrl, string direction, PictureBox pictureBox = null)
        {
            CameraId = cameraId ?? throw new ArgumentNullException(nameof(cameraId));
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
            _rtspUrl = rtspUrl ?? throw new ArgumentNullException(nameof(rtspUrl));
            _pictureBox = pictureBox;

            _heartbeatInterval = SystemParameters.HeartbeatTimerInterval;

            // Initialize components
            _streamCapture = new OpenCvStreamCapture(cameraId, rtspUrl);
            _motionDetector = new MotionDetector(cameraId, 
                10.0,  // Lowered threshold - typical motion is 7-15%
                SystemParameters.MotionDebounceMs);

            // Subscribe to events
            _streamCapture.FrameReceived += StreamCapture_FrameReceived;
            _streamCapture.StateChanged += StreamCapture_StateChanged;
            _motionDetector.MotionDetected += MotionDetector_MotionDetected;
            OcrWorker.Instance.PlateDetected += OcrWorker_PlateDetected;

            DatabaseManager.Instance.LogSystem("INFO",
                $"CameraWorkerV2 oluşturuldu: {CameraId} ({Direction})",
                $"CameraWorkerV2.{CameraId}.Constructor");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] CameraWorkerV2 oluşturuldu: {CameraId} ({Direction})");
#endif
        }

        public void Start()
        {
            if (IsRunning)
                return;

            IsRunning = true;
            _restartInitiatedAt = DateTime.Now;

            // Start stream capture
            _streamCapture.Start();

            // Start heartbeat timer
            _heartbeatTimer = new System.Threading.Timer(
                HeartbeatTimer_Tick,
                null,
                _heartbeatInterval,
                _heartbeatInterval);

            DatabaseManager.Instance.LogSystem("INFO",
                $"CameraWorkerV2 başlatıldı: {CameraId}",
                $"CameraWorkerV2.{CameraId}.Start");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] CameraWorkerV2 başlatıldı: {CameraId}");
#endif
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            IsRunning = false;

            // Stop timers
            _heartbeatTimer?.Change(Timeout.Infinite, Timeout.Infinite);

            // Stop stream
            _streamCapture?.Stop();

            DatabaseManager.Instance.LogSystem("INFO",
                $"CameraWorkerV2 durduruldu: {CameraId}",
                $"CameraWorkerV2.{CameraId}.Stop");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] CameraWorkerV2 durduruldu: {CameraId}");
#endif
        }

        private void StreamCapture_FrameReceived(object sender, FrameReceivedEventArgs e)
        {
            // Process frames even if not officially "started" - stream auto-starts on connect
            try
            {
                bool hasMotion = false;
                
                // Check if we should be in burst mode
                bool isBurstActive = _ocrAttempts < MAX_OCR_ATTEMPTS && !_stopBurst;

                // FAIL-SAFE: Check for infinite burst (e.g. motion but no valid ROI ever found)
                if (isBurstActive && (DateTime.Now - _burstStartedAt).TotalSeconds > 10)
                {
                    _stopBurst = true;
                    _ocrAttempts = MAX_OCR_ATTEMPTS;
                    isBurstActive = false;
                    DatabaseManager.Instance.LogSystem("WARNING", $"Burst mode timed out (10s) - {CameraId}", $"CameraWorkerV2.{CameraId}.FailSafe");
                }

                // Only run motion detection if NOT in burst mode (optimization)
                if (!isBurstActive)
                {
                    // 1. Motion detection (CPU intensive - only run when needed)
                    hasMotion = _motionDetector.ProcessFrame(e.Frame);
                    
                    if (hasMotion)
                    {
                        // Only start/reset burst if not already active or finished
                        if (_ocrAttempts >= MAX_OCR_ATTEMPTS || _stopBurst)
                        {
                            _ocrAttempts = 0;
                            _stopBurst = false;
                            _lastContinuousOcrTime = DateTime.MinValue; // Force immediate first OCR
                            _burstStartedAt = DateTime.Now; // Fail-safe timer start
                            isBurstActive = true;

                            #if DEBUG
                            Console.WriteLine($"[{DateTime.Now}] [OCR_BURST] Started for {CameraId}");
                            #endif
                        }
                    }
                }

                // 2. UI rendering (if PictureBox provided)
                if (_pictureBox != null)
                {
                    // CRITICAL: Clone frame for UI to avoid race condition with main Dispose
                    // The UI method will be responsible for disposing this clone
                    Mat uiClone = e.Frame.Clone();
                    RenderFrameToUI(uiClone);
                }

                // 3. Fire frame received event (for external UI)
                FrameReceived?.Invoke(this, e);

                // 4. ROI Sensing & CROPPING logic (Burst Mode)
                if (isBurstActive)
                {
                    var timeSinceLastOcr = (DateTime.Now - _lastContinuousOcrTime).TotalSeconds;

                    if (timeSinceLastOcr >= CONTINUOUS_OCR_INTERVAL_SEC)
                    {
                        // Update timing immediately to prevent spam
                        _lastContinuousOcrTime = DateTime.Now;

                        // DETECT ROI (YOLO) - Runs on this thread but only occasional (0.5s)
                        // This prevents blocking every frame
                        Rect roi = PlateDetectionEngine.Instance.DetectPrimaryRoi(e.Frame);

                        if (roi.Width > 0 && roi.Height > 0)
                        {
                            // Crop the plate region
                            Mat cropped = new Mat(e.Frame, roi).Clone();

                            // Enqueue to OCR worker (It will do ONLY OCR now)
                            OcrWorker.Instance.Enqueue(new OcrJob
                            {
                                CameraId = this.CameraId,
                                Direction = this.Direction,
                                Frame = cropped, 
                                CapturedAt = e.Timestamp
                            });
                            
                            _ocrAttempts++;
                        }
                        else
                        {
                             // ROI not found - FALLBACK: Try whole frame (case for very zoomed in images)
                             // To avoid over-occupying OCR worker, we use a slightly lower priority or just send it
                             // Since our interval is 0.5s, sending one full frame is not a big deal.
                             
                             OcrWorker.Instance.Enqueue(new OcrJob
                             {
                                 CameraId = this.CameraId,
                                 Direction = this.Direction,
                                 Frame = e.Frame.Clone(), // Full frame fallback
                                 CapturedAt = e.Timestamp
                             });

                             // We count this as an attempt to prevent infinite whole-frame OCR
                             _ocrAttempts++;

                             #if DEBUG
                             Console.WriteLine($"[{DateTime.Now}] [YOLO_FALLBACK] No ROI found for {CameraId}, sending full frame to OCR.");
                             #endif
                        }
                        
                        // Motion detection was skipped/reset
                        _lastMotionTime = DateTime.Now; 
                    }
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR",
                    $"Frame processing error: {CameraId}",
                    $"CameraWorkerV2.{CameraId}.StreamCapture_FrameReceived",
                    ex.ToString());
            }
            finally
            {
                // CRITICAL: We own the frame (passed from OpenCvStreamCapture).
                // We must dispose it to prevent memory leaks.
                e.Frame?.Dispose();
            }
        }

        private void RenderFrameToUI(Mat frame)
        {
            if (_pictureBox == null || _pictureBox.IsDisposed)
            {
                frame?.Dispose(); // We own this clone now
                return;
            }

            try
            {
                if (_pictureBox.InvokeRequired)
                {
                    // Pass ownership to UI thread
                    _pictureBox.BeginInvoke(new Action(() => RenderFrameToUI(frame)));
                    return;
                }

                // Convert Mat to Bitmap
                // ToBitmap() creates a new Bitmap from Mat.
                using (frame) // Ensure Mat is disposed after conversion
                {
                    var bitmap = frame.ToBitmap();
                    if (bitmap != null)
                    {
                        var oldImage = _pictureBox.Image;
                        _pictureBox.Image = bitmap;
                        oldImage?.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                // Ensure disposal on error
                frame?.Dispose(); 

                DatabaseManager.Instance.LogSystem("ERROR",
                    $"UI rendering error: {CameraId}",
                    $"CameraWorkerV2.{CameraId}.RenderFrameToUI",
                    ex.ToString());
            }
        }

        private void StreamCapture_StateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            DatabaseManager.Instance.LogSystem("INFO",
                $"Stream state changed: {e.State} - {e.Message} - {CameraId}",
                $"CameraWorkerV2.{CameraId}.StreamCapture_StateChanged");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [STATE] {CameraId}: {e.State} - {e.Message}");
#endif
        }

        private void MotionDetector_MotionDetected(object sender, MotionDetectedEventArgs e)
        {
#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [MOTION] Detected: {e.MotionPercentage:F2}% - {CameraId}");
#endif
        }

        private void OcrWorker_PlateDetected(object sender, PlateDetectedEventArgs e)
        {
            if (e.CameraId != this.CameraId)
                return;

            // Stop burst AND Reset logic
            _stopBurst = true;
            _ocrAttempts = MAX_OCR_ATTEMPTS; // Reset attempts to MAX so it doesn't auto-start again immediately
            
            // Note: Motion detector will need to "re-arm" logic in StreamCapture_FrameReceived
            // Currently: if (_ocrAttempts < MAX_OCR_ATTEMPTS && !_stopBurst) -> isBurstActive
            // So setting Attempts = MAX effectively stops it.

            // Pre-restart frame filtering
            if (e.CapturedAt < _restartInitiatedAt)
            {
                DatabaseManager.Instance.LogSystem("WARNING",
                    $"Pre-restart frame filtered: {e.Plate} - {CameraId}",
                    $"CameraWorkerV2.{CameraId}.OcrWorker_PlateDetected");
                return;
            }

            // Forward event
            PlateDetected?.Invoke(this, e);

            DatabaseManager.Instance.LogSystem("INFO",
                $"Plate detected: {e.Plate} ({e.Confidence:F1}%) - {CameraId}",
                $"CameraWorkerV2.{CameraId}.OcrWorker_PlateDetected");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [PLATE] {e.Plate} ({e.Confidence:F1}%) - {CameraId}");
#endif
        }

        private void HeartbeatTimer_Tick(object state)
        {
            if (!IsRunning)
                return;

            try
            {
                var timeSinceLastMotion = (DateTime.Now - _lastMotionTime).TotalSeconds;

                DatabaseManager.Instance.LogSystem("INFO",
                    $"Heartbeat: FPS={_streamCapture.CurrentFps:F1}, " +
                    $"State={_streamCapture.State}, " +
                    $"LastMotion={timeSinceLastMotion:F0}s ago - {CameraId}",
                    $"CameraWorkerV2.{CameraId}.Heartbeat");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR",
                    $"Heartbeat error: {CameraId}",
                    $"CameraWorkerV2.{CameraId}.HeartbeatTimer_Tick",
                    ex.ToString());
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            Stop();

            // Unsubscribe events
            if (_streamCapture != null)
            {
                _streamCapture.FrameReceived -= StreamCapture_FrameReceived;
                _streamCapture.StateChanged -= StreamCapture_StateChanged;
            }

            if (_motionDetector != null)
            {
                _motionDetector.MotionDetected -= MotionDetector_MotionDetected;
            }

            OcrWorker.Instance.PlateDetected -= OcrWorker_PlateDetected;

            // Dispose components
            _streamCapture?.Dispose();
            _motionDetector?.Dispose();
            _heartbeatTimer?.Dispose();

            DatabaseManager.Instance.LogSystem("INFO",
                $"CameraWorkerV2 disposed: {CameraId}",
                $"CameraWorkerV2.{CameraId}.Dispose");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] CameraWorkerV2 disposed: {CameraId}");
#endif
        }
    }
}
