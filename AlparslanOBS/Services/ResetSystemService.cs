using System;
using System.IO;
using Microsoft.Data.Sqlite;
using AlparslanOBS.DataAccess;

namespace AlparslanOBS.Services
{
    /// <summary>
    /// Service responsible for resetting the entire system while preserving PIN data.
    /// This includes clearing all database tables except Settings, deleting photos, and cleaning temporary files.
    /// </summary>
    public class ResetSystemService
    {
        /// <summary>
        /// Resets the entire system: clears Students, Classes, Teams, TeamMembers, Favorites, Guardians
        /// while preserving PIN data in Settings table. Also deletes all photo files.
        /// </summary>
        public void ResetSystem()
        {
            try
            {
                // Step 1: Delete all photo files
                DeleteAllPhotos();

                // Step 2: Clear database tables (preserve Settings)
                ClearDatabaseTables();

                // Step 3: VACUUM + WAL checkpoint — dosya boyutunu minimize et
                CompactDatabase();

                // Step 4: Clean up temporary PDF files
                CleanupPdfFolder();

                // Step 5: Clean up Students PDF folder
                CleanupFolder(DatabaseConnection.GetStudentsPdfFolder(), "*.*", "Student files");

                // Step 6: Clean up Class PDF folder
                CleanupFolder(DatabaseConnection.GetClassPdfFolder(), "*.*", "Class files");

                // Step 7: Clean up log files
                CleanupFolder(DatabaseConnection.GetLogsFolder(), "*.txt", "Log files");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Sistem sıfırlama işlemi başarısız oldu: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes all photo files from the Photos folder.
        /// GC ile WPF Image kontrollerinin dosya handle'ları serbest bırakılır.
        /// </summary>
        private void DeleteAllPhotos()
        {
            var photosFolder = DatabaseConnection.GetPhotosFolder();
            if (!Directory.Exists(photosFolder))
                return;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            try
            {
                // Tüm dosyaları sil (jpg, png, jpeg, bmp vb.)
                foreach (var file in Directory.GetFiles(photosFolder))
                {
                    TryDeleteFile(file);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Fotoğraflar silinirken hata oluştu: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Clears all student-related data from database while preserving PIN in Settings table.
        /// Uses transactional approach to ensure data integrity.
        /// </summary>
        private void ClearDatabaseTables()
        {
            using var conn = DatabaseConnection.GetConnection();
            conn.Open();

            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var transaction = conn.BeginTransaction();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = transaction;

                // FK bağımlılık sırasına göre sil
                cmd.CommandText = "DELETE FROM TeamMembers;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "DELETE FROM Teams;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "DELETE FROM Favorites;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "DELETE FROM Guardians;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "DELETE FROM Students;";
                cmd.ExecuteNonQuery();

                // Settings table is NOT cleared to preserve PIN data

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new InvalidOperationException($"Veritabanı temizlenirken hata oluştu: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// DELETE sonrası boş kalan SQLite sayfalarını geri alır ve dosya boyutunu küçültür.
        /// WAL dosyasını checkpoint edip truncate eder.
        ///
        /// DELETE FROM sadece sayfaları "boş" olarak işaretler — dosya boyutu aynı kalır.
        /// VACUUM veritabanını sıfırdan yeniden yazar → minimum dosya boyutu.
        /// WAL checkpoint → obs.db-wal dosyasını temizler.
        /// </summary>
        private void CompactDatabase()
        {
            using var conn = DatabaseConnection.GetConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();

            // WAL checkpoint: tüm değişiklikleri ana DB'ye yaz ve WAL dosyasını truncate et
            cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            cmd.ExecuteNonQuery();

            // VACUUM: veritabanını sıfırdan yeniden yaz → boş sayfalar kaldırılır
            cmd.CommandText = "VACUUM;";
            cmd.ExecuteNonQuery();
        }

        private void CleanupPdfFolder()
        {
            CleanupFolder(DatabaseConnection.GetPdfFolder(), "*.*", "PDF files");
        }

        private void CleanupFolder(string folderPath, string searchPattern, string description)
        {
            if (!Directory.Exists(folderPath))
                return;

            try
            {
                foreach (var file in Directory.GetFiles(folderPath, searchPattern))
                {
                    TryDeleteFile(file);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"{description} silinirken hata oluştu: {ex.Message}", ex);
            }
        }

        private static void TryDeleteFile(string filePath)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    File.Delete(filePath);
                    return;
                }
                catch (IOException) when (i < 2)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }
}
