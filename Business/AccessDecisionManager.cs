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
                    //Şayet doğruluk, min değerin de altında ise diğer işlemlere gerek yok. (min doğruluk = kayıtlı araçlar için tahsis edilen doğruluk yüzdesi)
                    if (confidence < SystemParameters.AuthorizedConfidenceThreshold || plate!.Length < 6)
                    {
                        return new AccessDecision
                        {
                            Plate = plate,
                            Direction = direction,
                            Action = AccessAction.Ignore,
                            Reason = $"Düşük güven skoru: {confidence:F1}% < {SystemParameters.AuthorizedConfidenceThreshold}%",
                            IsAuthorized = false,
                            Confidence = confidence
                        };
                    }

                    // 1. Plaka formatını düzelt
                    string correctedPlate = PlateSanitizer.ValidateTurkishPlateFormat(plate);

                    // 2. Yetkilendirme kontrolü
                    //bool isAuthorized = DatabaseManager.Instance.IsPlateAuthorized(correctedPlate);
                    bool isAuthorized = DatabaseManager.Instance.TryAuthorizePlate(
                                            correctedPlate,
                                            out string matchedDbPlate
                                        );

                    // 3. Confidence threshold (yetkili için daha düşük)
                    float confidenceThreshold = isAuthorized 
                        ? SystemParameters.AuthorizedConfidenceThreshold 
                        : SystemParameters.UnAuthorizedConfidenceThreshold;

                    

                    // 4. Global Gate Lock kontrolü
                    // Eğer kapı zaten açıksa (herhangi bir yönden), hiçbir işlem yapma
                    if (IsGateLockActiveGlobal())
                    {
#if DEBUG
                        Console.WriteLine($"[{DateTime.Now}] [GLOBAL LOCK] Kapı zaten açık → IGNORE: {correctedPlate} ({direction})");
#endif
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
#if DEBUG
                        Console.WriteLine($"[{DateTime.Now}] ⚠️ CROSS-DIRECTION DUPLICATE: {correctedPlate} - " +
                            $"Son işlem: {_lastProcessedDirectionGlobal} ({secondsSinceLastGlobal:F1}s önce), " +
                            $"Şimdi: {direction} - IGNORE");
#endif

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
                        return ProcessINDirection(matchedDbPlate, isAuthorized, confidence, correctedPlate);
                    }
                    else if (direction == "OUT")
                    {
                        return ProcessOUTDirection(matchedDbPlate, isAuthorized, confidence, correctedPlate);
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

        private AccessDecision ProcessINDirection(string plate, bool isAuthorized, double confidence, string ocrPlate)
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
                Guid gateOpId = Guid.NewGuid(); // 2. Gate Trigger Idempotency Token

                // 5. Canonical Log
#if DEBUG
                string ocrInfo = ocrPlate != plate ? $"OCR:{ocrPlate}" : "";
                Console.WriteLine($"[PLATE] {plate} | IN | AUTH | CONF:{confidence:F1} | ACTION:ALLOW | ID:{gateOpId} | OWNER:{owner} | {ocrInfo}");
#endif

                return new AccessDecision
                {
                    Plate = plate,
                    OcrPlate = ocrPlate,
                    Direction = "IN",
                    Action = AccessAction.Allow,
                    Reason = "Yetkili araç - giriş izni verildi",
                    IsAuthorized = true,
                    Owner = owner,
                    Confidence = confidence,
                    GateOpId = gateOpId
                };
            }
            // YETKİSİZ ARAÇ
            else
            {
                if (IsUnauthorizedCooldownActiveIN(ocrPlate))
                {
                    double seconds = (DateTime.Now - _lastUnauthorizedLogTimeIN).TotalSeconds;
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] Kayıtsız AYNI Araç (IN) → {SystemParameters.UNAUTHORIZED_COOLDOWN_SECONDS - seconds:F0} sn cooldown devam ediyor: {ocrPlate}");
#endif
                    
                    return new AccessDecision
                    {
                        Plate = ocrPlate,
                        Direction = "IN",
                        Action = AccessAction.Ignore,
                        Reason = "Yetkisiz araç - cooldown aktif",
                        IsAuthorized = false,
                        Confidence = confidence
                    };
                }

                // Yetkisiz araç - log at
                _lastUnauthorizedPlateIN = ocrPlate;
                _lastUnauthorizedLogTimeIN = DateTime.Now;
                _lastProcessedPlateIN = ocrPlate;

                // Global tracking güncelle
                UpdateGlobalTracking(ocrPlate, "IN");

