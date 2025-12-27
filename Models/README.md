# ONNX Models for License Plate Recognition

Bu klasör, plaka tanıma sistemi için kullanılan ONNX modellerini içerir.

## Model Listesi

### 1. Plaka Tespiti (Plate Detection)
- **Model**: YOLOv8n License Plate Detection
- **Dosya**: `yolov8n-plate-detection.onnx`
- **Boyut**: ~12 MB
- **Kaynak**: Hugging Face - keremberke/yolov8m-license-plate-detection
- **Lisans**: AGPL-3.0 (Ultralytics YOLOv8)
- **İndirme**: 
  ```
  https://huggingface.co/keremberke/yolov8m-license-plate-detection/resolve/main/best.onnx
  ```

### 2. OCR (Text Recognition)
- **Model**: PaddleOCR v5 Latin (Turkish Support)
- **Dosya**: `paddle-ocr-rec.onnx`
- **Boyut**: ~10 MB
- **Kaynak**: Hugging Face - monkt/paddleocr-onnx
- **Lisans**: Apache 2.0 (PaddlePaddle)
- **İndirme**:
  ```
  https://huggingface.co/monkt/paddleocr-onnx
  ```

### 3. OCR Dictionary
- **Dosya**: `paddle-dict-latin.txt`
- **İçerik**: Türkçe karakterler dahil Latin alfabesi
- **Karakterler**: 0-9, A-Z, Ç, Ğ, İ, Ö, Ş, Ü

## Manuel İndirme Talimatları

### YOLOv8n Plate Detection
1. Tarayıcıda aç: https://huggingface.co/keremberke/yolov8m-license-plate-detection
2. `best.onnx` dosyasını indir
3. `yolov8n-plate-detection.onnx` olarak yeniden adlandır
4. Bu klasöre kopyala

### PaddleOCR
1. Tarayıcıda aç: https://huggingface.co/monkt/paddleocr-onnx
2. `languages/latin/rec.onnx` dosyasını indir
3. `paddle-ocr-rec.onnx` olarak yeniden adlandır
4. `languages/latin/dict.txt` dosyasını indir
5. `paddle-dict-latin.txt` olarak yeniden adlandır
6. Her ikisini de bu klasöre kopyala

## Alternatif: Python ile Otomatik İndirme

```python
from huggingface_hub import hf_hub_download

# YOLOv8 Plate Detection
hf_hub_download(
    repo_id="keremberke/yolov8m-license-plate-detection",
    filename="best.onnx",
    local_dir="./Models",
    local_dir_use_symlinks=False
)

# PaddleOCR Recognition
hf_hub_download(
    repo_id="monkt/paddleocr-onnx",
    filename="languages/latin/rec.onnx",
    local_dir="./Models",
    local_dir_use_symlinks=False
)

# PaddleOCR Dictionary
hf_hub_download(
    repo_id="monkt/paddleocr-onnx",
    filename="languages/latin/dict.txt",
    local_dir="./Models",
    local_dir_use_symlinks=False
)
```

## Kullanım

Modeller `SystemParameters.cs` içinde referans edilir:

```csharp
public static string PlateDetectionModelPath => GetString("PlateDetectionModelPath", 
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "yolov8n-plate-detection.onnx"));

public static string OcrModelPath => GetString("OcrModelPath", 
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "paddle-ocr-rec.onnx"));

public static string OcrDictPath => GetString("OcrDictPath", 
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "paddle-dict-latin.txt"));
```

## Notlar

- Modeller `.gitignore`'a eklenmiştir (boyut nedeniyle)
- Her deployment'ta modellerin manuel olarak kopyalanması gerekir
- Alternatif: Build script ile otomatik indirme
