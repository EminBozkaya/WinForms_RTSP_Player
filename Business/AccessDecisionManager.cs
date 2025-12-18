using System;
using WinForms_RTSP_Player.Data;
using WinForms_RTSP_Player.Utilities;

namespace WinForms_RTSP_Player.Business
{
    /// <summary>
    /// Merkezi erişim karar yöneticisi - Thread-safe ve direction-aware
    /// </summary>
    public class AccessDecisionManager
    {
        private static readonly Lazy<AccessDecisionManager> _instance =
            new Lazy<AccessDecisionManager>(() => new AccessDecisionManager());

        public static AccessDecisionManager Instance => _instance.Value;

        private readonly object _lockObject = new object();

        // Global Gate Lock state
        private bool _isGateLockActiveGlobal = false;
        private DateTime _lastGateTriggerTimeGlobal = DateTime.MinValue;

        // Last processed plates for same-plate deduplication (direction-aware)
        private string _lastProcessedPlateIN = "";
        private string _lastProcessedPlateOUT = "";

        // Unauthorized cooldown tracking (direction-aware)
        private string _lastUnauthorizedPlateIN = "";
        private DateTime _lastUnauthorizedLogTimeIN = DateTime.MinValue;
        private string _lastUnauthorizedPlateOUT = "";
        private DateTime _lastUnauthorizedLogTimeOUT = DateTime.MinValue;

        // Global plate tracking (cross-direction duplicate prevention)
        private string _lastProcessedPlateGlobal = "";
        private string _lastProcessedDirectionGlobal = "";
        private DateTime _lastProcessedTimeGlobal = DateTime.MinValue;

        private const int UNAUTHORIZED_COOLDOWN_SECONDS = 60;
        private const int GATE_LOCK_SECONDS = 45;
        private const int CROSS_DIRECTION_COOLDOWN_SECONDS = 45;

        private AccessDecisionManager()
        {
            DatabaseManager.Instance.LogSystem("INFO", 
                "AccessDecisionManager başlatıldı", 
                "AccessDecisionManager.Constructor");
        }

        /// <summary>
        /// Plaka tespiti için karar ver
        /// </summary>
        public AccessDecision ProcessPlateDetection(string plate, string direction, double confidence)
        {
            lock (_lockObject)
            {
                try
                {
                    // 1. Plaka formatını düzelt
                    string correctedPlate = PlateSanitizer.ValidateTurkishPlateFormat(plate);

                    // 2. Yetkilendirme kontrolü
                    bool isAuthorized = DatabaseManager.Instance.IsPlateAuthorized(correctedPlate);

                    // 3. Confidence threshold (yetkili için daha düşük)
                    float confidenceThreshold = isAuthorized ? 70f : 75f;

                    if (confidence < confidenceThreshold)
                    {
                        return new AccessDecision
                        {
                            Plate = correctedPlate,
                            Direction = direction,
                            Action = AccessAction.Ignore,
                            Reason = $"Düşük güven skoru: {confidence:F1}% < {confidenceThreshold}%",
                            IsAuthorized = isAuthorized,
                            Confidence = confidence
                        };
                    }

                    // 4. Global Gate Lock kontrolü
                    // Eğer kapı zaten açıksa (herhangi bir yönden), hiçbir işlem yapma
                    if (IsGateLockActiveGlobal())
                    {
                        Console.WriteLine($"[GLOBAL LOCK] Kapı zaten açık → IGNORE: {correctedPlate} ({direction})");
                        return new AccessDecision
                        {
                            Plate = correctedPlate,
                            Direction = direction,
                            Action = AccessAction.Ignore,
                            Reason = "Fiziksel kapı zaten açık (Küresel kilit)",
                            IsAuthorized = isAuthorized,
                            Confidence = confidence
                        };
                    }

                    // 5. Cross-direction duplicate check
                    if (IsCrossDirectionDuplicate(correctedPlate, direction))
                    {
                        double secondsSinceLastGlobal = (DateTime.Now - _lastProcessedTimeGlobal).TotalSeconds;
                        Console.WriteLine($"⚠️ CROSS-DIRECTION DUPLICATE: {correctedPlate} - " +
                            $"Son işlem: {_lastProcessedDirectionGlobal} ({secondsSinceLastGlobal:F1}s önce), " +
                            $"Şimdi: {direction} - IGNORE");

                        return new AccessDecision
                        {
                            Plate = correctedPlate,
                            Direction = direction,
                            Action = AccessAction.Ignore,
                            Reason = $"Aynı araç {secondsSinceLastGlobal:F0}s önce {_lastProcessedDirectionGlobal} kamerasında işlendi",
                            IsAuthorized = isAuthorized,
                            Confidence = confidence
                        };
                    }

                    // 6. Direction-specific logic
                    if (direction == "IN")
                    {
                        return ProcessINDirection(correctedPlate, isAuthorized, confidence);
                    }
                    else if (direction == "OUT")
                    {
                        return ProcessOUTDirection(correctedPlate, isAuthorized, confidence);
                    }
                    else
                    {
                        DatabaseManager.Instance.LogSystem("WARNING", 
                            $"Bilinmeyen yön: {direction}", 
                            "AccessDecisionManager.ProcessPlateDetection");
                        
                        return new AccessDecision
                        {
                            Plate = correctedPlate,
                            Direction = direction,
                            Action = AccessAction.Ignore,
                            Reason = "Bilinmeyen yön",
                            IsAuthorized = isAuthorized,
                            Confidence = confidence
                        };
                    }
                }
                catch (Exception ex)
                {
                    DatabaseManager.Instance.LogSystem("ERROR", 
                        "Karar verme hatası", 
                        "AccessDecisionManager.ProcessPlateDetection", 
                        ex.ToString());

                    return new AccessDecision
                    {
                        Plate = plate,
                        Direction = direction,
                        Action = AccessAction.Ignore,
                        Reason = "İşlem hatası",
                        Confidence = confidence
                    };
                }
            }
        }

