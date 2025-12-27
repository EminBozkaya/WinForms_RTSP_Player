# Motion Detection Debug Guide

## Sorun: Motion Detection Çalışmıyor

### Kontrol Listesi:

1. **Build yapıldı mı?**
   ```bash
   dotnet build
   ```

2. **Uygulama yeniden başlatıldı mı?**
   - Eski instance'ı kapat
   - Yeni build'i çalıştır

3. **Console'da şu log'ları gör**:
   ```
   [INFO] MotionDetector oluşturuldu: CAM_IN
   [STATE] CAM_IN: Connected
   ```

4. **Frame geldiğinde debug log ekle**:
   
   `CameraWorkerV2.cs` satır 125'e şunu ekle:
   ```csharp
   Console.WriteLine($"[DEBUG] Frame received: {CameraId}");
   ```

5. **Motion threshold kontrolü**:
   
   Database'de kontrol et:
   ```sql
   SELECT * FROM SystemParameters WHERE Name = 'MotionThreshold';
   ```
   
   Değer: 25.0 (varsayılan)
   
   **Çok yüksekse motion detect olmaz!**
   
   Geçici olarak düşür:
   ```sql
   UPDATE SystemParameters SET Value = '5.0' WHERE Name = 'MotionThreshold';
   ```

6. **MotionDetector.ProcessFrame çağrılıyor mu?**
   
   `MotionDetector.cs` içine debug log ekle:
   ```csharp
   public bool ProcessFrame(Mat frame)
   {
       Console.WriteLine($"[DEBUG] ProcessFrame called: {_cameraId}");
       // ... rest of code
   }
   ```

## Hızlı Test:

Şu kodu `StreamCapture_FrameReceived` başına ekle:

```csharp
Console.WriteLine($"[DEBUG] Frame received, processing... {CameraId}");
```

Eğer bu log görünmüyorsa → **Frame event fırlatılmıyor**
Eğer bu log görünüyorsa → **Motion detection problemi**

## Muhtemel Nedenler:

1. ❌ Build yapılmadı
2. ❌ Eski instance çalışıyor
3. ❌ MotionThreshold çok yüksek (>25%)
4. ❌ Frame event subscribe edilmedi
5. ❌ Exception oluşuyor (console'da ERROR log var mı?)

## Acil Çözüm:

Motion threshold'u geçici olarak 0'a çek (her frame motion olarak algılanır):

```csharp
// CameraWorkerV2.cs constructor
_motionDetector = new MotionDetector(cameraId, 
    0.0,  // Threshold = 0 (her frame motion)
    SystemParameters.MotionDebounceMs);
```

Rebuild → Restart → Test
