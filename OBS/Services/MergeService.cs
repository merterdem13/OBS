using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using OBS.Models;
using OBS.DataAccess;

namespace OBS.Services
{
    /// <summary>
    /// Servis katmanı soyutlaması. İleride farklı kaynaklardan (Excel, API)
    /// aktarım yapıldığında bu Interface üzerinden DI (Dependency Injection) ile ilerlenecek.
    /// </summary>
    public interface IMergeService
    {
        void MergeStudents(IEnumerable<Student> importedStudents, IDictionary<string, string>? kunyePdfMap = null);
        void MergeClassList(IEnumerable<(string StudentNumber, string Class, string Gender)> classListData);
        void MergeStudentsWithClassList(IEnumerable<Student> kunyeStudents, IEnumerable<(string StudentNumber, string Class, string Gender)> classList);
    }

    /// <summary>
    /// Öğrenci verilerinin sisteme aktarılmasından, normalizasyonundan,
    /// disk temizliğinden ve Transaction (Rollback) bütünlüğünden sorumlu ana iş katmanı.
    /// İş kuralları bu sınıfta işletilir, veri yazma işlemi Repository'e devredilir.
    /// </summary>
    public class MergeService : IMergeService
    {
        private readonly StudentRepository _repo;
        private readonly SyncService _syncService;
        private static readonly object _logLock = new object(); // Çoklu thread (loglama) çakışmasını önlemek için

        public MergeService()
        {
            _repo = new StudentRepository();
            _syncService = new SyncService();
        }

        /// <summary>
        /// Künye (Kişisel Bilgi ve Fotoğraf) verilerini sisteme dahil eder (UPSERT).
        /// Hata durumunda hem veritabanını hem de fiziksel kopyalanan dosyaları geri alır (Rollback).
        /// </summary>
        public void MergeStudents(IEnumerable<Student> importedStudents, IDictionary<string, string>? kunyePdfMap = null)
        {
            if (importedStudents == null || !importedStudents.Any()) return;

            var photosFolder = DatabaseConnection.GetPhotosFolder();
            var logsFolder = DatabaseConnection.GetLogsFolder();
            var logFile = Path.Combine(logsFolder, "app_log.txt");

            // Fiziksel dosya rollback yönetimi için liste
            var copiedFilesDuringTransaction = new List<string>();

            // SyncService - 5 Ortak Öğrenci Kuralı ve Nakillerin Silinmesi (Künye PDF'leri için)
            var studentNumbersList = importedStudents.Select(s => s.StudentNumber).ToList();
            var syncResult = _syncService.AnalyzeClassSync(studentNumbersList, string.Empty);

            if (syncResult.IsUpdate)
            {
                if (syncResult.StudentsToDelete.Any())
                {
                    _repo.DeleteMultiple(syncResult.StudentsToDelete);
                    LogToFile(logFile, $"SYNC (Künye): {syncResult.OldClassName} sınıfı toplu Künye PDF'i ile güncellendi. {syncResult.StudentsToDelete.Count} adet öğrenci nakil/ayrılmış sayılarak silindi.");
                }

                // Ekranda okunup belleğe alınan geçici öğrencilerin (importedStudents) hepsine 
                // bu tespit edilen eski sınıfın adını ata. Böylece veritabanına bu isimle kaydedilirler.
                foreach (var s in importedStudents)
                {
                    s.Class = syncResult.NewClassName;
                }
            }

            using var conn = DatabaseConnection.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                foreach (var imported in importedStudents)
                {
                    if (string.IsNullOrWhiteSpace(imported.StudentNumber))
                    {
                        LogToFile(logFile, "UYARI: Öğrenci numarası boş olan bir kayıt atlandı.");
                        continue;
                    }

                    // İş kuralı 1: Öğrenci numarasını normalize et (virgül vb. ayırıcıları temizle)
                    imported.StudentNumber = FileNameHelper.NormalizeStudentNumber(imported.StudentNumber);

                    // İş kuralı 2: Fotoğraf Yönetimi ve Fallback (Güvenlik) Senaryosu
                    if (!string.IsNullOrWhiteSpace(imported.PhotoPath) && File.Exists(imported.PhotoPath))
                    {
                        try
                        {
                            var ext = Path.GetExtension(imported.PhotoPath);
                            if (string.IsNullOrEmpty(ext)) ext = ".jpg";

                            // {NO}-{AD-SOYAD} formatında isimlendirme
                            var baseName = FileNameHelper.BuildStudentFileName(imported.StudentNumber, imported.FirstName, imported.LastName);
                            if (string.IsNullOrWhiteSpace(baseName)) baseName = Guid.NewGuid().ToString("N");

                            var fileName = $"{baseName}{ext}";
                            var dest = Path.Combine(photosFolder, fileName);

                            File.Copy(imported.PhotoPath, dest, overwrite: true);
                            imported.PhotoPath = dest;

                            // Başarılı kopyalanan dosyayı fiziksel rollback listesine ekle
                            copiedFilesDuringTransaction.Add(dest);
                        }
                        catch (Exception ex)
                        {
                            LogToFile(logFile, $"HATA: {imported.StudentNumber} için fotoğraf kopyalanamadı: {ex.Message}");
                            ApplyFallbackAvatar(imported, photosFolder);
                        }
                    }
                    else
                    {
                        // Fotoğraf yoksa varsayılan avatarı ata (Fallback)
                        ApplyFallbackAvatar(imported, photosFolder);
                    }

                    // İş kuralı 4: Künye PDF yolu ataması ve fiziksel klasör taşıma
                    if (kunyePdfMap != null && kunyePdfMap.TryGetValue(imported.StudentNumber, out var initialPdfPath))
                    {
                        var studentsPdfFolder = DatabaseConnection.GetStudentsPdfFolder();
                        var tempFolder = Path.Combine(studentsPdfFolder, ".temp");
                        
                        // Öğrencinin sınıf bilgisi varsa (SyncService'den veya eski DB kaydından)
                        var dbClassFormat = imported.Class?.Replace("/", "-");
                        if (!string.IsNullOrWhiteSpace(dbClassFormat))
                        {
                            var classFolder = Path.Combine(studentsPdfFolder, dbClassFormat);
                            if (!Directory.Exists(classFolder)) Directory.CreateDirectory(classFolder);

                            var fileName = Path.GetFileName(initialPdfPath);
                            var newFilePath = Path.Combine(classFolder, fileName);

                            // Dosya halen ana Students klasöründeyse veya başka bir yerde ise class klasörüne taşı
                            if (!string.Equals(Path.GetDirectoryName(initialPdfPath), classFolder, StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);
                                    var tempFilePath = Path.Combine(tempFolder, fileName);
                                    
                                    // Temp üzerine güvenli kopyalama
                                    File.Copy(initialPdfPath, tempFilePath, overwrite: true);
                                    
                                    // Sınıf klasörüne taşıma (ezerek)
                                    File.Copy(tempFilePath, newFilePath, overwrite: true);
                                    
                                    // Orijinal yeri (genelde doğrudan Students klasörü) sil
                                    if (File.Exists(initialPdfPath)) File.Delete(initialPdfPath);

                                    // Yolu yeni yer olarak güncelle
                                    imported.KunyePdfPath = newFilePath;
                                    copiedFilesDuringTransaction.Add(newFilePath);
                                    
                                    if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
                                }
                                catch (Exception ex)
                                {
                                    LogToFile(logFile, $"HATA: Künye PDF dosyası sınıf klasörüne taşınamadı ({fileName}): {ex.Message}");
                                    // Hata olsa dahi eski dosya yolunu veritabanına yazalım ki kaybolmasın
                                    imported.KunyePdfPath = initialPdfPath;
                                }
                            }
                            else
                            {
                                imported.KunyePdfPath = initialPdfPath;
                            }
                        }
                        else
                        {
                            // Sınıfı henüz belli değilse, olduğu yerde (genelde Students kök dizini) bırak
                            imported.KunyePdfPath = initialPdfPath;
                        }
                    }

                    // İş kuralı 3: Repository üzerinden UPSERT işlemini Transaction'a dahil et
                    _repo.Upsert(imported, conn, tx);
                }

