using Microsoft.Data.Sqlite;
using System;
using System.Data;
using System.IO;

namespace WinForms_RTSP_Player.Data
{
    public class DatabaseManager
    {
        private static DatabaseManager _instance;
        public static DatabaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DatabaseManager();
                }
                return _instance;
            }
        }
        private readonly string _connectionString;
        private readonly string _dbPath;

        public DatabaseManager()
        {
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlateDatabase.db");
            _connectionString = $"Data Source={_dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(_dbPath))
            {
                CreateDatabase();
            }
            ////Bazı Tablolarda sütun ekleme çıkarma yaptığımzda kullanılacak ve ilgili metot düzeltilecek!!
            //else
            //{
            //    UpdateSchema();
            //}
        }

        private void UpdateSchema()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    
                    // AccessLog tablosunda PlateOwner kontrolü
                    bool columnExists = false;
                    using (var command = new SqliteCommand("PRAGMA table_info(AccessLog)", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader["name"].ToString() == "PlateOwner")
                                {
                                    columnExists = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!columnExists)
                    {
                        using (var command = new SqliteCommand("ALTER TABLE AccessLog ADD COLUMN PlateOwner TEXT", connection))
                        {
                            command.ExecuteNonQuery();
                        }
                        //Console.WriteLine("Şema güncellendi: PlateOwner kolonu eklendi.");
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Şema güncelleme hatası: {ex.Message}");
#endif
            }
        }

        private void CreateDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                
                // Plaka tablosu - izinli plakalar
                string createPlatesTable = @"
                    CREATE TABLE Plates (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PlateNumber TEXT UNIQUE NOT NULL,
                        OwnerName TEXT,
                        VehicleType TEXT,
                        IsActive INTEGER DEFAULT 1,
                        CreatedDate DATETIME DEFAULT (datetime('now', 'localtime')),
                        UpdatedDate DATETIME DEFAULT (datetime('now', 'localtime'))
                    )";

                // Giriş/çıkış log tablosu
                string createAccessLogTable = @"
                    CREATE TABLE AccessLog (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PlateNumber TEXT NOT NULL,
                        PlateOwner TEXT,
                        AccessType TEXT NOT NULL, -- 'IN' veya 'OUT'
                        AccessTime DATETIME DEFAULT (datetime('now', 'localtime')),
                        IsAuthorized INTEGER DEFAULT 0,
                        Confidence REAL,
                        Notes TEXT
                    )";

                // Sistem log tablosu
                string createSystemLogTable = @"
                    CREATE TABLE SystemLog (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        LogLevel TEXT NOT NULL, -- 'INFO', 'WARNING', 'ERROR'
                        Message TEXT NOT NULL,
                        Component TEXT,
                        LogTime DATETIME DEFAULT (datetime('now', 'localtime')),
                        Details TEXT
                    )";

                // Sistem parametreleri tablosu
                string createSystemParameterTable = @"
                    CREATE TABLE SystemParameter (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT UNIQUE NOT NULL,
                        Value TEXT NOT NULL,
                        Detail TEXT,
                        CreatedDate DATETIME DEFAULT (datetime('now', 'localtime')),
                        UpdatedDate DATETIME DEFAULT (datetime('now', 'localtime'))
                    )";

                using (var command = new SqliteCommand(createPlatesTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqliteCommand(createAccessLogTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqliteCommand(createSystemLogTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqliteCommand(createSystemParameterTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Sistem default parametreleri ekle
                InsertSystemDefaultParameters();

                //// Örnek plaka verileri ekle
                //InsertSamplePlates();
            }
        }

        private void InsertSystemDefaultParameters()
        {
            try
            {
                // Standart Parametreler ve değerleri:
                // Kamera & Akış Ayarları (DB'de saniye olarak saklanır)
                AddSystemParameter("FrameCaptureTimerInterval", "1", "Görüntü Yakalama Zaman Aralığı (saniye)");
                AddSystemParameter("StreamHealthTimerInterval", "30", "Kamera Yayın Görüntü Kontrol Zaman Aralığı (saniye)");
                AddSystemParameter("HeartbeatTimerInterval", "300", "Sistem Sağlığı Kontrol Zaman Aralığı (saniye)");
                AddSystemParameter("PeriodicResetTimerInterval", "600", "Görüntü Yeniden Başlatma Zaman Aralığı (saniye)");
                AddSystemParameter("PlateMinimumLength", "7", "Minimum Plaka Karakter Sayısı");
                AddSystemParameter("FrameKontrolInterval", "10", "Kamera Frame Kontrol Zaman Aralığı (saniye)");

                // UI Gösterim Süreleri (DB'de saniye olarak saklanır)
                AddSystemParameter("AuthorizedPlateShowTime", "45", "Kayıtlı Araç Plaka Gösterim Süresi (saniye)");
                AddSystemParameter("UnAuthorizedPlateShowTime", "10", "Kayıtsız Araç Plaka Gösterim Süresi (saniye)");

                // Kayıt Gösterim Limitleri
                AddSystemParameter("GetAccessLogLimit", "1000", "Araç Giriş-Çıkış Kayıt Gösterim Limiti (adet)");
                AddSystemParameter("GetSystemLogLimit", "1000", "Sistem Kayıt Gösterim Limiti (adet)");

                // Erişim Karar Parametreleri
                AddSystemParameter("UNAUTHORIZED_COOLDOWN_SECONDS", "60", "Kayıtsız Aynı Araç Log Kaydı Bekleme Süresi (saniye)");
                AddSystemParameter("GATE_LOCK_SECONDS", "45", "Kapı Açılma Bekleme Süresi (saniye)");
                AddSystemParameter("CROSS_DIRECTION_COOLDOWN_SECONDS", "45", "Aynı Araç Giriş-Çıkış Bekleme Süresi (saniye)");
                AddSystemParameter("AuthorizedConfidenceThreshold", "70", "Kayıtlı Araç Plaka Okuma Doğruluk Eşiği (%)");
                AddSystemParameter("UnAuthorizedConfidenceThreshold", "75", "Kayıtsız Araç Plaka Okuma Doğruluk Eşiği (%)");
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Default sistem parametreleri ekleme hatası: {ex.Message}");
#endif
            }
        }

        public bool AddSystemParameter(string parameterName, string parameterValue, string parameterDetail)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO SystemParameter (Name, Value, Detail, CreatedDate, UpdatedDate) 
                        VALUES (@Name, @Value, @Detail, datetime('now', 'localtime'), datetime('now', 'localtime'))";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", parameterName);
                        command.Parameters.AddWithValue("@Value", parameterValue);
                        command.Parameters.AddWithValue("@Detail", parameterDetail);
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Parametre ekleme hatası: {ex.Message}");
#endif
                return false;
            }
        }

        public bool UpdateSystemParameter(int id, string parameterValue)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                        UPDATE SystemParameter 
                        SET Value = @Value,
                            UpdatedDate = datetime('now', 'localtime')
                        WHERE Id = @Id";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@Value", parameterValue);
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Parametre güncelleme hatası: {ex.Message}");
#endif
                return false;
            }
        }

        public DataTable GetAllSystemParameters()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"SELECT Id, Name, Detail, Value, CreatedDate, UpdatedDate 
                                     FROM SystemParameter 
                                     ORDER BY Name";

                    using (var command = new SqliteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        var dataTable = new DataTable();
                        dataTable.Load(reader);
                        return dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Sistem parametreleri listeleme hatası: {ex.Message}");
#endif
                return new DataTable();
            }
        }

        /// <summary>
        /// Tek bir sistem parametresini döner. Kayıt yoksa, varsa verilen varsayılan değerle oluşturur.
        /// </summary>
        public string GetSystemParameter(string parameterName, string defaultValue = null, string defaultDetail = null)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // Önce var mı diye bak
                    string selectQuery = @"SELECT Value FROM SystemParameter WHERE Name = @Name LIMIT 1";
                    using (var command = new SqliteCommand(selectQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Name", parameterName);
                        var result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            return result.ToString();
                        }
                    }

                    // Kayıt yok, default değer verilmişse otomatik oluştur
                    if (defaultValue != null)
                    {
                        string detailToUse = defaultDetail ?? $"Otomatik oluşturulan varsayılan değer ({parameterName})";

                        string insertQuery = @"
                            INSERT INTO SystemParameter (Name, Value, Detail, CreatedDate, UpdatedDate)
                            VALUES (@Name, @Value, @Detail, datetime('now', 'localtime'), datetime('now', 'localtime'))";

                        using (var insertCommand = new SqliteCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@Name", parameterName);
                            insertCommand.Parameters.AddWithValue("@Value", defaultValue);
                            insertCommand.Parameters.AddWithValue("@Detail", detailToUse);
                            insertCommand.ExecuteNonQuery();
                        }

                        return defaultValue;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Sistem parametresi okuma hatası ({parameterName}): {ex.Message}");
#endif
            }

            return defaultValue;
        }


        private void InsertSamplePlates()
        {
            string[] samplePlates = {
                "55ABC75",
                "34DEF123",
                "06GHI456",
                "35JKL789",
                "16MNO012"
            };

            foreach (string plate in samplePlates)
            {
                AddPlate(plate, "Örnek Araç Sahibi", "Binek Araç");
            }
        }

        public bool AddPlate(string plateNumber, string ownerName, string vehicleType)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO Plates (PlateNumber, OwnerName, VehicleType, CreatedDate, UpdatedDate) 
                        VALUES (@PlateNumber, @OwnerName, @VehicleType, datetime('now', 'localtime'), datetime('now', 'localtime'))";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PlateNumber", plateNumber);
                        command.Parameters.AddWithValue("@OwnerName", ownerName);
                        command.Parameters.AddWithValue("@VehicleType", vehicleType);
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Plaka ekleme hatası: {ex.Message}");
#endif
                return false;
            }
        }

        public bool UpdatePlate(int id, string plateNumber, string ownerName, string vehicleType)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                        UPDATE Plates 
                        SET PlateNumber = @PlateNumber, 
                            OwnerName = @OwnerName, 
                            VehicleType = @VehicleType,
                            UpdatedDate = datetime('now', 'localtime')
                        WHERE Id = @Id";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@PlateNumber", plateNumber);
                        command.Parameters.AddWithValue("@OwnerName", ownerName);
                        command.Parameters.AddWithValue("@VehicleType", vehicleType);
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Plaka güncelleme hatası: {ex.Message}");
#endif
                return false;
            }
        }

        public bool SoftDeletePlate(int id)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = "UPDATE Plates SET IsActive = 0, UpdatedDate = datetime('now', 'localtime') WHERE Id = @Id";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Plaka silme hatası: {ex.Message}");
