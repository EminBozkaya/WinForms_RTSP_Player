using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.Threading;
using WinForms_RTSP_Player.Data;
using WinForms_RTSP_Player.Utilities;

namespace WinForms_RTSP_Player.Business
{
    /// <summary>
    /// OCR işlemi için taşınacak veri modeli
    /// </summary>
    public class OcrJob
    {
        public string CameraId { get; set; }
        public string Direction { get; set; }
        public Mat Frame { get; set; } // Changed from byte[] to Mat
        public DateTime CapturedAt { get; set; }
    }

    /// <summary>
    /// Singleton OCR Worker - Sıralı işleme (Sequential Processing) yapar.
    /// ONNX-based plate detection ve OCR pipeline'ı tek bir arka plan thread'inde yönetir.
    /// </summary>
    public class OcrWorker : IDisposable
    {
        // Singleton pattern
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
                Priority = ThreadPriority.BelowNormal // UI'ı etkilememesi için
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

            // 1. KUYRUK ŞİŞMESİ KORUMASI
            // Eğer kuyruk çok dolduysa (örn: 20 frame), sistem tıkanmış demektir.
            // Eski frame'lerin hiçbir değeri yok (kapı senaryosunda).
            // Hepsini silip en güncel frame'i işliyoruz.
            if (_queue.Count > 20)
            {
                ClearQueue(); // Hepsini boşalt
                DatabaseManager.Instance.LogSystem("WARNING", 
                    "OCR Kuyruğu taştı, temizlendi.", 
                    "OcrWorker.Enqueue");
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [WARNING] OCR Kuyruğu taştı, temizlendi. - OcrWorker.Enqueue");
#endif
            }

            _queue.Enqueue(job);
            _signal.Set(); // Worker'ı uyandır
        }

        public void ClearQueue()
        {
            // Kuyruğu boşalt
            while (_queue.TryDequeue(out _)) { }
        }

        private void ProcessLoop()
        {
            while (_running)
            {
                try
                {
                    if (!_queue.TryDequeue(out var job))
                    {
                        // İş yoksa uyu
                        _signal.WaitOne();
                        continue;
                    }

                    // İşleme başla
                    ProcessJob(job);
                }
                catch (Exception ex)
                {
                    DatabaseManager.Instance.LogSystem("ERROR", 
                        "OCR Worker Loop Hatası", 
                        "OcrWorker.ProcessLoop", 
                        ex.ToString());
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
                if (job.Frame == null || job.Frame.Empty())
                {
                    DatabaseManager.Instance.LogSystem("WARNING",
                        $"Boş frame alındı: {job.CameraId}",
                        "OcrWorker.ProcessJob");
                    return;
                }

                // STALE FRAME PROTECTION
                // Field condition: If frame is older than 2 seconds, it is irrelevant.
                // This protects against network jitter or CPU spikes accumulating old frames.
                if ((DateTime.Now - job.CapturedAt).TotalSeconds > 2)
                {
                    DatabaseManager.Instance.LogSystem("WARNING",
                        $"Eski frame atlandı ({(DateTime.Now - job.CapturedAt).TotalSeconds:F1}s): {job.CameraId}",
                        "OcrWorker.ProcessJob");
                    
                    job.Frame.Dispose();
                    return;
                }

                // LATENCY METRIC
                double latencyMs = (DateTime.Now - job.CapturedAt).TotalMilliseconds;
#if DEBUG
                if (latencyMs > 1000)
                {
                    Console.WriteLine($"[OCR_LATENCY] {latencyMs:F0} ms - {job.CameraId}");
                }
#endif

                // DIRECT OCR (Input is already cropped ROI)
                // Run OCR
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
                    if (!accepted)
                        Console.WriteLine($"[{DateTime.Now}] [OCR_REJECT] {ocrResult.Text} ({rejectionReason}) - {job.CameraId}");
                    #endif

                    if (accepted)
                    {
                        // Sanitize plate text (Türkçe karakterler)
                        string sanitizedPlate = PlateSanitizer.ValidateTurkishPlateFormat(ocrResult.Text);

                        if (!string.IsNullOrEmpty(sanitizedPlate))
                        {
                            // Event fırlat
                            PlateDetected?.Invoke(this, new PlateDetectedEventArgs
                            {
                                CameraId = job.CameraId,
                                Direction = job.Direction,
                                Plate = sanitizedPlate,
                                Confidence = ocrResult.Confidence * 100f, // Convert to percentage
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
                    Console.WriteLine($"[{DateTime.Now}] [OCR_EMPTY] Metin okunamadı - {job.CameraId}");
                    #endif
                }

                // Clean up frame
                job.Frame?.Dispose();
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR",
                    $"OCR İşleme Hatası: {job.CameraId}",
                    "OcrWorker.ProcessJob",
                    ex.ToString());
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [ERROR] OCR İşleme Hatası: {job.CameraId} - OcrWorker.ProcessJob - {ex.Message}");
#endif
            }
        }

        public void Dispose()
        {
            // 1. Önce kuyruğu temizle (Kapanırken eski işleri yapmaya gerek yok)
            ClearQueue();

            // 2. Döngüyü kır
            _running = false;
            _signal.Set(); // Thread'i son kez uyandır

            // 3. Mevcut işin bitmesini bekle (Graceful Shutdown)
            if (_workerThread.IsAlive)
            {
                // Max 3 saniye bekle, bitmezse zorla kapatma riskini al
                // (OCR işlemi genelde <500ms sürer)
                _workerThread.Join(3000);
            }
            
            _signal.Dispose();
            DatabaseManager.Instance.LogSystem("INFO", "OCR Worker kapatıldı.", "OcrWorker.Dispose");
#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] OCR Worker kapatıldı. - OcrWorker.Dispose");
#endif
        }
    }
}
