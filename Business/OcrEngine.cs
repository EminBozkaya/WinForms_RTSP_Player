using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WinForms_RTSP_Player.Data;
using WinForms_RTSP_Player.Utilities;

namespace WinForms_RTSP_Player.Business
{
    /// <summary>
    /// OCR result with recognized text and confidence
    /// </summary>
    public class OcrResult
    {
        public string Text { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }

    /// <summary>
    /// PaddleOCR-based text recognition using ONNX Runtime
    /// Singleton pattern - model loaded once
    /// NOTE: This is a simplified implementation
    /// Full PaddleOCR requires detection + recognition pipeline
    /// For now, we assume plate is already detected and cropped
    /// </summary>
    public class OcrEngine : IDisposable
    {
        // Singleton instance
        private static readonly Lazy<OcrEngine> _instance = 
            new Lazy<OcrEngine>(() => new OcrEngine());
        public static OcrEngine Instance => _instance.Value;

        // ONNX Runtime
        private InferenceSession? _session;
        private readonly object _sessionLock = new object();

        // Character dictionary
        private List<string> _characters = new List<string>();

        // Model configuration
        private const int INPUT_HEIGHT = 48;
        private const int INPUT_WIDTH = 320;
        private readonly float _confidenceThreshold;

        private bool _disposed = false;

        private OcrEngine()
        {
            _confidenceThreshold = SystemParameters.OcrConfidence;

            try
            {
                LoadCharacterDictionary();
                InitializeModel();

                DatabaseManager.Instance.LogSystem("INFO",
                    $"OcrEngine başlatıldı (Confidence: {_confidenceThreshold}, Chars: {_characters.Count})",
                    "OcrEngine.Constructor");

#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [INFO] OcrEngine başlatıldı");
#endif
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR",
                    "OcrEngine başlatma hatası",
                    "OcrEngine.Constructor",
                    ex.ToString());

                throw;
            }
        }

        private void LoadCharacterDictionary()
        {
            string dictPath = SystemParameters.OcrDictPath;

            if (!File.Exists(dictPath))
            {
                // Fallback: Turkish plate characters (no İ,Ç,Ğ,Q,X,Ö,Ü,Ş,W)
                _characters = new List<string> 
                { 
                    "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
                    "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", 
                    "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", 
                    "V", "Y", "Z"
                };

                DatabaseManager.Instance.LogSystem("WARNING",
                    $"Dictionary dosyası bulunamadı, default kullanılıyor: {dictPath}",
                    "OcrEngine.LoadCharacterDictionary");
            }
            else
            {
                // Load from file line-by-line (Latin dict has many special chars and symbols)
                _characters = File.ReadAllLines(dictPath)
                                  .Select(line => line.Trim('\r', '\n'))
                                  .ToList();

                DatabaseManager.Instance.LogSystem("INFO",
                    $"Dictionary yüklendi: {_characters.Count} karakter - {dictPath}",
                    "OcrEngine.LoadCharacterDictionary");
            }
        }

        private void InitializeModel()
        {
            string modelPath = SystemParameters.OcrModelPath;

            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException($"ONNX model bulunamadı: {modelPath}");
            }

            // Create session options
            var sessionOptions = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };

            // Create inference session
            _session = new InferenceSession(modelPath, sessionOptions);

            DatabaseManager.Instance.LogSystem("INFO",
                $"OCR model yüklendi: {modelPath}",
                "OcrEngine.InitializeModel");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] OCR model yüklendi: {modelPath}");