        private AccessDecision ProcessINDirection(string plate, bool isAuthorized, double confidence)
        {
            // YETKİLİ ARAÇ
            if (isAuthorized)
            {
                // Kapıyı aç (Küresel kilidi aktifleştir)
                TriggerGateGlobal();
                _lastProcessedPlateIN = plate;

                // Global tracking güncelle
                UpdateGlobalTracking(plate, "IN");

                string owner = DatabaseManager.Instance.GetPlateOwner(plate);
                Console.WriteLine($"✅ Kapı Açılıyor (IN): {plate} - {owner} - {DateTime.Now}");

                return new AccessDecision
                {
                    Plate = plate,
                    Direction = "IN",
                    Action = AccessAction.Allow,
                    Reason = "Yetkili araç - giriş izni verildi",
                    IsAuthorized = true,
                    Owner = owner,
                    Confidence = confidence
                };
            }
            // YETKİSİZ ARAÇ
            else
            {
                if (IsUnauthorizedCooldownActiveIN(plate))
                {
                    double seconds = (DateTime.Now - _lastUnauthorizedLogTimeIN).TotalSeconds;
                    Console.WriteLine($"Kayıtsız AYNI Araç (IN) → {UNAUTHORIZED_COOLDOWN_SECONDS - seconds:F0} sn cooldown devam ediyor: {plate}");
                    
                    return new AccessDecision
                    {
                        Plate = plate,
                        Direction = "IN",
                        Action = AccessAction.Ignore,
                        Reason = "Yetkisiz araç - cooldown aktif",
                        IsAuthorized = false,
                        Confidence = confidence
                    };
                }

                // Yetkisiz araç - log at
                _lastUnauthorizedPlateIN = plate;
                _lastUnauthorizedLogTimeIN = DateTime.Now;
                _lastProcessedPlateIN = plate;

                // Global tracking güncelle
                UpdateGlobalTracking(plate, "IN");

                Console.WriteLine($"❌ Kayıtsız YENİ Araç LOG ATILIYOR (IN): {plate}");

                return new AccessDecision
                {
                    Plate = plate,
                    Direction = "IN",
                    Action = AccessAction.Deny,
                    Reason = "Yetkisiz araç - giriş reddedildi",
                    IsAuthorized = false,
                    Owner = "Yabancı/Tanımsız",
                    Confidence = confidence
                };
            }
        }

