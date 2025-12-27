# WinForms RTSP Player - ONNX Migration

## 🎯 Proje Durumu

**Versiyon**: 2.0.0 (ONNX Migration)  
**Durum**: Core Implementation Complete (%55)  
**Son Güncelleme**: 27 Aralık 2025

---

## 📋 Genel Bakış

Bu proje, RTSP kamera akışlarından plaka tanıma yapan bir Windows Forms uygulamasıdır. 

**Önceki Mimari**: LibVLC + alpr.exe (external process)  
**Yeni Mimari**: OpenCV + ONNX Runtime (in-process ML)

---

## ✅ Tamamlanan Özellikler

### Core Components
- ✅ **OpenCvStreamCapture**: RTSP stream capture with auto-reconnect
- ✅ **MotionDetector**: Frame differencing based motion detection
- ✅ **PlateDetectionEngine**: YOLOv8 ONNX plate detection
- ✅ **OcrEngine**: PaddleOCR ONNX text recognition
- ✅ **CameraWorkerV2**: Refactored camera worker (OpenCV-based)
- ✅ **OcrWorker**: ONNX pipeline integration

### Kazanımlar
- ❌ Disk I/O eliminated (no temp file writes)
- ❌ Process spawn eliminated (no alpr.exe calls)
- ✅ Motion-based OCR triggering (resource optimization)
- ✅ In-process ONNX inference
- ✅ Frame-level control
- ✅ Turkish character support

---

## 🏗️ Mimari

```
RTSP Stream
    ↓
OpenCvStreamCapture (VideoCapture + FFMPEG)
    ↓
Frame Buffer (Memory - Mat)
    ├── UI Consumer (PictureBox)
    └── Background Pipeline
            ↓
        MotionDetector (Frame Differencing)
            ↓ (if motion detected)
        PlateDetectionEngine (YOLOv8 ONNX)
            ↓ (if plate found)
        OcrEngine (PaddleOCR ONNX)
            ↓
        AccessDecisionManager
            ↓
        HardwareController (Gate Open)
```

---

## 🚀 Hızlı Başlangıç

### Gereksinimler
- .NET 9.0
- Windows 10/11
- RTSP kamera (Dahua IPC-HFW1230S veya uyumlu)
- Arduino (kapı kontrolü için - opsiyonel)

### Kurulum

1. **Repository Clone**
```bash
git clone <repository-url>
cd WinForms_RTSP_Player
```

2. **ONNX Modellerini İndir**
```bash
cd Models
# Manuel indirme gerekli (README.md'ye bakın)
# - yolov8n-plate-detection.onnx (~12 MB)
# - paddle-ocr-rec.onnx (~10 MB)
```

3. **Build**
```bash
dotnet restore
dotnet build
```

4. **Konfigürasyon**

`User.config` dosyasını düzenle:
```xml
<appSettings>
  <add key="RtspUrl_IN" value="rtsp://admin:password@192.168.1.100:554/stream" />
  <add key="RtspUrl_OUT" value="rtsp://admin:password@192.168.1.101:554/stream" />
</appSettings>
```

5. **Çalıştır**
```bash
dotnet run
```

---

## 📁 Proje Yapısı

```
WinForms_RTSP_Player/
├── Business/
│   ├── CameraWorkerV2.cs          # Yeni OpenCV-based worker
│   ├── OpenCvStreamCapture.cs     # RTSP stream capture
│   ├── MotionDetector.cs          # Motion detection
│   ├── PlateDetectionEngine.cs    # YOLOv8 ONNX
│   ├── OcrEngine.cs               # PaddleOCR ONNX
│   ├── OcrWorker.cs               # ONNX pipeline
│   └── CameraWorker.cs            # Eski (deprecated)
├── Data/
│   └── DatabaseManager.cs         # SQLite database
├── Utilities/
│   ├── SystemParameters.cs        # Configuration
│   ├── MatExtensions.cs           # OpenCV helpers
│   └── PlateSanitizer.cs          # Turkish plate validation
├── Models/
│   ├── yolov8n-plate-detection.onnx
│   ├── paddle-ocr-rec.onnx
│   └── paddle-dict-turkish.txt
└── DEPLOYMENT.md                  # Deployment guide
```