#endif
                return false;
            }
        }

        public bool IsPlateAuthorized(string plateNumber)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Plates WHERE PlateNumber = @PlateNumber AND IsActive = 1";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PlateNumber", plateNumber);
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Plaka kontrol hatası: {ex.Message}");
#endif
                return false;
            }
        }

        public string GetPlateOwner(string plateNumber)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT OwnerName FROM Plates WHERE PlateNumber = @PlateNumber AND IsActive = 1";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PlateNumber", plateNumber);
                        var result = command.ExecuteScalar();
                        return result != null ? result.ToString() : "";
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Araç sahibi bulma hatası: {ex.Message}");
#endif
                return "";
            }
        }

        public void LogAccess(string plateNumber, string plateOwner, string accessType, bool isAuthorized, double confidence = 0, string notes = "")
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO AccessLog (PlateNumber, PlateOwner, AccessType, IsAuthorized, Confidence, AccessTime, Notes) 
                        VALUES (@PlateNumber, @PlateOwner, @AccessType, @IsAuthorized, @Confidence, datetime('now', 'localtime'), @Notes)";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PlateNumber", plateNumber);
                        command.Parameters.AddWithValue("@PlateOwner", plateOwner ?? "");
                        command.Parameters.AddWithValue("@AccessType", accessType);
                        command.Parameters.AddWithValue("@IsAuthorized", isAuthorized ? 1 : 0);
                        command.Parameters.AddWithValue("@Confidence", confidence);
                        command.Parameters.AddWithValue("@Notes", notes ?? "");
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Erişim log hatası: {ex.Message}");
#endif
            }
        }

        public void LogSystem(string logLevel, string message, string component = "", string details = "")
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                INSERT INTO SystemLog (LogLevel, Message, LogTime, Component, Details) 
                VALUES (@LogLevel, @Message, datetime('now', 'localtime'), @Component, @Details)";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@LogLevel", logLevel);
                        command.Parameters.AddWithValue("@Message", message);
                        command.Parameters.AddWithValue("@Component", component);
                        command.Parameters.AddWithValue("@Details", details);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Sistem log hatası: {ex.Message}");
