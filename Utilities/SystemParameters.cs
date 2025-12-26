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
        public static int FrameCaptureTimerInterval { get; private set; } = 2000; //2 sn - Görüntü Yakalama Zaman Aralığı
        public static int StreamHealthTimerInterval { get; private set; } = 5000; //5 sn - Kamera Yayın Görüntü Kontrol Zaman Aralığı
        public static int HeartbeatTimerInterval { get; private set; } = 60000; //1 dk - Sistem Sağlığı Kontrol Zaman Aralığı
        public static int PeriodicResetTimerInterval { get; private set; } = 7200000; //2 saat - Görüntü Yeniden Başlatma Zaman Aralığı
        public static int PlateMinimumLength { get; private set; } = 6; // Minimum Plaka Karakter Sayısı
        public static double FrameKontrolInterval { get; private set; } = 6.0; //6 sn - Kamera Frame Kontrol Zaman Aralığı (Double destekli)

        // UI Gösterim Süreleri (ms cinsinden kullanılır, DB'de saniye olarak saklanır)
        public static int AuthorizedPlateShowTime { get; private set; } = 45000; //45 sn - Kayıtlı Araç Plaka Gösterim Süresi
        public static int UnAuthorizedPlateShowTime { get; private set; } = 10000; //10 sn - Kayıtsız Araç Plaka Gösterim Süresi

        // Kayıt Gösterim Limitleri
        public static int GetAccessLogLimit { get; private set; } = 3000; //Araç Giriş-Çıkış Kayıt Gösterim Limiti
        public static int GetSystemLogLimit { get; private set; } = 3000; //Sistem Kayıt Gösterim Limiti
        public static int LogDisplayDays { get; private set; } = 3; // Varsayılan Log Gösterim Son Gün Sayısı (geçmiş)

        // Erişim Karar Parametreleri
        public static int UNAUTHORIZED_COOLDOWN_SECONDS { get; private set; } = 60; //60 sn - Kayıtsız Aynı Araç Log Kaydı Bekleme Süresi
        public static int GATE_LOCK_SECONDS { get; private set; } = 45; //45 sn - Kapı Açılma Bekleme Süresi
        public static int CROSS_DIRECTION_COOLDOWN_SECONDS { get; private set; } = 45; //45 sn - Aynı Araç Giriş-Çıkış Bekleme Süresi
        public static float AuthorizedConfidenceThreshold { get; private set; } = 65f; //Kayıtlı Araç Plaka Okuma Doğruluk Eşiği (%)
        public static float UnAuthorizedConfidenceThreshold { get; private set; } = 85f; //Kayıtsız Araç Plaka Okuma Doğruluk Eşiği (%)

        // Veri Temizleme Parametreleri
        public static int LogRetentionDays { get; private set; } = 15; //Logların Saklanacağı Gün Sayısı (Varsayılan: 15)

        /// <summary>
        /// Parametreleri veritabanından bir kez yükler. Hatalı okumalarda varsayılan değerler korunur.
        /// </summary>
        public static void Load()
        {
            try
            {
                var db = DatabaseManager.Instance;

                // Zaman parametrelerini DB'de saniye, kod tarafında ms olarak kullan
                FrameCaptureTimerInterval = GetSecondsAsMilliseconds(db, "FrameCaptureTimerInterval", 2);   // 2 sn
                StreamHealthTimerInterval = GetSecondsAsMilliseconds(db, "StreamHealthTimerInterval", 30);  // 30 sn
                HeartbeatTimerInterval = GetSecondsAsMilliseconds(db, "HeartbeatTimerInterval", 300);       // 5 dk
                PeriodicResetTimerInterval = GetSecondsAsMilliseconds(db, "PeriodicResetTimerInterval", 600); // 10 dk
                PlateMinimumLength = GetInt(db, "PlateMinimumLength", PlateMinimumLength);
                FrameKontrolInterval = GetDouble(db, "FrameKontrolInterval", FrameKontrolInterval);

                AuthorizedPlateShowTime = GetSecondsAsMilliseconds(db, "AuthorizedPlateShowTime", 45);      // 45 sn
                UnAuthorizedPlateShowTime = GetSecondsAsMilliseconds(db, "UnAuthorizedPlateShowTime", 10);  // 10 sn

                GetAccessLogLimit = GetInt(db, "GetAccessLogLimit", GetAccessLogLimit);
                GetSystemLogLimit = GetInt(db, "GetSystemLogLimit", GetSystemLogLimit);

                UNAUTHORIZED_COOLDOWN_SECONDS = GetInt(db, "UNAUTHORIZED_COOLDOWN_SECONDS", UNAUTHORIZED_COOLDOWN_SECONDS);
                GATE_LOCK_SECONDS = GetInt(db, "GATE_LOCK_SECONDS", GATE_LOCK_SECONDS);
                CROSS_DIRECTION_COOLDOWN_SECONDS = GetInt(db, "CROSS_DIRECTION_COOLDOWN_SECONDS", CROSS_DIRECTION_COOLDOWN_SECONDS);

                AuthorizedConfidenceThreshold = GetFloat(db, "AuthorizedConfidenceThreshold", AuthorizedConfidenceThreshold);
                UnAuthorizedConfidenceThreshold = GetFloat(db, "UnAuthorizedConfidenceThreshold", UnAuthorizedConfidenceThreshold);

                LogDisplayDays = GetInt(db, "LogDisplayDays", 3);

                // LogRetentionDays artık otomatik OLUŞTURULMAZ. Eğer DB'de yoksa default (15) döner ama DB'ye yazmaz.
                LogRetentionDays = GetInt(db, "LogRetentionDays", 15);
            }
            catch (Exception ex)
            {
                // Bu aşamada veritabanına log atmak riskli olabilir, en azından konsola yazalım
                Console.WriteLine($"[{DateTime.Now}] SystemParameters.Load hatası: {ex.Message}");
            }
        }

        private static int GetInt(DatabaseManager db, string name, int defaultValue, bool autoCreate = true)
        {
            string raw = db.GetSystemParameter(name, defaultValue.ToString(), null, autoCreate);
            if (int.TryParse(raw, out int value))
                return value;

            return defaultValue;
        }

        /// <summary>
        /// DB'de saniye (ondalıklı olabilir) olarak tutulan değeri ms'e çevirir.
        /// </summary>
        private static int GetSecondsAsMilliseconds(DatabaseManager db, string name, double defaultSeconds)
        {
            string raw = db.GetSystemParameter(name, defaultSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double value))
            {
                return (int)(value * 1000);
            }

            return (int)(defaultSeconds * 1000);
        }

        private static double GetDouble(DatabaseManager db, string name, double defaultValue)
        {
            string raw = db.GetSystemParameter(name, defaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double value))
                return value;

            return defaultValue;
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
