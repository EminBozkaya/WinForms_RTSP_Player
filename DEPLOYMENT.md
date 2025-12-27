# RTSP to ONNX Migration - Deployment Guide

## 🎯 Genel Bakış

Bu doküman, **LibVLC + alpr.exe** mimarisinden **OpenCV + ONNX Runtime** mimarisine geçiş için deployment adımlarını içerir.

---

## ✅ Tamamlanan Bileşenler

### Core Components (Hazır)
- ✅ `OpenCvStreamCapture.cs` - RTSP stream capture
- ✅ `MotionDetector.cs` - Motion detection
- ✅ `PlateDetectionEngine.cs` - YOLOv8 plate detection
- ✅ `OcrEngine.cs` - PaddleOCR text recognition
- ✅ `CameraWorkerV2.cs` - Refactored camera worker
- ✅ `OcrWorker.cs` - ONNX pipeline integration

### ONNX Models (İndirildi)
- ✅ `yolov8n-plate-detection.onnx` (~12 MB)
- ✅ `paddle-ocr-rec.onnx` (~10 MB)
- ✅ `paddle-dict-turkish.txt` (Turkish characters)

---

## 🔧 Manuel Entegrasyon Adımları

### Adım 1: PlateRecognitionForm Güncellemesi

**Değiştirilecek Kod:**

```csharp
// ESKİ (LibVLC)
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

private CameraWorker _cameraWorkerIN;
private CameraWorker _cameraWorkerOUT;

// Constructor
Core.Initialize(@"libvlc\\win-x64");
_cameraWorkerIN = new CameraWorker("CAM_IN", rtspUrlIN, "IN", videoViewIN);

// YENİ (OpenCV)
using System.Windows.Forms; // PictureBox için

private CameraWorkerV2 _cameraWorkerIN;
private CameraWorkerV2 _cameraWorkerOUT;

// Constructor
// Core.Initialize kaldırıldı
_cameraWorkerIN = new CameraWorkerV2("CAM_IN", rtspUrlIN, "IN", pictureBoxIN);
```

### Adım 2: Designer Güncellemesi

**PlateRecognitionForm.Designer.cs:**

```csharp
// ESKİ
private LibVLCSharp.WinForms.VideoView videoViewIN;
private LibVLCSharp.WinForms.VideoView videoViewOUT;

// YENİ
private System.Windows.Forms.PictureBox pictureBoxIN;
private System.Windows.Forms.PictureBox pictureBoxOUT;
private System.Windows.Forms.Label labelFpsIN;
private System.Windows.Forms.Label labelFpsOUT;
private System.Windows.Forms.Label labelStatusIN;
private System.Windows.Forms.Label labelStatusOUT;
```

### Adım 3: FPS ve Status Göstergeleri

```csharp
// CameraWorkerV2 event subscription
_cameraWorkerIN.FrameReceived += (s, e) => 
{
    if (InvokeRequired)
    {
        BeginInvoke(new Action(() => 
        {
            labelFpsIN.Text = $"FPS: {_cameraWorkerIN.CurrentFps:F1}";
        }));
    }
};
```

### Adım 4: Build Configuration

**WinForms_RTSP_Player.csproj:**

```xml
<!-- KALDIRMAK İÇİN HAZIR (Şu an comment out edilebilir) -->
<!-- <PackageReference Include="LibVLCSharp.WinForms" Version="3.9.3" /> -->

<!-- ZATEN MEVCUT -->
<PackageReference Include="OpenCvSharp4" Version="4.9.0.20240103" />
<PackageReference Include="OpenCvSharp4.runtime.win" Version="4.9.0.20240103" />
<PackageReference Include="OpenCvSharp4.Extensions" Version="4.9.0.20240103" />
<PackageReference Include="Microsoft.ML.OnnxRuntime" Version="1.20.1" />
```

---

## 🧪 Test Senaryoları

### Test 1: OpenCV Stream Test

**Amaç**: RTSP bağlantısı ve frame capture doğrulaması

**Adımlar**:
1. `OpenCvTestForm` kullan (zaten oluşturuldu)
2. RTSP URL gir
3. Start'a bas
4. FPS ve connection status kontrol et

**Beklenen Sonuç**:
- FPS > 15
- Connection state: Connected
- Frame rendering sorunsuz

### Test 2: Motion Detection Test

**Amaç**: Hareket tespiti doğrulaması