#endif
            }
        }

        public DataTable GetPlates()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM Plates WHERE IsActive = 1 ORDER BY CreatedDate DESC";

                    using (var command = new SqliteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        var dataTable = new DataTable();
                        dataTable.Load(reader);
                        return dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Plaka listesi alma hatası: {ex.Message}");
#endif
                return new DataTable();
            }
        }

        public DataTable GetAccessLog(int limit = 1000)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = $"SELECT * FROM AccessLog ORDER BY AccessTime DESC LIMIT {limit}";

                    using (var command = new SqliteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        var dataTable = new DataTable();
                        dataTable.Load(reader);
                        return dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Erişim log listesi alma hatası: {ex.Message}");
#endif
                return new DataTable();
            }
        }

        public bool DeleteAccessLogs(List<int> ids)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var command = connection.CreateCommand();
                            command.Transaction = transaction;
                            
                            // Generate parameters dynamically
                            var parameters = new List<string>();
                            for (int i = 0; i < ids.Count; i++)
                            {
                                string paramName = $"@id{i}";
                                parameters.Add(paramName);
                                command.Parameters.AddWithValue(paramName, ids[i]);
                            }

                            command.CommandText = $"DELETE FROM AccessLog WHERE Id IN ({string.Join(",", parameters)})";
                            command.ExecuteNonQuery();

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Log silme hatası: {ex.Message}");
#endif
                LogSystem("ERROR", "Log silme başarısız", "DatabaseManager.DeleteAccessLogs", ex.Message);
                return false;
            }
        }

        public DataTable GetSystemLog(int limit = 1000)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = $"SELECT * FROM SystemLog ORDER BY LogTime DESC LIMIT {limit}";

                    using (var command = new SqliteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        var dataTable = new DataTable();
                        dataTable.Load(reader);
                        return dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Sistem log listesi alma hatası: {ex.Message}");
#endif
                return new DataTable();
            }
        }

        public bool DeleteSystemLogs(List<int> ids)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var command = connection.CreateCommand();
                            command.Transaction = transaction;
                            
                            // Generate parameters dynamically
                            var parameters = new List<string>();
                            for (int i = 0; i < ids.Count; i++)
                            {
                                string paramName = $"@id{i}";
                                parameters.Add(paramName);
                                command.Parameters.AddWithValue(paramName, ids[i]);
                            }

                            command.CommandText = $"DELETE FROM SystemLog WHERE Id IN ({string.Join(",", parameters)})";
                            command.ExecuteNonQuery();

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Sistem log silme hatası: {ex.Message}");
#endif
                return false;
            }
        }

        public bool DeleteOldAccessLogs(int days)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM AccessLog WHERE AccessTime < datetime('now', 'localtime', @Days)";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Days", $"-{days} days");
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Eski erişim kayıtları silme hatası: {ex.Message}");
#endif
                return false;
            }
        }

        public bool DeleteOldSystemLogs(int days)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM SystemLog WHERE LogTime < datetime('now', 'localtime', @Days)";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Days", $"-{days} days");
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] Eski sistem kayıtları silme hatası: {ex.Message}");
#endif
                return false;
            }
        }
    }
} 