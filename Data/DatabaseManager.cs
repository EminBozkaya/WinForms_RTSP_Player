using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace WinForms_RTSP_Player.Data
{
    public class DatabaseManager
    {
        private readonly string _connectionString;
        private readonly string _dbPath;

        public DatabaseManager()
        {
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlateDatabase.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(_dbPath))
            {
                CreateDatabase();
            }
        }

        private void CreateDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
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
                        CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                    )";

                // Giriş/çıkış log tablosu
                string createAccessLogTable = @"
                    CREATE TABLE AccessLog (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PlateNumber TEXT NOT NULL,
                        AccessType TEXT NOT NULL, -- 'IN' veya 'OUT'
                        AccessTime DATETIME DEFAULT CURRENT_TIMESTAMP,
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
                        LogTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                        Details TEXT
                    )";

                using (var command = new SQLiteCommand(createPlatesTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SQLiteCommand(createAccessLogTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SQLiteCommand(createSystemLogTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Örnek plaka verileri ekle
                InsertSamplePlates();
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
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO Plates (PlateNumber, OwnerName, VehicleType) 
                        VALUES (@PlateNumber, @OwnerName, @VehicleType)";

                    using (var command = new SQLiteCommand(query, connection))
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

        public bool IsPlateAuthorized(string plateNumber)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Plates WHERE PlateNumber = @PlateNumber AND IsActive = 1";

                    using (var command = new SQLiteCommand(query, connection))
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

        public void LogAccess(string plateNumber, string accessType, bool isAuthorized, double confidence = 0)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO AccessLog (PlateNumber, AccessType, IsAuthorized, Confidence) 
                        VALUES (@PlateNumber, @AccessType, @IsAuthorized, @Confidence)";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PlateNumber", plateNumber);
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

        public void LogSystem(string logLevel, string message, string details = "")
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO SystemLog (LogLevel, Message, Details) 
                        VALUES (@LogLevel, @Message, @Details)";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@LogLevel", logLevel);
                        command.Parameters.AddWithValue("@Message", message);
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
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM Plates WHERE IsActive = 1 ORDER BY CreatedDate DESC";

                    using (var adapter = new SQLiteDataAdapter(query, connection))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
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
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    string query = $"SELECT * FROM AccessLog ORDER BY AccessTime DESC LIMIT {limit}";

                    using (var adapter = new SQLiteDataAdapter(query, connection))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
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