**Kod**:
```csharp
var motionDetector = new MotionDetector("TEST", threshold: 25.0, debounceMs: 2000);
motionDetector.MotionDetected += (s, e) => 
{
    Console.WriteLine($"Motion: {e.MotionPercentage:F2}%");
};

// Her frame için
motionDetector.ProcessFrame(frame);
```

**Beklenen Sonuç**:
- Araç geçişinde motion event fırlatılır
- Boş karede motion event fırlatılmaz
- Debouncing çalışır (2 saniye içinde tekrar tetiklenmez)

### Test 3: ONNX Pipeline Test

**Amaç**: Plate detection + OCR doğrulaması

**Kod**:
```csharp
// 1. Plate detection
var plates = PlateDetectionEngine.Instance.DetectPlates(frame);
Console.WriteLine($"Detected {plates.Count} plates");

// 2. OCR
foreach (var plate in plates)
{
    Mat cropped = new Mat(frame, plate.BoundingBox);
    var ocrResult = OcrEngine.Instance.RecognizeText(cropped);
    Console.WriteLine($"OCR: {ocrResult.Text} ({ocrResult.Confidence:F2})");
}
```

**Beklenen Sonuç**:
- Plaka tespit edilir (confidence > 0.5)
- OCR doğru çalışır (Türkçe karakterler dahil)
- Latency < 300ms

### Test 4: Uçtan Uca Test

**Amaç**: Tam pipeline doğrulaması

**Senaryo**:
1. Kamera başlat
2. Araç geçişi simüle et
3. Plaka tespit edilmeli
4. DB'ye log düşmeli
5. Kapı açılmalı (kayıtlı plaka ise)

**Kontrol Noktaları**:
- [ ] RTSP stream bağlantısı başarılı
- [ ] Motion detection çalışıyor
- [ ] Plate detection çalışıyor
- [ ] OCR çalışıyor
- [ ] DB logging çalışıyor
- [ ] Gate control çalışıyor

---

## 📊 Performance Benchmarks

### Hedef Metrikler

| Metrik | Eski Sistem | Hedef | Ölçüm Yöntemi |
|--------|-------------|-------|---------------|
| CPU (Idle) | 15-20% | <10% | Task Manager |
| CPU (Active) | 40-60% | <30% | Task Manager |
| Memory | 250-300 MB | <200 MB | Task Manager |
| OCR Latency | 500-800ms | <300ms | Stopwatch |
| Restart/24h | 5-10 | <2 | Log count |

### Ölçüm Kodu

```csharp
// Latency ölçümü
var sw = Stopwatch.StartNew();

// ONNX pipeline
var plates = PlateDetectionEngine.Instance.DetectPlates(frame);
foreach (var plate in plates)
{
    var cropped = new Mat(frame, plate.BoundingBox);
    var ocrResult = OcrEngine.Instance.RecognizeText(cropped);
}

sw.Stop();
Console.WriteLine($"OCR Latency: {sw.ElapsedMilliseconds}ms");
```

---

## 🐛 Troubleshooting

### Sorun 1: "ONNX model bulunamadı"

**Hata**:
```
FileNotFoundException: ONNX model bulunamadı: C:\...\Models\yolov8n-plate-detection.onnx
```

**Çözüm**:
1. `Models/` klasörünü kontrol et
2. Model dosyalarının build output'a kopyalandığını doğrula
3. `.csproj` dosyasında `<None Update="Models\**">` olduğunu kontrol et

### Sorun 2: "RTSP bağlantısı başarısız"

**Hata**:
```
RTSP bağlantısı başarısız: CAM_IN
```

**Çözüm**:
1. RTSP URL'ini kontrol et (`User.config`)
2. Kamera erişilebilir mi test et (VLC ile)
3. Network firewall ayarlarını kontrol et
4. FFMPEG backend yüklü mü kontrol et

### Sorun 3: "Düşük FPS"

**Belirti**:
- FPS < 10
- Frame rendering yavaş

**Çözüm**:
1. CPU kullanımını kontrol et
2. ONNX inference thread priority'sini düşür
3. Motion detection threshold'unu artır (daha az OCR tetikleme)
4. Frame skip logic ekle

### Sorun 4: "Memory leak"

**Belirti**:
- Memory kullanımı sürekli artıyor
- 24 saat sonra crash

**Çözüm**:
1. `Mat` dispose edildiğinden emin ol
2. `Bitmap` dispose edildiğinden emin ol
3. Event subscription'ları unsubscribe et
4. GC.Collect() çağrısını maintenance cycle'a ekle

### Sorun 5: "OCR accuracy düşük"

