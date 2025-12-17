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
                Console.WriteLine($"Şema güncelleme hatası: {ex.Message}");
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
                        LogTime DATETIME DEFAULT (datetime('now', 'localtime')),
                        Details TEXT
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

                // Örnek plaka verileri ekle
                //InsertSamplePlates();
            }
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
                Console.WriteLine($"Plaka ekleme hatası: {ex.Message}");
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
                Console.WriteLine($"Plaka güncelleme hatası: {ex.Message}");
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
                Console.WriteLine($"Plaka silme hatası: {ex.Message}");
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
                Console.WriteLine($"Plaka kontrol hatası: {ex.Message}");
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
                Console.WriteLine($"Araç sahibi bulma hatası: {ex.Message}");
                return "";
            }
        }

        public void LogAccess(string plateNumber, string plateOwner, string accessType, bool isAuthorized, double confidence = 0)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO AccessLog (PlateNumber, PlateOwner, AccessType, IsAuthorized, Confidence, AccessTime) 
                        VALUES (@PlateNumber, @PlateOwner, @AccessType, @IsAuthorized, @Confidence, datetime('now', 'localtime'))";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PlateNumber", plateNumber);
                        command.Parameters.AddWithValue("@PlateOwner", plateOwner ?? "");
                        command.Parameters.AddWithValue("@AccessType", accessType);
                        command.Parameters.AddWithValue("@IsAuthorized", isAuthorized ? 1 : 0);
                        command.Parameters.AddWithValue("@Confidence", confidence);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erişim log hatası: {ex.Message}");
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
                Console.WriteLine($"Sistem log hatası: {ex.Message}");
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
                Console.WriteLine($"Plaka listesi alma hatası: {ex.Message}");
                return new DataTable();
            }
        }

        public DataTable GetAccessLog(int limit = 100)
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
                Console.WriteLine($"Erişim log listesi alma hatası: {ex.Message}");
                return new DataTable();
            }
        }
    }
} 