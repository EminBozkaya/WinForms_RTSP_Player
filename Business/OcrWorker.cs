using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.Threading;
using WinForms_RTSP_Player.Data;
using WinForms_RTSP_Player.Utilities;

namespace WinForms_RTSP_Player.Business
{
    public class OcrJob
    {
        public string CameraId { get; set; }
        public string Direction { get; set; }
        public Mat Frame { get; set; }
        public DateTime CapturedAt { get; set; }
    }

    public class OcrWorker : IDisposable
    {
        private static readonly Lazy<OcrWorker> _instance = new Lazy<OcrWorker>(() => new OcrWorker());
        public static OcrWorker Instance => _instance.Value;

        private readonly ConcurrentQueue<OcrJob> _queue = new ConcurrentQueue<OcrJob>();
        private readonly AutoResetEvent _signal = new AutoResetEvent(false);
        private readonly Thread _workerThread;
        private volatile bool _running = true;

        public event EventHandler<PlateDetectedEventArgs> PlateDetected;

        private OcrWorker()
        {
            _workerThread = new Thread(ProcessLoop)
            {
                IsBackground = true,
                Name = "OCR_WORKER_THREAD",
                Priority = ThreadPriority.BelowNormal
            };
            _workerThread.Start();
            
            DatabaseManager.Instance.LogSystem("INFO", "OCR Worker başlatıldı.", "OcrWorker.Constructor");
#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] OCR Worker başlatıldı. - OcrWorker.Constructor");
#endif
        }

        public void Enqueue(OcrJob job)
        {
            if (!_running) return;
            if (_queue.Count > 20)
            {
                ClearQueue();
                DatabaseManager.Instance.LogSystem("WARNING", "OCR Kuyruğu taştı, temizlendi.", "OcrWorker.Enqueue");
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [WARNING] OCR Kuyruğu taştı, temizlendi. - OcrWorker.Enqueue");
#endif
            }
            _queue.Enqueue(job);
            _signal.Set();
        }

        public void ClearQueue()
        {
            while (_queue.TryDequeue(out var job)) { job.Frame?.Dispose(); }
        }

        private void ProcessLoop()
        {
            while (_running)
            {
                try
                {
                    if (!_queue.TryDequeue(out var job))
                    {
                        _signal.WaitOne();
                        continue;
                    }
                    ProcessJob(job);
                }
                catch (Exception ex)
                {
                    DatabaseManager.Instance.LogSystem("ERROR", "OCR Worker Loop Hatası", "OcrWorker.ProcessLoop", ex.ToString());
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] [ERROR] OCR Worker Loop Hatası - OcrWorker.ProcessLoop - {ex.Message}");
#endif
                }
            }
        }

        private void ProcessJob(OcrJob job)
        {
            try
            {
                if (job.Frame == null || job.Frame.Empty()) return;

                if ((DateTime.Now - job.CapturedAt).TotalSeconds > 2)
                {
                    job.Frame.Dispose();
                    return;
                }

                var ocrResult = OcrEngine.Instance.RecognizeText(job.Frame);
                bool accepted = false;
                string rejectionReason = "";

                if (ocrResult != null && !string.IsNullOrEmpty(ocrResult.Text))
                {
                    if (ocrResult.Text.Length < SystemParameters.PlateMinimumLength)
                        rejectionReason = "Kısa";
                    else if (ocrResult.Confidence < SystemParameters.OcrConfidence)
                        rejectionReason = $"Düşük Güven (Conf: {ocrResult.Confidence:F2})";
                    else
                        accepted = true;

                    #if DEBUG
                    // if (!accepted)
                    //    Console.WriteLine($"[{DateTime.Now}] [OCR_REJECT] {ocrResult.Text} ({rejectionReason}) - {job.CameraId}");
                    #endif

                    if (accepted)
                    {
                        string sanitizedPlate = PlateSanitizer.ValidateTurkishPlateFormat(ocrResult.Text);
                        if (!string.IsNullOrEmpty(sanitizedPlate))
                        {
                            PlateDetected?.Invoke(this, new PlateDetectedEventArgs
                            {
                                CameraId = job.CameraId,
                                Direction = job.Direction,
                                Plate = sanitizedPlate,
                                Confidence = ocrResult.Confidence * 100f,
                                DetectedAt = DateTime.Now,
                                CapturedAt = job.CapturedAt
                            });

                            #if DEBUG
                            Console.WriteLine($"[{DateTime.Now}] [OCR_SUCCESS] {sanitizedPlate} ({ocrResult.Confidence:F2}) - {job.CameraId}");
                            #endif
                        }
                    }
                }
                else
                {
                    #if DEBUG
                    // Console.WriteLine($"[{DateTime.Now}] [OCR_EMPTY] Metin okunamadı - {job.CameraId}");
                    #endif
                }

                job.Frame?.Dispose();
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", $"OCR İşleme Hatası: {job.CameraId}", "OcrWorker.ProcessJob", ex.ToString());
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [ERROR] OCR İşleme Hatası: {job.CameraId} - OcrWorker.ProcessJob - {ex.Message}");
#endif
            }
        }

        public void Dispose()
        {
            ClearQueue();
            _running = false;
            _signal.Set();
            if (_workerThread.IsAlive) _workerThread.Join(3000);
            _signal.Dispose();
        }
    }
}