#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] ❌ Kayıtsız YENİ Araç LOG ATILIYOR (IN): {ocrPlate}");
#endif

                return new AccessDecision
                {
                    Plate = ocrPlate,
                    Direction = "IN",
                    Action = AccessAction.Deny,
                    Reason = "Yetkisiz araç - giriş reddedildi",
                    IsAuthorized = false,
                    Owner = "Yabancı/Tanımsız",
                    Confidence = confidence
                };
            }
        }

        private AccessDecision ProcessOUTDirection(string plate, bool isAuthorized, double confidence, string ocrPlate)
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
                Guid gateOpId = Guid.NewGuid(); // 2. Gate Trigger Idempotency Token

                // 5. Canonical Log
#if DEBUG
                string ocrInfo = ocrPlate != plate ? $"OCR:{ocrPlate}" : "";
                Console.WriteLine($"[PLATE] {plate} | OUT | AUTH | CONF:{confidence:F1} | ACTION:ALLOW | ID:{gateOpId} | OWNER:{owner} | {ocrInfo}");
#endif

                return new AccessDecision
                {
                    Plate = plate,
                    OcrPlate = ocrPlate,
                    Direction = "OUT",
                    Action = AccessAction.Allow,
                    Reason = "Yetkili araç - çıkış izni verildi",
                    IsAuthorized = true,
                    Owner = owner,
                    Confidence = confidence,
                    GateOpId = gateOpId
                };
            }
            // YETKİSİZ ARAÇ
            else
            {
                if (IsUnauthorizedCooldownActiveOUT(ocrPlate))
                {
                    double seconds = (DateTime.Now - _lastUnauthorizedLogTimeOUT).TotalSeconds;
#if DEBUG
                    Console.WriteLine($"[{DateTime.Now}] Kayıtsız AYNI Araç (OUT) → {SystemParameters.UNAUTHORIZED_COOLDOWN_SECONDS - seconds:F0} sn cooldown devam ediyor: {ocrPlate}");
#endif

                    return new AccessDecision
                    {
                        Plate = ocrPlate,
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
                UpdateGlobalTracking(ocrPlate, "OUT");

#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] ❌ Kayıtsız YENİ Araç LOG ATILIYOR (OUT): {ocrPlate}");
#endif

                return new AccessDecision
                {
                    Plate = ocrPlate,
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

            if (secondsSinceGateOpened >= SystemParameters.GATE_LOCK_SECONDS)
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
            return seconds < SystemParameters.UNAUTHORIZED_COOLDOWN_SECONDS;
        }

        private bool IsUnauthorizedCooldownActiveOUT(string plate)
        {
            if (plate != _lastUnauthorizedPlateOUT)
                return false;

            double seconds = (DateTime.Now - _lastUnauthorizedLogTimeOUT).TotalSeconds;
            return seconds < SystemParameters.UNAUTHORIZED_COOLDOWN_SECONDS;
        }

        private bool IsCrossDirectionDuplicate(string plate, string direction)
        {
            // Aynı plaka değilse duplicate değil
            if (plate != _lastProcessedPlateGlobal)
                return false;

            // Aynı direction ise cross-direction duplicate değil (normal duplicate kontrolü yapılacak)
            if (direction == _lastProcessedDirectionGlobal)
                return false;

            // Farklı direction ve belirlenen süre dolmadıysa cross-direction duplicate
            double seconds = (DateTime.Now - _lastProcessedTimeGlobal).TotalSeconds;
            return seconds < SystemParameters.CROSS_DIRECTION_COOLDOWN_SECONDS;
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
        public string? OcrPlate { get; set; }
        public string Direction { get; set; }
        public AccessAction Action { get; set; }
        public string Reason { get; set; }
        public bool IsAuthorized { get; set; }
        public string Owner { get; set; }
        public double Confidence { get; set; }
        public Guid? GateOpId { get; set; } // Veritabanı izlenebilirliği için
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
