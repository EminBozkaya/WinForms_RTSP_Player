using System;
using WinForms_RTSP_Player.Data;

namespace WinForms_RTSP_Player.Utilities
{
    /// <summary>
    /// Sistem genelinde kullanılan parametrelerin tek noktadan yönetimi.
    /// Uygulama başlangıcında bir kez yüklenecek ve runtime boyunca sabit kalacak.
    /// </summary>
    public static class SystemParameters
    {
        // Kamera & Akış Ayarları (ms cinsinden kullanılır, DB'de saniye olarak saklanır)
        public static int FrameCaptureTimerInterval { get; private set; } = 1000; //1 sn - Görüntü Yakalama Zaman Aralığı
        public static int StreamHealthTimerInterval { get; private set; } = 5000; //5 sn - Kamera Yayın Görüntü Kontrol Zaman Aralığı
        public static int HeartbeatTimerInterval { get; private set; } = 60000; //1 dk - Sistem Sağlığı Kontrol Zaman Aralığı
        public static int PeriodicResetTimerInterval { get; private set; } = 7200000; //2 saat - Görüntü Yeniden Başlatma Zaman Aralığı
        public static int PlateMinimumLength { get; private set; } = 6; // Minimum Plaka Karakter Sayısı (TR için min 7 fakat, 7'de 6 karakter, 8'de 7 doğru eşleşmeyi kabul etmek için min 6 ayarladık!)
        public static int FrameKontrolInterval { get; private set; } = 6; //6 sn - Kamera Frame Kontrol Zaman Aralığı

        // UI Gösterim Süreleri (ms cinsinden kullanılır, DB'de saniye olarak saklanır)
        public static int AuthorizedPlateShowTime { get; private set; } = 45000; //45 sn - Kayıtlı Araç Plaka Gösterim Süresi
        public static int UnAuthorizedPlateShowTime { get; private set; } = 10000; //10 sn - Kayıtsız Araç Plaka Gösterim Süresi

        // Kayıt Gösterim Limitleri
        public static int GetAccessLogLimit { get; private set; } = 1000; //Araç Giriş-Çıkış Kayıt Gösterim Limiti
        public static int GetSystemLogLimit { get; private set; } = 3000; //Sistem Kayıt Gösterim Limiti

        // Erişim Karar Parametreleri
        public static int UNAUTHORIZED_COOLDOWN_SECONDS { get; private set; } = 60; //60 sn - Kayıtsız Aynı Araç Log Kaydı Bekleme Süresi
        public static int GATE_LOCK_SECONDS { get; private set; } = 45; //45 sn - Kapı Açılma Bekleme Süresi
        public static int CROSS_DIRECTION_COOLDOWN_SECONDS { get; private set; } = 45; //45 sn - Aynı Araç Giriş-Çıkış Bekleme Süresi
        public static float AuthorizedConfidenceThreshold { get; private set; } = 65f; //Kayıtlı Araç Plaka Okuma Doğruluk Eşiği (%)
        public static float UnAuthorizedConfidenceThreshold { get; private set; } = 75f; //Kayıtsız Araç Plaka Okuma Doğruluk Eşiği (%)

        /// <summary>
        /// Parametreleri veritabanından bir kez yükler. Hatalı okumalarda varsayılan değerler korunur.
        /// </summary>
        public static void Load()
        {
            try
            {
                var db = DatabaseManager.Instance;

                // Zaman parametrelerini DB'de saniye, kod tarafında ms olarak kullan
                FrameCaptureTimerInterval = GetSecondsAsMilliseconds(db, "FrameCaptureTimerInterval", 1);   // 1 sn
                StreamHealthTimerInterval = GetSecondsAsMilliseconds(db, "StreamHealthTimerInterval", 30);  // 30 sn
                HeartbeatTimerInterval = GetSecondsAsMilliseconds(db, "HeartbeatTimerInterval", 300);       // 5 dk
                PeriodicResetTimerInterval = GetSecondsAsMilliseconds(db, "PeriodicResetTimerInterval", 600); // 10 dk
                PlateMinimumLength = GetInt(db, "PlateMinimumLength", PlateMinimumLength);
                FrameKontrolInterval = GetInt(db, "FrameKontrolInterval", FrameKontrolInterval);

                AuthorizedPlateShowTime = GetSecondsAsMilliseconds(db, "AuthorizedPlateShowTime", 45);      // 45 sn
                UnAuthorizedPlateShowTime = GetSecondsAsMilliseconds(db, "UnAuthorizedPlateShowTime", 10);  // 10 sn

                GetAccessLogLimit = GetInt(db, "GetAccessLogLimit", GetAccessLogLimit);
                GetSystemLogLimit = GetInt(db, "GetSystemLogLimit", GetSystemLogLimit);

                UNAUTHORIZED_COOLDOWN_SECONDS = GetInt(db, "UNAUTHORIZED_COOLDOWN_SECONDS", UNAUTHORIZED_COOLDOWN_SECONDS);
                GATE_LOCK_SECONDS = GetInt(db, "GATE_LOCK_SECONDS", GATE_LOCK_SECONDS);
                CROSS_DIRECTION_COOLDOWN_SECONDS = GetInt(db, "CROSS_DIRECTION_COOLDOWN_SECONDS", CROSS_DIRECTION_COOLDOWN_SECONDS);

                AuthorizedConfidenceThreshold = GetFloat(db, "AuthorizedConfidenceThreshold", AuthorizedConfidenceThreshold);
                UnAuthorizedConfidenceThreshold = GetFloat(db, "UnAuthorizedConfidenceThreshold", UnAuthorizedConfidenceThreshold);
            }
            catch (Exception ex)
            {
                // Bu aşamada veritabanına log atmak riskli olabilir, en azından konsola yazalım
                Console.WriteLine($"[{DateTime.Now}] SystemParameters.Load hatası: {ex.Message}");
            }
        }

        private static int GetInt(DatabaseManager db, string name, int defaultValue)
        {
            string raw = db.GetSystemParameter(name, defaultValue.ToString());
            if (int.TryParse(raw, out int value))
                return value;

            return defaultValue;
        }

        /// <summary>
        /// DB'de saniye olarak tutulan değeri ms'e çevirir.
        /// </summary>
        private static int GetSecondsAsMilliseconds(DatabaseManager db, string name, int defaultSeconds)
        {
            string raw = db.GetSystemParameter(name, defaultSeconds.ToString());
            if (int.TryParse(raw, out int value))
            {
                return value * 1000;
            }

            return defaultSeconds * 1000;
        }

        private static float GetFloat(DatabaseManager db, string name, float defaultValue)
        {
            string raw = db.GetSystemParameter(name, defaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
                return value;

            return defaultValue;
        }
    }
}
