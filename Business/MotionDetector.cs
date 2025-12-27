using OpenCvSharp;
using System;
using WinForms_RTSP_Player.Data;

namespace WinForms_RTSP_Player.Business
{
    /// <summary>
    /// Event args for motion detected event
    /// </summary>
    public class MotionDetectedEventArgs : EventArgs
    {
        public double MotionPercentage { get; set; }
        public DateTime DetectedAt { get; set; }
    }

    /// <summary>
    /// Motion detection using frame differencing
    /// Triggers OCR only when significant movement is detected
    /// </summary>
    public class MotionDetector : IDisposable
    {
        // Events
        public event EventHandler<MotionDetectedEventArgs>? MotionDetected;

        // Configuration
        public string CameraId { get; private set; }
        public double Threshold { get; set; }
        public int DebounceMs { get; set; }

        // State
        private Mat? _previousFrame;
        private DateTime _lastMotionTime = DateTime.MinValue;
        private bool _disposed = false;

        // ROI (Region of Interest) - optional
        private Rect? _roi;

        // Statistics
        public double LastMotionPercentage { get; private set; }
        public DateTime LastMotionTime => _lastMotionTime;

        public MotionDetector(string cameraId, double threshold = 25.0, int debounceMs = 2000)
        {
            CameraId = cameraId ?? throw new ArgumentNullException(nameof(cameraId));
            Threshold = threshold;
            DebounceMs = debounceMs;

            DatabaseManager.Instance.LogSystem("INFO",
                $"MotionDetector oluşturuldu (Threshold: {threshold}, Debounce: {debounceMs}ms): {CameraId}",
                $"MotionDetector.{CameraId}.Constructor");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] MotionDetector oluşturuldu: {CameraId}");
#endif
        }

        /// <summary>
        /// Set Region of Interest for motion detection
        /// Only this area will be checked for motion
        /// </summary>
        public void SetRoi(Rect roi)
        {
            _roi = roi;

            DatabaseManager.Instance.LogSystem("INFO",
                $"ROI ayarlandı: X={roi.X}, Y={roi.Y}, W={roi.Width}, H={roi.Height} - {CameraId}",
                $"MotionDetector.{CameraId}.SetRoi");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] ROI ayarlandı: {CameraId} - {roi}");
#endif
        }

        /// <summary>
        /// Clear ROI (use entire frame)
        /// </summary>
        public void ClearRoi()
        {
            _roi = null;

            DatabaseManager.Instance.LogSystem("INFO",
                $"ROI temizlendi (tüm frame kullanılacak): {CameraId}",
                $"MotionDetector.{CameraId}.ClearRoi");
        }

        /// <summary>
        /// Process frame and detect motion
        /// Returns true if motion detected
        /// </summary>
        public bool ProcessFrame(Mat frame)
        {
            if (frame == null || frame.Empty())
                return false;

            try
            {
                // Convert to grayscale
                Mat grayFrame = new Mat();
                Cv2.CvtColor(frame, grayFrame, ColorConversionCodes.BGR2GRAY);

                // Apply ROI if set
                Mat processFrame = grayFrame;
                if (_roi.HasValue)
                {
                    processFrame = new Mat(grayFrame, _roi.Value);
                }

                // First frame - just store it
                if (_previousFrame == null || _previousFrame.Empty())
                {
                    _previousFrame = processFrame.Clone();
                    grayFrame.Dispose();
                    if (_roi.HasValue && processFrame != grayFrame)
                        processFrame.Dispose();
                    return false;
                }

                // Calculate frame difference
                Mat diff = new Mat();
                Cv2.Absdiff(_previousFrame, processFrame, diff);

                // Apply threshold
                Mat thresh = new Mat();
                Cv2.Threshold(diff, thresh, 25, 255, ThresholdTypes.Binary);

                // Calculate motion percentage
                double motionPixels = Cv2.CountNonZero(thresh);
                double totalPixels = thresh.Rows * thresh.Cols;
                double motionPercentage = (motionPixels / totalPixels) * 100.0;

                LastMotionPercentage = motionPercentage;

                // Clean up
                diff.Dispose();
                thresh.Dispose();

                // Update previous frame
                _previousFrame.Dispose();
                _previousFrame = processFrame.Clone();

                grayFrame.Dispose();
                if (_roi.HasValue && processFrame != grayFrame)
                    processFrame.Dispose();

#if DEBUG
                // Debug: Show motion percentage only when >= threshold
                if (motionPercentage >= Threshold)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [MOTION_DEBUG] {CameraId}: {motionPercentage:F2}% (Threshold: {Threshold}%)");
                }
#endif

                // Check if motion exceeds threshold
                if (motionPercentage > Threshold)
                {
                    // Check debounce
                    var timeSinceLastMotion = (DateTime.Now - _lastMotionTime).TotalMilliseconds;
                    if (timeSinceLastMotion < DebounceMs)
                    {
#if DEBUG
                        Console.WriteLine($"[{DateTime.Now}] [MOTION] Debounced: {CameraId} ({motionPercentage:F2}%)");
#endif
                        return false; // Too soon, ignore
                    }

                    _lastMotionTime = DateTime.Now;

                    DatabaseManager.Instance.LogSystem("INFO",
                        $"Hareket tespit edildi: {motionPercentage:F2}% - {CameraId}",
                        $"MotionDetector.{CameraId}.ProcessFrame");

#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [MOTION] Detected: {CameraId} ({motionPercentage:F2}%)");
#endif

                    // Fire event
                    MotionDetected?.Invoke(this, new MotionDetectedEventArgs
                    {
                        MotionPercentage = motionPercentage,
                        DetectedAt = DateTime.Now
                    });

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR",
                    $"Motion detection hatası: {CameraId}",
                    $"MotionDetector.{CameraId}.ProcessFrame",
                    ex.ToString());

#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [ERROR] Motion detection hatası: {CameraId} - {ex.Message}");
#endif

                return false;
            }
        }

        /// <summary>
        /// Reset motion detector (clear previous frame)
        /// </summary>
        public void Reset()
        {
            _previousFrame?.Dispose();
            _previousFrame = null;
            _lastMotionTime = DateTime.MinValue;
            LastMotionPercentage = 0;

            DatabaseManager.Instance.LogSystem("INFO",
                $"MotionDetector reset: {CameraId}",
                $"MotionDetector.{CameraId}.Reset");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] MotionDetector reset: {CameraId}");
#endif
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _previousFrame?.Dispose();
            _previousFrame = null;

            DatabaseManager.Instance.LogSystem("INFO",
                $"MotionDetector disposed: {CameraId}",
                $"MotionDetector.{CameraId}.Dispose");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] MotionDetector disposed: {CameraId}");
#endif
        }
    }
}