        private AccessDecision ProcessOUTDirection(string plate, bool isAuthorized, double confidence)
        {
            // YETKİLİ ARAÇ
            if (isAuthorized)
            {
                // Kapıyı aç (Küresel kilidi aktifleştir)
                TriggerGateGlobal();
                _lastProcessedPlateOUT = plate;

                // Global tracking güncelle
                UpdateGlobalTracking(plate, "OUT");

                string owner = DatabaseManager.Instance.GetPlateOwner(plate);
                Console.WriteLine($"✅ Kapı Açılıyor (OUT): {plate} - {owner} - {DateTime.Now}");

                return new AccessDecision
                {
                    Plate = plate,
                    Direction = "OUT",
                    Action = AccessAction.Allow,
                    Reason = "Yetkili araç - çıkış izni verildi",
                    IsAuthorized = true,
                    Owner = owner,
                    Confidence = confidence
                };
            }
            // YETKİSİZ ARAÇ
            else
            {
                if (IsUnauthorizedCooldownActiveOUT(plate))
                {
                    double seconds = (DateTime.Now - _lastUnauthorizedLogTimeOUT).TotalSeconds;
                    Console.WriteLine($"Kayıtsız AYNI Araç (OUT) → {UNAUTHORIZED_COOLDOWN_SECONDS - seconds:F0} sn cooldown devam ediyor: {plate}");

                    return new AccessDecision
                    {
                        Plate = plate,
                        Direction = "OUT",
                        Action = AccessAction.Ignore,
                        Reason = "Yetkisiz araç - cooldown aktif",
                        IsAuthorized = false,
                        Confidence = confidence
                    };
                }

                // Yetkisiz araç - log at
                _lastUnauthorizedPlateOUT = plate;
                _lastUnauthorizedLogTimeOUT = DateTime.Now;
                _lastProcessedPlateOUT = plate;

                // Global tracking güncelle
                UpdateGlobalTracking(plate, "OUT");

                Console.WriteLine($"❌ Kayıtsız YENİ Araç LOG ATILIYOR (OUT): {plate}");

                return new AccessDecision
                {
                    Plate = plate,
                    Direction = "OUT",
                    Action = AccessAction.Deny,
                    Reason = "Yetkisiz araç - çıkış reddedildi",
                    IsAuthorized = false,
                    Owner = "Yabancı/Tanımsız",
                    Confidence = confidence
                };
            }
        }

        private bool IsGateLockActiveGlobal()
        {
            if (!_isGateLockActiveGlobal)
                return false;

            double secondsSinceGateOpened = (DateTime.Now - _lastGateTriggerTimeGlobal).TotalSeconds;

            if (secondsSinceGateOpened >= GATE_LOCK_SECONDS)
            {
                _isGateLockActiveGlobal = false;
                return false;
            }

            return true;
        }

        private void TriggerGateGlobal()
        {
            _isGateLockActiveGlobal = true;
            _lastGateTriggerTimeGlobal = DateTime.Now;
        }

        private bool IsUnauthorizedCooldownActiveIN(string plate)
        {
            if (plate != _lastUnauthorizedPlateIN)
                return false;

            double seconds = (DateTime.Now - _lastUnauthorizedLogTimeIN).TotalSeconds;
            return seconds < UNAUTHORIZED_COOLDOWN_SECONDS;
        }

        private bool IsUnauthorizedCooldownActiveOUT(string plate)
        {
            if (plate != _lastUnauthorizedPlateOUT)
                return false;

            double seconds = (DateTime.Now - _lastUnauthorizedLogTimeOUT).TotalSeconds;
            return seconds < UNAUTHORIZED_COOLDOWN_SECONDS;
        }

        private bool IsCrossDirectionDuplicate(string plate, string direction)
        {
            // Aynı plaka değilse duplicate değil
            if (plate != _lastProcessedPlateGlobal)
                return false;

            // Aynı direction ise cross-direction duplicate değil (normal duplicate kontrolü yapılacak)
            if (direction == _lastProcessedDirectionGlobal)
                return false;

            // Farklı direction ve 45 saniye dolmadıysa cross-direction duplicate
            double seconds = (DateTime.Now - _lastProcessedTimeGlobal).TotalSeconds;
            return seconds < CROSS_DIRECTION_COOLDOWN_SECONDS;
        }

        private void UpdateGlobalTracking(string plate, string direction)
        {
            _lastProcessedPlateGlobal = plate;
            _lastProcessedDirectionGlobal = direction;
            _lastProcessedTimeGlobal = DateTime.Now;
        }
    }

    /// <summary>
    /// Erişim kararı sonucu
    /// </summary>
    public class AccessDecision
    {
        public string Plate { get; set; }
        public string Direction { get; set; }
        public AccessAction Action { get; set; }
        public string Reason { get; set; }
        public bool IsAuthorized { get; set; }
        public string Owner { get; set; }
        public double Confidence { get; set; }
    }

    /// <summary>
    /// Erişim aksiyonu
    /// </summary>
    public enum AccessAction
    {
        Allow,      // Kapıyı aç, log at
        Deny,       // Kapıyı açma, log at
        Ignore      // Hiçbir şey yapma (cooldown, düşük confidence, vb.)
    }
}