---

## 🔧 Konfigürasyon

### System Parameters (Database)

| Parameter | Default | Açıklama |
|-----------|---------|----------|
| `MotionThreshold` | 25.0 | Hareket tespit eşiği (%) |
| `MotionDebounceMs` | 2000 | Debounce süresi (ms) |
| `PlateDetectionConfidence` | 0.5 | YOLO confidence threshold |
| `OcrConfidence` | 0.7 | OCR confidence threshold |
| `PlateMinimumLength` | 6 | Minimum plaka uzunluğu |

### User Config

```xml
<!-- RTSP Cameras -->
<add key="RtspUrl_IN" value="rtsp://..." />
<add key="RtspUrl_OUT" value="rtsp://..." />

<!-- Hardware -->
<add key="ArduinoPort" value="COM3" />
<add key="ArduinoBaudRate" value="9600" />
```

---

## 🧪 Test

### Unit Test (Opsiyonel)
```bash
dotnet test
```

### Manuel Test

1. **OpenCV Stream Test**
   - `OpenCvTestForm` kullan
   - RTSP URL gir ve test et

2. **ONNX Pipeline Test**
   - Debug mode'da çalıştır
   - Console log'larını izle

3. **Uçtan Uca Test**
   - Gerçek kamera ile test et
   - Plaka tanıma doğruluğunu ölç

---

## 📊 Performance

### Hedef Metrikler
- CPU (Idle): <10%
- CPU (Active): <30%
- Memory: <200 MB
- OCR Latency: <300ms
- Restart/24h: <2

### Benchmark
```bash
# Debug mode'da performans logları
dotnet run --configuration Debug
```

---

## 🐛 Troubleshooting

### Sık Karşılaşılan Sorunlar

**1. ONNX model bulunamadı**
- `Models/` klasörünü kontrol et
- Build output'a kopyalandığını doğrula

**2. RTSP bağlantısı başarısız**
- RTSP URL'ini kontrol et
- Kamera erişilebilir mi test et (VLC ile)

**3. Düşük FPS**
- CPU kullanımını kontrol et
- Motion threshold'unu artır

Detaylı troubleshooting için: [DEPLOYMENT.md](DEPLOYMENT.md)

---

## 📝 Dokümantasyon

- [DEPLOYMENT.md](DEPLOYMENT.md) - Deployment guide
- [Models/README.md](Models/README.md) - ONNX model documentation
- [walkthrough.md](.gemini/antigravity/brain/.../walkthrough.md) - Migration walkthrough
- [task.md](.gemini/antigravity/brain/.../task.md) - Implementation task list

---

## 🔄 Migration Status

### Tamamlanan (Faz 1-5)
- [x] Altyapı hazırlığı
- [x] OpenCV stream capture
- [x] Motion detection
- [x] ONNX pipeline
- [x] CameraWorkerV2

### Kalan (Faz 6-9)
- [ ] UI entegrasyonu (PlateRecognitionForm)
- [ ] Uçtan uca test
- [ ] LibVLC cleanup
- [ ] Final documentation

**İlerleme**: %55 (5/9 faz)

---

## 🤝 Katkıda Bulunma

Bu proje şu an development aşamasında ve bireysel kullanım içindir.

---

## 📄 Lisans

Bu proje aşağıdaki açık kaynak bileşenleri kullanmaktadır:

- **OpenCvSharp**: Apache 2.0
- **ONNX Runtime**: MIT
- **YOLOv8**: AGPL-3.0
- **PaddleOCR**: Apache 2.0

---

## 📞 İletişim

Sorular için: [GitHub Issues](../../issues)

---

**Not**: Bu README, ONNX migration sonrası güncellenmiştir. Eski LibVLC-based implementasyon için `CameraWorker.cs` (deprecated) dosyasına bakınız.
