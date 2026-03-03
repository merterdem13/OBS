using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace OBS.DataAccess
{
    /// <summary>
    /// SQLite veritabanı dosyasını oluşturma/açma ve gerekli PRAGMA ayarlarını sağlamaktan sorumlu.
    /// Veritabanı dosyası admin izin sorunlarını önlemek için AppData\Local\OBS_System altına yerleştirilir.
    /// .NET 10 ile uyumlu Microsoft.Data.Sqlite kullanır.
    /// </summary>
    public static class DatabaseConnection
    {
        private static readonly string _appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "OBS_System"
        );
        private static readonly string _dbPath = Path.Combine(_appFolder, "obs.db");
        private static readonly string _connectionString = $"Data Source={_dbPath}";

        // ── Migrasyon Sistemi ───────────────────────────────────────────────
        // Her yeni DB değişikliğinde:
        // 1. LatestSchemaVersion'ı artır
        // 2. RunMigrations içine yeni case ekle
        private const int LatestSchemaVersion = 1;

        /// <summary>
        /// Veritabanı dosyasının ve klasörlerin var olduğundan emin olur.
        /// Bağlantı açıldığında dosya yoksa otomatik oluşturulur.
        /// </summary>
        public static void EnsureDatabase()
        {
            if (!Directory.Exists(_appFolder))
                Directory.CreateDirectory(_appFolder);

            // Destekleyici klasörleri oluştur
            var photosFolder = GetPhotosFolder();
            var logsFolder = GetLogsFolder();
            var pdfFolder = GetPdfFolder();
            var studentsPdfFolder = GetStudentsPdfFolder();
            var classPdfFolder = GetClassPdfFolder();

            if (!Directory.Exists(photosFolder)) Directory.CreateDirectory(photosFolder);
            if (!Directory.Exists(logsFolder)) Directory.CreateDirectory(logsFolder);
            if (!Directory.Exists(pdfFolder)) Directory.CreateDirectory(pdfFolder);
            if (!Directory.Exists(studentsPdfFolder)) Directory.CreateDirectory(studentsPdfFolder);
            if (!Directory.Exists(classPdfFolder)) Directory.CreateDirectory(classPdfFolder);

            var isNew = !File.Exists(_dbPath);

            using var conn = GetConnection();
            conn.Open();

            // Performans PRAGMA'ları — batch import (500-600 PDF) için kritik
            ApplyPerformancePragmas(conn);

            if (isNew)
            {
                // İlk çalıştırmada gerekli tabloları oluştur
                CreateTablesIfNotExist(conn);
                CreateIndexes(conn);
                SetSchemaVersion(conn, LatestSchemaVersion);
            }
            else
            {
                // Mevcut veritabanları için migrasyonları çalıştır
                RunMigrations(conn);
            }
        }

        /// <summary>
        /// Performans ve bütünlük PRAGMA'larını uygular.
        /// WAL modu: eş zamanlı okuma/yazma desteği, batch import için ~3-5x hız artışı.
        /// </summary>
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

        /// <summary>
        /// Tüm tabloları oluşturur (ilk kurulumda).
        /// İSTEM METNİNE GÖRE: Students, Guardians, Teams, TeamMembers, Favorites, Settings tabloları.
        /// </summary>
        private static void CreateTablesIfNotExist(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                -- Veli bilgileri tablosu
                CREATE TABLE IF NOT EXISTS Guardians (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    PhoneNumber TEXT,
                    StudentNumber TEXT NOT NULL,
                    FOREIGN KEY(StudentNumber) REFERENCES Students(StudentNumber) ON DELETE CASCADE
                );

                -- Öğrenci bilgileri tablosu
                -- StudentNumber UNIQUE index (merge ve arama için zorunlu)
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
                    FOREIGN KEY(GuardianId) REFERENCES Guardians(Id) ON DELETE CASCADE
                );

                -- Takım bilgileri tablosu (Category alanı eklendi)
                CREATE TABLE IF NOT EXISTS Teams (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TeamName TEXT NOT NULL UNIQUE,
                    Category TEXT NOT NULL,
                    MatchDate TEXT,
                    Description TEXT
                );

                -- Takım-Öğrenci ilişki tablosu
                -- StudentNumber ile ilişki kurulur (StudentId değil)
                -- Bir öğrenci birden fazla takımda olamaz (UNIQUE StudentNumber constraint)
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

                -- Favori öğrenciler tablosu
                CREATE TABLE IF NOT EXISTS Favorites (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentNumber TEXT NOT NULL UNIQUE,
                    AddedDate TEXT NOT NULL,
                    FOREIGN KEY(StudentNumber) REFERENCES Students(StudentNumber) ON DELETE CASCADE
                );

                -- Ayarlar tablosu (PIN vb.)
                CREATE TABLE IF NOT EXISTS Settings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Key TEXT NOT NULL UNIQUE,
                    Value TEXT NOT NULL
                );
            ";

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Performans için index'leri oluşturur.
        /// İSTEM METNİ GEREĞİ ZORUNLU INDEX'LER.
        /// </summary>
        private static void CreateIndexes(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                -- Students.StudentNumber → UNIQUE index (zaten PRIMARY KEY olduğu için otomatik)
                
                -- Students.Class → Non-unique index (sınıf filtreleme için)
                CREATE INDEX IF NOT EXISTS idx_students_class ON Students(Class);

                -- TeamMembers.TeamId → Index (takım listeleme için)
                CREATE INDEX IF NOT EXISTS idx_teammembers_teamid ON TeamMembers(TeamId);

                -- TeamMembers (TeamId + StudentNumber) → UNIQUE composite index (zaten UNIQUE constraint'te var)
            ";

            cmd.ExecuteNonQuery();
        }

        // ── Migrasyon Altyapısı ─────────────────────────────────────────────

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
                // Settings tablosu yoksa veya SchemaVersion kaydı yoksa
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

        /// <summary>
        /// Bekleyen migrasyonları sırasıyla çalıştırır.
        /// v0 → v1 → v2 → ... → LatestSchemaVersion
        /// </summary>
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
                        // ── Gelecek migrasyonlar buraya eklenir ──
                        // case 2: Migration_2(conn); break;
                        // case 3: Migration_3(conn); break;
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

        // ── Migrasyonlar ────────────────────────────────────────────────────

        /// <summary>
        /// Migration 1: v1.0.0 öncesi kullanıcılar için mevcut şema güncellemelerini konsolide eder.
        /// Gender, Category, KunyePdfPath sütunlarını ekler (yoksa).
        /// </summary>
        private static void Migration_1(SqliteConnection conn)
        {
            AddColumnIfNotExists(conn, "Students", "Gender", "TEXT");
            AddColumnIfNotExists(conn, "Teams", "Category", "TEXT NOT NULL DEFAULT 'Futbol'");
            AddColumnIfNotExists(conn, "Students", "KunyePdfPath", "TEXT");
            CreateIndexes(conn);
        }

        // ── ÖRNEK: Gelecekte yeni tablo/sütun eklemek istediğinizde ─────────
        //
        // private static void Migration_2(SqliteConnection conn)
        // {
        //     using var cmd = conn.CreateCommand();
        //     cmd.CommandText = @"
        //         CREATE TABLE IF NOT EXISTS Notes (
        //             Id INTEGER PRIMARY KEY AUTOINCREMENT,
        //             StudentNumber TEXT NOT NULL,
        //             Content TEXT NOT NULL,
        //             CreatedAt TEXT NOT NULL,
        //             FOREIGN KEY(StudentNumber) REFERENCES Students(StudentNumber) ON DELETE CASCADE
        //         );
        //         CREATE INDEX IF NOT EXISTS idx_notes_student ON Notes(StudentNumber);";
        //     cmd.ExecuteNonQuery();
        // }

        /// <summary>
        /// Tabloya sütun ekler (yoksa). Migrasyon metotlarından çağrılır.
        /// </summary>
        private static void AddColumnIfNotExists(
            SqliteConnection conn, string table, string column, string definition)
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
        /// Çağıran taraf bağlantıyı Open() etmelidir.
        /// </summary>
        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        /// <summary>
        /// DB dosyasının yolunu döndürür (tanılama/loglama için kullanışlı).
        /// </summary>
        public static string GetDatabasePath() => _dbPath;

        /// <summary>
        /// Fotoğraf klasörünün yolunu döndürür.
        /// </summary>
        public static string GetPhotosFolder() => Path.Combine(_appFolder, "Photos");

        /// <summary>
        /// Log klasörünün yolunu döndürür.
        /// </summary>
        public static string GetLogsFolder() => Path.Combine(_appFolder, "Logs");

        /// <summary>
        /// PDF klasörünün yolunu döndürür (geçici PDF dosyaları için).
        /// </summary>
        public static string GetPdfFolder() => Path.Combine(_appFolder, "PDFs");

        /// <summary>
        /// Öğrenci künye PDF'lerinin ve resimlerinin saklandığı klasörü döndürür.
        /// </summary>
        public static string GetStudentsPdfFolder() => Path.Combine(_appFolder, "Students");

        /// <summary>
        /// Sınıf listesi ve resim listesi PDF'lerinin saklandığı klasörü döndürür.
        /// </summary>
        public static string GetClassPdfFolder() => Path.Combine(_appFolder, "Class");

        /// <summary>
        /// Tüm veritabanını ve fotoğraf klasörünü siler (SİSTEMİ SIFIRLA fonksiyonu için).
        /// PIN verisi korunur (Settings tablosundan sadece PIN silinmez).
        /// </summary>
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

                    // PIN ve SchemaVersion hariç tüm verileri sil
                    cmd.CommandText = @"
                        DELETE FROM TeamMembers;
                        DELETE FROM Teams;
                        DELETE FROM Favorites;
                        DELETE FROM Guardians;
                        DELETE FROM Students;
                        DELETE FROM Settings WHERE Key NOT IN ('PIN', 'SchemaVersion');
                    ";
                    cmd.ExecuteNonQuery();

                    // WAL checkpoint — WAL dosyasını ana DB'ye yaz ve truncate et
                    cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                    cmd.ExecuteNonQuery();
                }

                // VACUUM ayrı bağlantıda — dosya boyutunu küçült
                using (var vacuumConn = GetConnection())
                {
                    vacuumConn.Open();
                    using var vacuumCmd = vacuumConn.CreateCommand();
                    vacuumCmd.CommandText = "VACUUM;";
                    vacuumCmd.ExecuteNonQuery();
                }

                System.Threading.Thread.Sleep(100);

                // Fotoğraf klasörünü temizle — tüm formatlar
                var photosFolder = GetPhotosFolder();
                if (Directory.Exists(photosFolder))
                {
                    foreach (var file in Directory.GetFiles(photosFolder))
                    {
                        TryDeleteFile(file);
                    }
                }

                // PDF klasörünü temizle
                var pdfFolder = GetPdfFolder();
                if (Directory.Exists(pdfFolder))
                {
                    foreach (var file in Directory.GetFiles(pdfFolder))
                    {
                        TryDeleteFile(file);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Veritabanı sıfırlama hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Dosyayı silmeyi dener, kilitli ise birkaç deneme yapar.
        /// </summary>
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
                    // Kısa bir bekleme sonrası tekrar dene
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Threading.Thread.Sleep(100);
                }
                catch
                {
                    // Son denemede de başarısız olursa sessizce devam et
                    if (i == 2)
                    {
                        System.Diagnostics.Debug.WriteLine($"Dosya silinemedi: {filePath}");
                    }
                }
            }
        }
    }
}