#endif
        }

        /// <summary>
        /// Recognize text from cropped plate image
        /// </summary>
        public OcrResult RecognizeText(Mat croppedPlate)
        {
            if (croppedPlate == null || croppedPlate.Empty())
                return new OcrResult { Text = "", Confidence = 0f };

            if (_session == null)
            {
                DatabaseManager.Instance.LogSystem("ERROR",
                    "ONNX session null - model yüklenmemiş",
                    "OcrEngine.RecognizeText");
                return new OcrResult { Text = "", Confidence = 0f };
            }

            try
            {
                    // 2. Detect Plate ROI (YOLO)
                var inputTensor = PreprocessImage(croppedPlate);

                // Run inference
                lock (_sessionLock)
                {
                    // DIAGNOSTICS: Check input name if needed
                    /*
                    foreach (var input in _session.InputMetadata)
                        Console.WriteLine($"[OCR DEBUG] Input: {input.Key} Shape: {string.Join(",", input.Value.Dimensions)}");
                    */

                    var inputs = new List<NamedOnnxValue>
                    {
                        // Some models use 'x', some use 'input', some 'images'
                        NamedOnnxValue.CreateFromTensor(_session.InputMetadata.Keys.First(), inputTensor)
                    };

                    using var results = _session.Run(inputs);
                    var output = results.First().AsTensor<float>();

                    // Debug: Log dimensions once in console
                    var dims = output.Dimensions.ToArray();
                    // Console.WriteLine($"[OCR DEBUG] Output Dims: {string.Join("x", dims)} | Chars in Dict: {_characters.Count}");

                    // Postprocess results
                    return PostprocessResults(output);
                }
            }
            catch (Exception ex)
            {
                DatabaseManager.Instance.LogSystem("ERROR",
                    "OCR hatası",
                    "OcrEngine.RecognizeText",
                    ex.ToString());

#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [ERROR] OCR hatası: {ex.Message}");
#endif

                return new OcrResult { Text = "", Confidence = 0f };
            }
        }

        /// <summary>
        /// Preprocess image for OCR input
        /// </summary>
        private DenseTensor<float> PreprocessImage(Mat image)
        {
            // 1. Calculate aspect ratio preserving resize
            float scale = (float)INPUT_HEIGHT / image.Height;
            int newWidth = (int)(image.Width * scale);
            
            // Limit width to INPUT_WIDTH
            if (newWidth > INPUT_WIDTH) newWidth = INPUT_WIDTH;

            // 2. Resize
            Mat resized = new Mat();
            Cv2.Resize(image, resized, new OpenCvSharp.Size(newWidth, INPUT_HEIGHT));

            // 3. Create canvas (padding)
            // Create a black image of the target size (3, 48, 320)
            Mat canvas = new Mat(new OpenCvSharp.Size(INPUT_WIDTH, INPUT_HEIGHT), MatType.CV_8UC3, new Scalar(0, 0, 0));
            
            // 4. Copy resized image to canvas
            Mat rgbResized = new Mat();
            // Check channels
             if (resized.Channels() == 1)
                Cv2.CvtColor(resized, rgbResized, ColorConversionCodes.GRAY2BGR);
            else
                resized.CopyTo(rgbResized);

            // Copy to ROI of canvas
            var roi = new Rect(0, 0, newWidth, INPUT_HEIGHT);
            rgbResized.CopyTo(canvas[roi]);

            // 5. Create tensor
            // Create tensor (1, 3, 48, 320) - NCHW format (RGB)
            var tensor = new DenseTensor<float>(new[] { 1, 3, INPUT_HEIGHT, INPUT_WIDTH });

            // 6. Normalize and fill tensor
            // PaddleOCR expects: (pixel / 255.0 - 0.5) / 0.5  => which is (pixel/255.0)*2.0 - 1.0 => pixel * (2.0/255.0) - 1.0
            // Also BGR -> RGB check (OpenCV is BGR)
            
            for (int y = 0; y < INPUT_HEIGHT; y++)
            {
                for (int x = 0; x < INPUT_WIDTH; x++)
                {
                    Vec3b pixel = canvas.At<Vec3b>(y, x);
                    
                    // BGR to RGB and normalize (0.0 to 1.0)
                    // Trying 0.0 to 1.0 first as it's common for many ONNX exports
                    tensor[0, 0, y, x] = pixel.Item2 / 255f; // R
                    tensor[0, 1, y, x] = pixel.Item1 / 255f; // G
                    tensor[0, 2, y, x] = pixel.Item0 / 255f; // B
                }
            }

            resized.Dispose();
            rgbResized.Dispose();
            canvas.Dispose();

            return tensor;
        }

        /// <summary>
        /// Postprocess OCR output to text
        /// </summary>
        private OcrResult PostprocessResults(Tensor<float> output)
        {
            // PaddleOCR output format: [batch, sequence_length, num_classes]
            // For simplicity, we'll use greedy decoding (argmax)
            
            var dimensions = output.Dimensions.ToArray();
            int seqLength = dimensions.Length > 1 ? dimensions[1] : 1;
            int numClasses = dimensions.Length > 2 ? dimensions[2] : _characters.Count;

            var text = new StringBuilder();
            float totalConfidence = 0f;
            int charCount = 0;

            int lastChar = -1; // For CTC decoding (remove duplicates)

            for (int t = 0; t < seqLength; t++)
            {
                // Find max probability character
                int maxIdx = 0;
                float maxProb = float.MinValue;

                for (int c = 0; c < Math.Min(numClasses, _characters.Count); c++)
                {
                    float prob = output[0, t, c];
                    if (prob > maxProb)
                    {
                        maxProb = prob;
                        maxIdx = c;
                    }
                }

                // CTC decoding: 
                // PaddleOCR standard is usually: Index 0 = BLANK
                // The character list (dict) corresponds to indices 1 to N.
                
                if (maxIdx > 0 && maxIdx != lastChar)
                {
                    // charIdx = maxIdx - 1
                    int charIdx = maxIdx - 1;

                    if (charIdx < _characters.Count)
                    {
                        text.Append(_characters[charIdx]);
                        totalConfidence += maxProb;
                        charCount++;
                    }
                }

                lastChar = maxIdx;
            }

            float avgConfidence = charCount > 0 ? totalConfidence / charCount : 0f;
            string recognizedText = text.ToString();
            
            // Filter garbage (single chars, very short text)
            // Turkish plates minimum 6-7 chars. Filtering < 2 to show ANYTHING for now.
            if (recognizedText.Length < 2)
            {
                return new OcrResult { Text = "", Confidence = 0f };
            }

            return new OcrResult
            {
                Text = recognizedText,
                Confidence = avgConfidence
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            lock (_sessionLock)
            {
                _session?.Dispose();
                _session = null;
            }

            DatabaseManager.Instance.LogSystem("INFO",
                "OcrEngine disposed",
                "OcrEngine.Dispose");

#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [INFO] OcrEngine disposed");
#endif
        }
    }
}