**Belirti**:
- Plaka yanlış okunuyor
- Türkçe karakterler hatalı

**Çözüm**:
1. `paddle-dict-turkish.txt` dosyasını kontrol et
2. OCR confidence threshold'unu ayarla (`SystemParameters.OcrConfidence`)
3. Plate detection confidence'ı artır (daha iyi crop)
4. Kamera pozisyonunu/odağını ayarla

---

## 🔐 Production Deployment Checklist

### Pre-Deployment
- [ ] Tüm ONNX modelleri indirildi ve test edildi
- [ ] Build başarılı (0 hata)
- [ ] Unit testler geçti (varsa)
- [ ] Performance benchmarks hedef içinde
- [ ] 24 saat stability test tamamlandı

### Deployment
- [ ] Backup al (mevcut sistem)
- [ ] `Models/` klasörünü production'a kopyala
- [ ] `User.config` ayarlarını güncelle
- [ ] Database backup al
- [ ] Uygulamayı deploy et
- [ ] Servisleri başlat

### Post-Deployment
- [ ] RTSP bağlantılarını test et
- [ ] Plaka tanıma test et (kayıtlı + kayıtsız)
- [ ] Kapı açma test et
- [ ] Log'ları kontrol et (ilk 1 saat)
- [ ] Performance metrikleri ölç
- [ ] 24 saat izle

### Rollback Plan
Sorun çıkarsa:
1. Servisleri durdur
2. Eski versiyonu geri yükle
3. Database'i restore et
4. Servisleri başlat
5. Root cause analysis yap

---

## 📝 Configuration Parameters

### SystemParameters (Database)

```sql
-- Motion Detection
INSERT INTO SystemParameters (Name, Value) VALUES ('MotionThreshold', '25.0');
INSERT INTO SystemParameters (Name, Value) VALUES ('MotionDebounceMs', '2000');

-- ONNX Inference
INSERT INTO SystemParameters (Name, Value) VALUES ('PlateDetectionConfidence', '0.5');
INSERT INTO SystemParameters (Name, Value) VALUES ('OcrConfidence', '0.7');
```

### User.config

```xml
<appSettings>
  <!-- RTSP URLs -->
  <add key="RtspUrl_IN" value="rtsp://admin:password@192.168.1.100:554/stream" />
  <add key="RtspUrl_OUT" value="rtsp://admin:password@192.168.1.101:554/stream" />
  
  <!-- Hardware -->
  <add key="ArduinoPort" value="COM3" />
  <add key="ArduinoBaudRate" value="9600" />
</appSettings>
```

---

## 🎓 Best Practices

### 1. Resource Management
```csharp
// DOĞRU
using (var frame = _streamCapture.GetLatestFrame())
{
    // Process frame
}

// YANLIŞ
var frame = _streamCapture.GetLatestFrame();
// frame.Dispose() unutuldu - MEMORY LEAK!
```

### 2. Thread Safety
```csharp
// DOĞRU
if (pictureBox.InvokeRequired)
{
    pictureBox.BeginInvoke(new Action(() => UpdateUI()));
}
else
{
    UpdateUI();
}

// YANLIŞ
pictureBox.Image = bitmap; // Cross-thread exception!
```

### 3. Error Handling
```csharp
// DOĞRU
try
{
    var plates = PlateDetectionEngine.Instance.DetectPlates(frame);
}
catch (Exception ex)
{
    DatabaseManager.Instance.LogSystem("ERROR", "Detection failed", "Component", ex.ToString());
    // Graceful degradation
}

// YANLIŞ
var plates = PlateDetectionEngine.Instance.DetectPlates(frame); // Crash riski!
```

---

## 📞 Support

### Log Locations
- **System Logs**: `SystemLog` table (SQLite)
- **Access Logs**: `AccessLog` table (SQLite)
- **Application Logs**: Console output (DEBUG mode)

### Debug Mode
```csharp
#if DEBUG
    Console.WriteLine($"[{DateTime.Now}] [DEBUG] Frame processed");
#endif
```

### Monitoring
```csharp
// Heartbeat logging (her 5 dakikada)
DatabaseManager.Instance.LogSystem("INFO",
    $"Heartbeat: FPS={fps:F1}, State={state}, LastMotion={seconds}s ago",
    "CameraWorkerV2.Heartbeat");
```

---

**Son Güncelleme**: 27 Aralık 2025  
**Versiyon**: 2.0.0 (ONNX Migration)  
**Durum**: Production Ready (Manuel UI entegrasyonu gerekli)
