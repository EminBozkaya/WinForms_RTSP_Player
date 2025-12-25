using System;
using System.Collections.Concurrent;
using System.IO;
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
        public byte[] ImageBytes { get; set; }
        public DateTime CapturedAt { get; set; }
    }

    /// <summary>
    /// Singleton OCR Worker - Sıralı işleme (Sequential Processing) yapar.
    /// Disk I/O ve OpenALPR yükünü tek bir arka plan thread'inde yönetir.
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
            string tempPath = null;
            try
            {
                // Unique temp dosya adı (Lock olmadan)
                tempPath = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid()}.jpg");
                
                // Resmi diske yaz
                File.WriteAllBytes(tempPath, job.ImageBytes);

                // LATENCY METRIC
                double latencyMs = (DateTime.Now - job.CapturedAt).TotalMilliseconds;
#if DEBUG
                // Sadece CİDDİ gecikme varsa (1000ms üzeri) logla
                // Normal işlem süresi 200-500ms arası olabilir
                if (latencyMs > 1000)
                {
                    Console.WriteLine($"[OCR_LATENCY] {latencyMs:F0} ms - {job.CameraId}");
                }
#endif

                // OpenALPR çalıştır
                string jsonResult = PlateRecognitionHelper.RunOpenALPR(tempPath);
                
                // Sonucu parse et
                var plateResult = PlateRecognitionHelper.ExtractPlateFromJson(jsonResult);

                // Dosyayı hemen sil
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                tempPath = null;

                if (plateResult != null && 
                    !string.IsNullOrEmpty(plateResult.Plate) &&
                    plateResult.Plate.Length >= SystemParameters.PlateMinimumLength)
                {
                    // Event fırlat
                    PlateDetected?.Invoke(this, new PlateDetectedEventArgs
                    {
                        CameraId = job.CameraId,
                        Direction = job.Direction,
                        Plate = plateResult.Plate,
                        Confidence = plateResult.Confidence,
                        DetectedAt = DateTime.Now,  // OCR işleme zamanı
                        CapturedAt = job.CapturedAt // Frame yakalama zamanı (restart filtering için)
                    });
                }
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
            finally
            {
                // Her ihtimale karşı temizlik
                if (tempPath != null && File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
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
