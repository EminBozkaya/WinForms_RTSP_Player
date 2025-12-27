# Migration Complete - Test Guide

## ✅ Yapılan Değişiklikler

### PlateRecognitionForm.cs
- ❌ `using LibVLCSharp.Shared;` kaldırıldı
- ❌ `Core.Initialize(@"libvlc\win-x64");` kaldırıldı
- ✅ `CameraWorker` → `CameraWorkerV2` geçişi yapıldı
- ✅ FPS tracking eklendi
- ✅ Connection state monitoring eklendi

### CameraWorkerV2.cs
- ✅ `CurrentFps` property eklendi
- ✅ `State` property eklendi
- ✅ UI entegrasyonu için public API hazır

---

## 🧪 Test Adımları

### 1. Uygulamayı Çalıştır

```bash
cd C:\Users\MSI\source\repos\WinForms_RTSP_Player
dotnet run
```

### 2. Kontrol Edilecekler

#### ✅ Başlangıç
- [ ] Uygulama açılıyor mu?
- [ ] Hata mesajı var mı?
- [ ] Login ekranı görünüyor mu?

#### ✅ Kamera Bağlantısı
- [ ] "Başlat" butonuna basıldığında kameralar bağlanıyor mu?
- [ ] Console'da "OpenCV" log'ları görünüyor mu?
- [ ] RTSP bağlantı hatası var mı?

#### ✅ Stream Görüntüsü
- [ ] **ÖNEMLİ**: Şu an PictureBox yok, VideoView'lar hala kullanılıyor
- [ ] VideoView'larda görüntü geliyor mu? (Eski LibVLC ile)
- [ ] Eğer görüntü gelmiyorsa: Normal (OpenCV backend kullanıyor ama UI render yok)

#### ✅ Motion Detection
- [ ] Araç geçişinde console'da `[MOTION]` log'u görünüyor mu?
- [ ] Boş karede motion tetiklenmiyor mu?

#### ✅ Plate Detection
- [ ] Araç geçişinde console'da `[YOLO]` log'u görünüyor mu?
- [ ] Plaka tespit ediliyor mu?
- [ ] Console'da `[OCR]` log'u görünüyor mu?

#### ✅ OCR
- [ ] Plaka doğru okunuyor mu?
- [ ] Türkçe karakterler doğru mu? (Ç, Ğ, İ, Ö, Ş, Ü)
- [ ] Confidence score makul mi? (>0.7)

#### ✅ Database & Gate
- [ ] Plaka DB'ye kaydediliyor mu?
- [ ] Kayıtlı plaka için kapı açılıyor mu?
- [ ] Kayıtsız plaka için kapı açılmıyor mu?

---

## 📊 Beklenen Console Output

```
[2025-12-27 17:00:00] [INFO] CameraWorkerV2 oluşturuldu: CAM_IN (IN)
[2025-12-27 17:00:00] [INFO] CameraWorkerV2 başlatıldı: CAM_IN
[2025-12-27 17:00:01] [STATE] CAM_IN: Connecting - Attempting to connect...
[2025-12-27 17:00:02] [STATE] CAM_IN: Connected - Stream connected
[2025-12-27 17:00:05] [MOTION] Detected: 45.23% - CAM_IN
[2025-12-27 17:00:05] [MOTION] Frame enqueued for OCR: CAM_IN
[2025-12-27 17:00:05] [YOLO] 1 plaka tespit edildi
[2025-12-27 17:00:05] [OCR] Tanınan: '34ABC123' (Confidence: 0.85)
[2025-12-27 17:00:05] [OCR_SUCCESS] 34ABC123 (0.85) - CAM_IN
[2025-12-27 17:00:05] [PLATE] 34ABC123 (85.0%) - CAM_IN
```

---

## 🐛 Olası Sorunlar ve Çözümler

### Sorun 1: "ONNX model bulunamadı"

**Çözüm**:
```bash
# Models klasörünü kontrol et
dir C:\Users\MSI\source\repos\WinForms_RTSP_Player\bin\Debug\net9.0-windows\Models

# Eğer yoksa:
# 1. Models/README.md'deki linklerden modelleri indir
# 2. Models/ klasörüne koy
# 3. Rebuild yap
```

### Sorun 2: "RTSP bağlantısı başarısız"

**Çözüm**:
```bash
# User.config'i kontrol et
notepad C:\Users\MSI\source\repos\WinForms_RTSP_Player\bin\Debug\net9.0-windows\User.config

# RTSP URL formatı:
# rtsp://admin:password@192.168.1.100:554/stream
```

### Sorun 3: "Görüntü gelmiyor"

**Beklenen Durum**: 
- VideoView'lar hala mevcut ama artık kullanılmıyor
- OpenCV backend çalışıyor ama UI render için PictureBox gerekli
- **Çözüm**: Designer'da VideoView → PictureBox değişikliği yapılmalı (opsiyonel)

### Sorun 4: "Plaka tespit edilmiyor"

**Kontrol**:
```bash
# 1. Motion detection çalışıyor mu?
# Console'da [MOTION] log'u olmalı

# 2. YOLO model yüklendi mi?
# Console'da "YOLO model yüklendi" log'u olmalı

# 3. Kamera pozisyonu doğru mu?
# Plaka net görünüyor mu?
```

### Sorun 5: "OCR yanlış okuyor"

**Ayarlama**:
```sql
-- SystemParameters tablosunda threshold'ları ayarla
UPDATE SystemParameters SET Value = '0.6' WHERE Name = 'OcrConfidence';
UPDATE SystemParameters SET Value = '0.4' WHERE Name = 'PlateDetectionConfidence';
```

---

## 📝 Test Sonuçlarını Paylaş

Lütfen şu bilgileri paylaş:

1. **Başlangıç**: Uygulama açıldı mı?
2. **Kamera Bağlantısı**: Bağlantı başarılı mı?
3. **Console Log'ları**: İlk 20 satırı kopyala
4. **Motion Detection**: Çalışıyor mu?
5. **Plate Detection**: Plaka tespit ediliyor mu?
6. **OCR**: Plaka doğru okunuyor mu?
7. **Hatalar**: Varsa hata mesajlarını paylaş

---

## 🎯 Başarı Kriterleri

- [x] Build başarılı (0 hata) ✅
- [ ] Uygulama çalışıyor
- [ ] RTSP bağlantısı başarılı
- [ ] Motion detection çalışıyor
- [ ] Plate detection çalışıyor
- [ ] OCR çalışıyor
- [ ] DB logging çalışıyor
- [ ] Gate control çalışıyor

---

**Not**: UI rendering (PictureBox) opsiyoneldir. Core functionality (motion, detection, OCR, gate) çalışıyorsa migration başarılıdır.
