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
        }

        public void Enqueue(OcrJob job)
        {
            if (!_running) return;

            // Kuyruk çok şişerse (örn: sistem tıkanırsa) eski işleri atlayabiliriz
            // Şimdilik sadece yeni işi ekliyoruz.
            _queue.Enqueue(job);
            _signal.Set(); // Worker'ı uyandır
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
                        DetectedAt = job.CapturedAt // Görüntünün alındığı zamanı kullan
                    });
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", 
                    $"OCR İşleme Hatası: {job.CameraId}", 
                    "OcrWorker.ProcessJob", 
                    ex.ToString());
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
            _running = false;
            _signal.Set(); // Thread'i son kez uyandır ki loop'tan çıksın
            
            // Thread'in bitmesini bekle (opsiyonel timeout ile)
            if (_workerThread.IsAlive)
            {
                _workerThread.Join(1000);
            }
            
            _signal.Dispose();
        }
    }
}
