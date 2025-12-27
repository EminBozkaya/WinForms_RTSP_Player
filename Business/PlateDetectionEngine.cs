using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using WinForms_RTSP_Player.Data;
using WinForms_RTSP_Player.Utilities;

namespace WinForms_RTSP_Player.Business
{
    public class PlateRegion
    {
        public Rect BoundingBox { get; set; }
        public float Confidence { get; set; }
    }

    public class PlateDetectionEngine : IDisposable
    {
        private static readonly Lazy<PlateDetectionEngine> _instance = 
            new Lazy<PlateDetectionEngine>(() => new PlateDetectionEngine());
        public static PlateDetectionEngine Instance => _instance.Value;

        private InferenceSession? _session;
        private readonly object _sessionLock = new object();

        private const int INPUT_WIDTH = 640;
        private const int INPUT_HEIGHT = 640;
        
        private bool _disposed = false;

        private PlateDetectionEngine()
        {
            try
            {
                InitializeModel();
                DatabaseManager.Instance.LogSystem("INFO",
                    $"PlateDetectionEngine başlatıldı (Confidence: {SystemParameters.PlateDetectionConfidence})",
                    "PlateDetectionEngine.Constructor");
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "PlateDetectionEngine başlatma hatası", "PlateDetectionEngine.Constructor", ex.ToString());
                throw;
            }
        }

        private void InitializeModel()
        {
            string modelPath = SystemParameters.PlateDetectionModelPath;
            if (!System.IO.File.Exists(modelPath)) throw new System.IO.FileNotFoundException($"ONNX model bulunamadı: {modelPath}");
            var sessionOptions = new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };
            _session = new InferenceSession(modelPath, sessionOptions);
        }

        public Rect DetectPrimaryRoi(Mat frame)
        {
            var plates = DetectPlates(frame);
            if (plates == null || plates.Count == 0) return new Rect(0, 0, 0, 0);
            return plates.OrderByDescending(p => p.Confidence).First().BoundingBox;
        }

        public List<PlateRegion> DetectPlates(Mat frame)
        {
            if (frame == null || frame.Empty()) return new List<PlateRegion>();
            if (_session == null) return new List<PlateRegion>();

            try
            {
                // 1. Preprocess with Letterboxing
                float scale = Math.Min((float)INPUT_WIDTH / frame.Width, (float)INPUT_HEIGHT / frame.Height);
                int newWidth = (int)(frame.Width * scale);
                int newHeight = (int)(frame.Height * scale);

                using var resized = new Mat();
                Cv2.Resize(frame, resized, new OpenCvSharp.Size(newWidth, newHeight));

                using var canvas = new Mat(INPUT_HEIGHT, INPUT_WIDTH, MatType.CV_8UC3, new Scalar(114, 114, 114)); // YOLO gray padding
                var roi = new Rect(0, 0, newWidth, newHeight);
                resized.CopyTo(canvas[roi]);

                using var rgb = new Mat();
                Cv2.CvtColor(canvas, rgb, ColorConversionCodes.BGR2RGB);

                var tensor = new DenseTensor<float>(new[] { 1, 3, INPUT_HEIGHT, INPUT_WIDTH });
                for (int y = 0; y < INPUT_HEIGHT; y++)
                {
                    for (int x = 0; x < INPUT_WIDTH; x++)
                    {
                        var pixel = rgb.At<Vec3b>(y, x);
                        tensor[0, 0, y, x] = pixel.Item0 / 255f;
                        tensor[0, 1, y, x] = pixel.Item1 / 255f;
                        tensor[0, 2, y, x] = pixel.Item2 / 255f;
                    }
                }

                // 2. Run Inference
                lock (_sessionLock)
                {
                    var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("images", tensor) };
                    using var results = _session.Run(inputs);
                    var output = results.First().AsEnumerable<float>().ToArray();

                    // 3. Postprocess
                    return PostprocessResults(output, frame.Width, frame.Height, scale);
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR", "Plate detection hatası", "PlateDetectionEngine.DetectPlates", ex.ToString());
                return new List<PlateRegion>();
            }
        }

        private List<PlateRegion> PostprocessResults(float[] output, int originalWidth, int originalHeight, float scale)
        {
            var plates = new List<PlateRegion>();
            const int NUM_ANCHORS = 8400;
            float maxFoundConf = 0f;

            for (int i = 0; i < NUM_ANCHORS; i++)
            {
                float confidence = output[NUM_ANCHORS * 4 + i];
                if (confidence > maxFoundConf) maxFoundConf = confidence;

                if (confidence > SystemParameters.PlateDetectionConfidence)
                {
                    float cx = output[NUM_ANCHORS * 0 + i];
                    float cy = output[NUM_ANCHORS * 1 + i];
                    float w  = output[NUM_ANCHORS * 2 + i];
                    float h  = output[NUM_ANCHORS * 3 + i];

                    // Scale back (since we used letterboxing)
                    float x = (cx - w / 2) / scale;
                    float y = (cy - h / 2) / scale;
                    float width = w / scale;
                    float height = h / scale;

                    int left = (int)x;
                    int top = (int)y;
                    int iWidth = (int)width;
                    int iHeight = (int)height;

                    // Clamp
                    left = Math.Max(0, Math.Min(left, originalWidth - 1));
                    top = Math.Max(0, Math.Min(top, originalHeight - 1));
                    iWidth = Math.Max(1, Math.Min(iWidth, originalWidth - left));
                    iHeight = Math.Max(1, Math.Min(iHeight, originalHeight - top));

                    plates.Add(new PlateRegion { BoundingBox = new Rect(left, top, iWidth, iHeight), Confidence = confidence });
                }
            }

            if (plates.Count > 1) plates = ApplyNMS(plates, 0.45f);

#if DEBUG
            if (plates.Count > 0)
                Console.WriteLine($"[{DateTime.Now}] [YOLO] {plates.Count} plaka tespit edildi (MaxConf: {maxFoundConf:F4})");
            else if (maxFoundConf > 0.01)
                Console.WriteLine($"[{DateTime.Now}] [YOLO DEBUG] Plaka bulunamadı. En yüksek aday güveni: {maxFoundConf:F4}");
#endif
            return plates;
        }

        private List<PlateRegion> ApplyNMS(List<PlateRegion> boxes, float iouThreshold)
        {
            boxes = boxes.OrderByDescending(b => b.Confidence).ToList();
            var result = new List<PlateRegion>();
            while (boxes.Count > 0)
            {
                var best = boxes[0];
                result.Add(best);
                boxes.RemoveAt(0);
                boxes = boxes.Where(b => CalculateIoU(best.BoundingBox, b.BoundingBox) < iouThreshold).ToList();
            }
            return result;
        }

        private float CalculateIoU(Rect box1, Rect box2)
        {
            int x1 = Math.Max(box1.X, box2.X), y1 = Math.Max(box1.Y, box2.Y);
            int x2 = Math.Min(box1.X + box1.Width, box2.X + box2.Width), y2 = Math.Min(box1.Y + box1.Height, box2.Y + box2.Height);
            int intersection = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
            int union = box1.Width * box1.Height + box2.Width * box2.Height - intersection;
            return union > 0 ? (float)intersection / union : 0;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            lock (_sessionLock) { _session?.Dispose(); _session = null; }
        }
    }
}
