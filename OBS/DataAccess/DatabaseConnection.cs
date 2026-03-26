using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace OBS.DataAccess
{
    /// <summary>
    /// SQLite veritabanı dosyasını oluşturma/açma ve gerekli PRAGMA ayarlarını sağlamaktan sorumlu.
    /// SQLCipher kullanarak tüm veritabanı içeriğini (Şifreli obs_secure.db) AES-256 ile korur.
    /// Şifre, DatabaseKeyManager aracılığıyla Windows oturumuna bağlı olarak DPAPI ile korunur.
    /// </summary>
    public static class DatabaseConnection
    {
        private static readonly string _appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "OBS_System"
        );
        
        // Eski güvensiz veritabanı
        private static readonly string _oldDbPath = Path.Combine(_appFolder, "obs.db");
        // Yeni şifrelenmiş veritabanı
        private static readonly string _secureDbPath = Path.Combine(_appFolder, "obs_secure.db");
        
        // SqliteConnection için bağlantı dizesi
        private static readonly string _connectionString = $"Data Source={_secureDbPath}";

        private const int LatestSchemaVersion = 3;

        /// <summary>
        /// Sadece şifrelenmiş yeni veritabanını oluşturur ve açar.
        /// Eğer eski şifresiz veritabanı varsa ve şifreli olan yoksa,
        /// tüm verileri şifreli OLARAK YENİ DOSYAYA migrate eder.
        /// </summary>
        public static void EnsureDatabase()
        {
            if (!Directory.Exists(_appFolder))
                Directory.CreateDirectory(_appFolder);

            // Klasörleri oluştur
            var photosFolder = GetPhotosFolder();
            var logsFolder = GetLogsFolder();
            var pdfFolder = GetPdfFolder();
            var studentsFolder = GetStudentsPdfFolder();
            var classFolder = GetClassPdfFolder();

            if (!Directory.Exists(photosFolder)) Directory.CreateDirectory(photosFolder);
            if (!Directory.Exists(logsFolder)) Directory.CreateDirectory(logsFolder);
            if (!Directory.Exists(pdfFolder)) Directory.CreateDirectory(pdfFolder);
            // SQLCipher'ı (kriptolama motorunu) başlat
            SQLitePCL.Batteries_V2.Init();
            
            bool oldDbExists = File.Exists(_oldDbPath);
            bool secureDbExists = File.Exists(_secureDbPath);

            // GÜVENLİK/SAĞLAMLIK KONTROLÜ: Eğer migration yarım kalmışsa ve dosya hatalıysa (0 byte) dosyayı sil
            // SQLite dosyası diske yazılamadıysa 0 byte olarak kalır. Normal bir veritabanı en azından header (genelde 4096 byte) içerir.
            if (secureDbExists && new FileInfo(_secureDbPath).Length == 0)
            {
                File.Delete(_secureDbPath);
                secureDbExists = false;
            }

            // ŞİFRESİZ -> ŞİFRELİ MİGRASYON SÜRECİ
            if (oldDbExists && !secureDbExists)
            {
                MigrateToEncryptedDatabase();
                return; // Migrasyon sonrası yeni açılış tamamlandı
            }

            // Normal Açılış
            var isNew = !secureDbExists;

            using var conn = GetConnection();
            conn.Open();

            ApplyPerformancePragmas(conn);

            if (isNew)
            {
                CreateTablesIfNotExist(conn);
                CreateIndexes(conn);
                SetSchemaVersion(conn, LatestSchemaVersion);
            }
            else
            {
                RunMigrations(conn);
            }
        }

        /// <summary>
        /// Eski 'obs.db' içindeki tüm tablo şemasını ve verileri yeni
        /// korumalı 'obs_secure.db' dosyasına SQLite sqlcipher_export komutu
        /// ile sorunsuz şekilde aktarır ve sonrasında eski dosyayı güvenli siler.
        /// </summary>
        private static void MigrateToEncryptedDatabase()
        {
            string dbKey = DatabaseKeyManager.GetOrCreateKey();

            // Sadece bir defaya mahsus eski şifresiz dosyaya bağlanıyoruz
            using (var unencryptedConn = new SqliteConnection($"Data Source={_oldDbPath}"))
            {
                unencryptedConn.Open();
                
                // Korumalı veritabanını eski dosyanın üzerinden ATTACH edip şifre anahtarını tanımlayarak yaratacağız
                using var cmd = unencryptedConn.CreateCommand();

                // 1. Şifreli dosyayı "encrypted" adıyla (ATTACH kullanarak) sisteme tanıtıyoruz
                cmd.CommandText = $"ATTACH DATABASE '{_secureDbPath}' AS encrypted KEY '{dbKey}';";
                cmd.ExecuteNonQuery();

                // 2. sqlcipher_export komutu eski veritabanının Tıpatıp kopyasını (şema+veri) yeni şifreli kısma aktarır
                cmd.CommandText = "SELECT sqlcipher_export('encrypted');";
                cmd.ExecuteNonQuery();

                // 3. Dosyayı de-attach et
                cmd.CommandText = "DETACH DATABASE encrypted;";
                cmd.ExecuteNonQuery();
            } // Bağlantı kapanır
            
            // sqlite-net / Microsoft.Data.Sqlite pool ve WAL dosyalarını serbest bırakması için biraz zaman tanı
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            System.Threading.Thread.Sleep(500);

            // Eğer aktarım başarılı olduysa ve şifreli dosya oluştuysa, eski güvensiz dosyayı (ve WAL dosyalarını) uçur
            if (File.Exists(_secureDbPath))
            {
                TryDeleteFile(_oldDbPath);
                TryDeleteFile(_oldDbPath + "-wal");
                TryDeleteFile(_oldDbPath + "-shm");
            }
        }

        private static void ApplyPerformancePragmas(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                PRAGMA foreign_keys = ON;
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;
                PRAGMA busy_timeout = 5000;
                PRAGMA cache_size = -20000;
                PRAGMA temp_store = MEMORY;
                PRAGMA mmap_size = 268435456;
            ";
            cmd.ExecuteNonQuery();
        }

        private static void CreateTablesIfNotExist(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Guardians (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    PhoneNumber TEXT,
                    StudentNumber TEXT NOT NULL,
                    FOREIGN KEY(StudentNumber) REFERENCES Students(StudentNumber) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Students (
                    StudentNumber TEXT PRIMARY KEY,
                    FirstName TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    Class TEXT,
                    ClassNo INTEGER,
                    TcNo TEXT,
                    BirthDate TEXT,
                    PhotoPath TEXT,
                    Gender TEXT,
                    GuardianId INTEGER,
                    KunyePdfPath TEXT,
                    SpecialNote TEXT,
                    FOREIGN KEY(GuardianId) REFERENCES Guardians(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS StudentNotes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentNumber TEXT NOT NULL,
                    NoteText TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY(StudentNumber) REFERENCES Students(StudentNumber) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Teams (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TeamName TEXT NOT NULL UNIQUE,
                    Category TEXT NOT NULL,
                    MatchDate TEXT,
                    Description TEXT
                );

                CREATE TABLE IF NOT EXISTS TeamMembers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TeamId INTEGER NOT NULL,
                    StudentNumber TEXT NOT NULL,
                    AddedDate TEXT NOT NULL,
                    UNIQUE(StudentNumber),
                    UNIQUE(TeamId, StudentNumber),
                    FOREIGN KEY(TeamId) REFERENCES Teams(Id) ON DELETE CASCADE,
                    FOREIGN KEY(StudentNumber) REFERENCES Students(StudentNumber) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Favorites (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentNumber TEXT NOT NULL UNIQUE,
                    AddedDate TEXT NOT NULL,
                    FOREIGN KEY(StudentNumber) REFERENCES Students(StudentNumber) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Settings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Key TEXT NOT NULL UNIQUE,
                    Value TEXT NOT NULL
                );
            ";

            cmd.ExecuteNonQuery();
        }

        private static void CreateIndexes(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE INDEX IF NOT EXISTS idx_students_class ON Students(Class);
                CREATE INDEX IF NOT EXISTS idx_teammembers_teamid ON TeamMembers(TeamId);
            ";

            cmd.ExecuteNonQuery();
        }

        private static int GetSchemaVersion(SqliteConnection conn)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Value FROM Settings WHERE Key = 'SchemaVersion';";
                var result = cmd.ExecuteScalar();
                return result is string s && int.TryParse(s, out var v) ? v : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static void SetSchemaVersion(SqliteConnection conn, int version)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Settings (Key, Value) VALUES ('SchemaVersion', @v)
                ON CONFLICT(Key) DO UPDATE SET Value = @v;";
            cmd.Parameters.AddWithValue("@v", version.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void RunMigrations(SqliteConnection conn)
        {
            var currentVersion = GetSchemaVersion(conn);
            if (currentVersion >= LatestSchemaVersion) return;

            while (currentVersion < LatestSchemaVersion)
            {
                currentVersion++;

                using var tx = conn.BeginTransaction();
                try
                {
                    switch (currentVersion)
                    {
                        case 1: Migration_1(conn); break;
                        case 2: Migration_2(conn); break;
                        case 3: Migration_3(conn); break;
                    }

                    SetSchemaVersion(conn, currentVersion);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        private static void Migration_1(SqliteConnection conn)
        {
            AddColumnIfNotExists(conn, "Students", "Gender", "TEXT");
            AddColumnIfNotExists(conn, "Teams", "Category", "TEXT NOT NULL DEFAULT 'Futbol'");
            AddColumnIfNotExists(conn, "Students", "KunyePdfPath", "TEXT");
            CreateIndexes(conn);
        }

        private static void Migration_2(SqliteConnection conn)
        {
            AddColumnIfNotExists(conn, "Students", "SpecialNote", "TEXT");
        }

        private static void Migration_3(SqliteConnection conn)
        {
            // Yeni tabloyu oluştur
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS StudentNotes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentNumber TEXT NOT NULL,
                    NoteText TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY(StudentNumber) REFERENCES Students(StudentNumber) ON DELETE CASCADE
                );
            ";
            cmd.ExecuteNonQuery();

            // Eski SpecialNote verilerini yeni tabloya taşı. Veri kaybını önlemek için güvenli migration.
            using var migrateCmd = conn.CreateCommand();
            migrateCmd.CommandText = @"
                INSERT INTO StudentNotes (StudentNumber, NoteText, CreatedAt)
                SELECT StudentNumber, SpecialNote, datetime('now', 'localtime')
                FROM Students
                WHERE SpecialNote IS NOT NULL AND SpecialNote != '';
            ";
            migrateCmd.ExecuteNonQuery();
        }

        private static void AddColumnIfNotExists(SqliteConnection conn, string table, string column, string definition)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({table});";
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                if (reader.GetString(1).Equals(column, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            reader.Close();

            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
            alterCmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Repository'ler tarafından kullanılacak yeni bir SqliteConnection döndürür.
        /// SQLCipher kullanıldığı için bağlantı sırasında şifre de iletilir.
        /// </summary>
        public static SqliteConnection GetConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            
            // Güvenlik Anahtarını al ve SQLite bağlantısına (SQLCipher) ata
            string dbKey = DatabaseKeyManager.GetOrCreateKey();
            
            // Eğer Connection henüz Open() edilmediyse SQLiteConnection komut yürütmeye izin vermez
            // Ancak Microsoft.Data.Sqlite, Parolayı SQLite'a doğrudan geçirmek için Password property'sini desteklemediğinde, 
            // SqliteConnection içerisinde bir callback event tanımlayabiliriz ki Open() dediğimiz anda PRAGMA key otomatik çalışsın.
            
            // Yöntem (ADO.NET): SqliteConnection objesi oluşturulur, ama Password parametresi verilemezse doğrudan Exec edilemez
            // En güvenilir yöntem connectionString'e Password eklemektir. Sqlcipher modifiye edilmiş eklentisinde Password= parametresi PRAGMA key yerine geçer:
            
            return new SqliteConnection(_connectionString + $";Password={dbKey}");
        }

        public static string GetDatabasePath() => _secureDbPath;

        public static string GetPhotosFolder() => Path.Combine(_appFolder, "Photos");
        public static string GetLogsFolder() => Path.Combine(_appFolder, "Logs");
        public static string GetPdfFolder() => Path.Combine(_appFolder, "PDFs");
        public static string GetStudentsPdfFolder() => Path.Combine(_appFolder, "Students");
        public static string GetClassPdfFolder() => Path.Combine(_appFolder, "Class");

        public static void ResetDatabase()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                using (var conn = GetConnection())
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();

                    cmd.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"
                        DELETE FROM TeamMembers;
                        DELETE FROM Teams;
                        DELETE FROM Favorites;
                        DELETE FROM Guardians;
                        DELETE FROM Students;
                        DELETE FROM Settings WHERE Key NOT IN ('PIN', 'SchemaVersion');
                    ";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                    cmd.ExecuteNonQuery();
                }

                using (var vacuumConn = GetConnection())
                {
                    vacuumConn.Open();
                    using var vacuumCmd = vacuumConn.CreateCommand();
                    vacuumCmd.CommandText = "VACUUM;";
                    vacuumCmd.ExecuteNonQuery();
                }

                System.Threading.Thread.Sleep(100);

                var photosFolder = GetPhotosFolder();
                if (Directory.Exists(photosFolder))
                {
                    foreach (var file in Directory.GetFiles(photosFolder)) TryDeleteFile(file);
                }

                var pdfFolder = GetPdfFolder();
                if (Directory.Exists(pdfFolder))
                {
                    foreach (var file in Directory.GetFiles(pdfFolder)) TryDeleteFile(file);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Veritabanı sıfırlama hatası: {ex.Message}", ex);
            }
        }

        private static void TryDeleteFile(string filePath)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    File.Delete(filePath);
                    break;
                }
                catch (IOException) when (i < 2)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Threading.Thread.Sleep(100);
                }
                catch
                {
                    if (i == 2) System.Diagnostics.Debug.WriteLine($"Dosya silinemedi: {filePath}");
                }
            }
        }
    }
}
