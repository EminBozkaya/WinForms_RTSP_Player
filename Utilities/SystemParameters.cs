using WinForms_RTSP_Player.Data;

namespace WinForms_RTSP_Player.Utilities
{
    /// <summary>
    /// Sistem genelinde kullanılan parametrelerin tek noktadan yönetimi.
    /// Uygulama başlangıcında bir kez yüklenecek ve runtime boyunca sabit kalacak.
    /// </summary>
    public static class SystemParameters
    {
        private static bool _isLoaded = false;

        // Kamera & Akış Ayarları
        public static int FrameCaptureTimerInterval { get; private set; } = 1000; //1 sn - Görüntü Yakalama Zaman Aralığı
        public static int StreamHealthTimerInterval { get; private set; } = 30000; //30 sn - Kamera Yayın Görüntü Kontrol Zaman Aralığı
        public static int HeartbeatTimerInterval { get; private set; } = 300000; //5 dk - Sistem Sağlığı Kontrol Zaman Aralığı
        public static int PeriodicResetTimerInterval { get; private set; } = 600000; //10 dk - Görüntü Yeniden Başlatma Zaman Aralığı
        public static int PlateMinimumLength { get; private set; } = 7; // Minimum Plaka Karakter Sayısı (TR için)
        public static int FrameKontrolInterval { get; private set; } = 10; //10 sn - Kamera Frame Kontrol Zaman Aralığı

        // UI Gösterim Süreleri
        public static int AuthorizedPlateShowTime { get; private set; } = 45000; //45 sn - Kayıtlı Araç Plaka Gösterim Süresi
        public static int UnAuthorizedPlateShowTime { get; private set; } = 10000; //10 sn - Kayıtsız Araç Plaka Gösterim Süresi

        // Kayıt Gösterim Limitleri
        public static int GetAccessLogLimit { get; private set; } = 1000; //Araç Giriş-Çıkış Kayıt Gösterim Limiti
        public static int GetSystemLogLimit { get; private set; } = 1000; //Sistem Kayıt Gösterim Limiti

        // Erişim Karar Parametreleri
        public static int UNAUTHORIZED_COOLDOWN_SECONDS { get; private set; } = 60; //Kayıtsız Aynı Araç Log Kaydı Bekleme Süresi
        public static int GATE_LOCK_SECONDS { get; private set; } = 45; //Kapı Açılma Bekleme Süresi
        public static int CROSS_DIRECTION_COOLDOWN_SECONDS { get; private set; } = 45; //Aynı Araç Giriş-Çıkış Bekleme Süresi
        public static float AuthorizedConfidenceThreshold { get; private set; } = 70f; //Kayıtlı Araç Plaka Okuma Doğruluğu Yüzdelik Eşiği
        public static float UnAuthorizedConfidenceThreshold { get; private set; } = 75f; //Kayıtsız Araç Plaka Okuma Doğruluğu Yüzdelik Eşiği

        /// <summary>
        /// Parametreleri veritabanından bir kez yükler. Hatalı okumalarda varsayılan değerler korunur.
        /// </summary>
        public static void Load()
        {
            if (_isLoaded)
                return;

            try
            {
                var db = DatabaseManager.Instance;

                FrameCaptureTimerInterval = GetInt(db, "FrameCaptureTimerInterval", FrameCaptureTimerInterval);
                StreamHealthTimerInterval = GetInt(db, "StreamHealthTimerInterval", StreamHealthTimerInterval);
                HeartbeatTimerInterval = GetInt(db, "HeartbeatTimerInterval", HeartbeatTimerInterval);
                PeriodicResetTimerInterval = GetInt(db, "PeriodicResetTimerInterval", PeriodicResetTimerInterval);
                PlateMinimumLength = GetInt(db, "PlateMinimumLength", PlateMinimumLength);
                FrameKontrolInterval = GetInt(db, "FrameKontrolInterval", FrameKontrolInterval);

                AuthorizedPlateShowTime = GetInt(db, "AuthorizedPlateShowTime", AuthorizedPlateShowTime);
                UnAuthorizedPlateShowTime = GetInt(db, "UnAuthorizedPlateShowTime", UnAuthorizedPlateShowTime);

                GetAccessLogLimit = GetInt(db, "GetAccessLogLimit", GetAccessLogLimit);
                GetSystemLogLimit = GetInt(db, "GetSystemLogLimit", GetSystemLogLimit);

                UNAUTHORIZED_COOLDOWN_SECONDS = GetInt(db, "UNAUTHORIZED_COOLDOWN_SECONDS", UNAUTHORIZED_COOLDOWN_SECONDS);
                GATE_LOCK_SECONDS = GetInt(db, "GATE_LOCK_SECONDS", GATE_LOCK_SECONDS);
                CROSS_DIRECTION_COOLDOWN_SECONDS = GetInt(db, "CROSS_DIRECTION_COOLDOWN_SECONDS", CROSS_DIRECTION_COOLDOWN_SECONDS);

                AuthorizedConfidenceThreshold = GetFloat(db, "AuthorizedConfidenceThreshold", AuthorizedConfidenceThreshold);
                UnAuthorizedConfidenceThreshold = GetFloat(db, "UnAuthorizedConfidenceThreshold", UnAuthorizedConfidenceThreshold);

                _isLoaded = true;
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

        private static float GetFloat(DatabaseManager db, string name, float defaultValue)
        {
            string raw = db.GetSystemParameter(name, defaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
                return value;

            return defaultValue;
        }
    }
}