                tx.Commit(); // Her şey başarılıysa DB onaylanır
            }
            catch (Exception ex)
            {
                // DB Hata verirse Rollback yap
                try { tx.Rollback(); } catch { }

                // MİMARİ DOKUNUŞ: Veritabanı geri alındıysa, bu süreçte diske kopyalanan fotoğrafları da sil! (Orphan File önlemi)
                foreach (var physicalFile in copiedFilesDuringTransaction)
                {
                    if (File.Exists(physicalFile))
                    {
                        try { File.Delete(physicalFile); } catch { }
                    }
                }

                LogToFile(logFile, $"KRİTİK HATA: Merge işlemi başarısız oldu, Transaction geri alındı. Hata: {ex.Message}");
                throw; // UI katmanına hatayı fırlat
            }
        }

        /// <summary>
        /// Sınıf listesi PDF'lerinden gelen (Öğrenci No -> Sınıf, Cinsiyet) eşleşmesini veritabanına aktarır.
        /// </summary>
        public void MergeClassList(IEnumerable<(string StudentNumber, string Class, string Gender)> classListData)
        {
            if (classListData == null || !classListData.Any()) return;

            var logFile = Path.Combine(DatabaseConnection.GetLogsFolder(), "app_log.txt");
            var classGroups = classListData.Where(c => !string.IsNullOrWhiteSpace(c.Class)).GroupBy(c => c.Class).ToList();

            using var conn = DatabaseConnection.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // SyncService - 5 Ortak Öğrenci Kuralı ve Nakillerin Silinmesi
                foreach (var group in classGroups)
                {
                    var studentNumbersInClass = group.Select(g => g.StudentNumber).ToList();
                    var syncResult = _syncService.AnalyzeClassSync(studentNumbersInClass, group.Key);

                    if (syncResult.IsUpdate && syncResult.StudentsToDelete.Any())
                    {
                        var dbClassFormatSync = group.Key.Replace("/", "-");
                        _repo.DeleteMultiple(syncResult.StudentsToDelete);
                        LogToFile(logFile, $"SYNC: {syncResult.OldClassName} sınıfı {dbClassFormatSync} olarak güncellendi. {syncResult.StudentsToDelete.Count} adet öğrenci nakil/ayrılmış sayılarak silindi.");
                    }
                }

                // Sınıf bilgilerinin ve fiziksel dosyaların güncellenmesi
                var studentsPdfFolder = DatabaseConnection.GetStudentsPdfFolder();
                var tempFolder = Path.Combine(studentsPdfFolder, ".temp");
                if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

                foreach (var (studentNumber, classInfo, gender) in classListData)
                {
                    if (string.IsNullOrWhiteSpace(studentNumber)) continue;

                    var normalizedSn = FileNameHelper.NormalizeStudentNumber(studentNumber);

                    // İş Kuralı: "7/F" formatını "7-F" formatına çevir
                    var dbClassFormat = classInfo?.Replace("/", "-") ?? string.Empty;

                    var studentInDb = _repo.GetByStudentNumber(normalizedSn);

                    var student = new Student
                    {
                        StudentNumber = normalizedSn,
                        Class = dbClassFormat,
                        Gender = gender ?? string.Empty
                    };

                    // UpdateClass işlemi (Kayıt yoksa Repository içerisinden DB yerine Log dosyasına yazılacak)
                    _repo.UpsertClass(student, conn, tx);

                    // Fiziksel Dosya Taşıma
                    if (studentInDb != null && !string.IsNullOrWhiteSpace(studentInDb.KunyePdfPath) && File.Exists(studentInDb.KunyePdfPath))
                    {
                        var classFolder = Path.Combine(studentsPdfFolder, dbClassFormat);
                        if (!Directory.Exists(classFolder)) Directory.CreateDirectory(classFolder);

                        var currentPdfDir = Path.GetDirectoryName(studentInDb.KunyePdfPath);

                        // Eğer dosya henüz bu sınıfın klasöründe değilse
                        if (!string.Equals(currentPdfDir, classFolder, StringComparison.OrdinalIgnoreCase))
                        {
                            var fileName = Path.GetFileName(studentInDb.KunyePdfPath);
                            var tempFilePath = Path.Combine(tempFolder, fileName);
                            var newFilePath = Path.Combine(classFolder, fileName);

                            try
                            {
                                // Önce temp klasörüne kopyala
                                File.Copy(studentInDb.KunyePdfPath, tempFilePath, overwrite: true);

                                // Sonra hedefe taşı (ezerek)
                                File.Copy(tempFilePath, newFilePath, overwrite: true);

                                // Eski dosyayı sil
                                File.Delete(studentInDb.KunyePdfPath);

                                // Veritabanındaki KunyePdfPath bilgisini güncelle
                                using var updateCmd = conn.CreateCommand();
                                updateCmd.Transaction = tx;
                                updateCmd.CommandText = "UPDATE Students SET KunyePdfPath = @kpp WHERE StudentNumber = @sn;";
                                updateCmd.Parameters.AddWithValue("@kpp", newFilePath);
                                updateCmd.Parameters.AddWithValue("@sn", normalizedSn);
                                updateCmd.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                LogToFile(logFile, $"HATA: {studentNumber} numaralı öğrencinin PDF dosyası taşınamadı. ({ex.Message})");
                            }
                            finally
                            {
                                if (File.Exists(tempFilePath))
                                {
                                    try { File.Delete(tempFilePath); } catch { }
                                }
                            }
                        }
                    }
                }

                tx.Commit();
                
                // Temp klasörünü temizle
                if (Directory.Exists(tempFolder) && !Directory.EnumerateFileSystemEntries(tempFolder).Any())
                {
                    try { Directory.Delete(tempFolder); } catch { }
                }
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                LogToFile(logFile, $"HATA: Sınıf listesi aktarımı başarısız oldu: {ex.Message}");
                throw;
            }
        }

        public void MergeStudentsWithClassList(IEnumerable<Student> kunyeStudents, IEnumerable<(string StudentNumber, string Class, string Gender)> classList)
        {
            var logFile = Path.Combine(DatabaseConnection.GetLogsFolder(), "app_log.txt");
            try
            {
                if (kunyeStudents != null && kunyeStudents.Any()) MergeStudents(kunyeStudents);
                if (classList != null && classList.Any()) MergeClassList(classList);
            }
            catch (Exception ex)
            {
                LogToFile(logFile, $"KOORDİNELİ HATA: Birleştirme sürecinde hata: {ex.Message}");
                throw;
            }
        }

        #region Helper Methods (İş Mantığı Yardımcıları)

        private void ApplyFallbackAvatar(Student student, string photosFolder)
        {
            // İleride UI tarafında bu yola boş bir silüet koyacağız
            string fallbackPath = Path.Combine(photosFolder, "default_avatar.jpg");
            student.PhotoPath = fallbackPath;
        }

        private void LogToFile(string logPath, string message)
        {
            // Lock mekanizması sayesinde aynı anda sınıf listesi ve künye servisi asenkron çalışırsa dosya çakışmaz
            lock (_logLock)
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
        }

        #endregion
    }
}
