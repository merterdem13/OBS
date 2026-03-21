using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OBS.DataAccess;
using OBS.Models;

namespace OBS.Services
{
    public interface IGarbageCollectorService
    {
        Task RunAsync();
    }

    public class GarbageCollectorService : IGarbageCollectorService
    {
        private readonly StudentRepository _studentRepository;
        private readonly string _logFile;

        public GarbageCollectorService()
        {
            _studentRepository = new StudentRepository();
            _logFile = Path.Combine(DatabaseConnection.GetLogsFolder(), "app_log.txt");
        }

        public async Task RunAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    LogToFile("--- Garbage Collector Başladı ---");

                    // 1. Veritabanındaki tüm öğrencileri çek
                    var allStudents = _studentRepository.GetAll().ToList();
                    
                    // 1.5. FİZİKSEL KLASÖR DÜZENLEME (Eski düzendeki dosyaları ait oldukları sınıf klasörlerine taşı)
                    var studentsPdfFolder = DatabaseConnection.GetStudentsPdfFolder();
                    foreach (var student in allStudents)
                    {
                        if (!string.IsNullOrWhiteSpace(student.KunyePdfPath) && File.Exists(student.KunyePdfPath))
                        {
                            var dbClassFormat = student.Class?.Replace("/", "-");
                            if (!string.IsNullOrWhiteSpace(dbClassFormat))
                            {
                                var classFolder = Path.Combine(studentsPdfFolder, dbClassFormat);
                                var currentFolder = Path.GetDirectoryName(student.KunyePdfPath);

                                // Öğrencinin PDF'i kendi sınıf klasöründe değilse (örneğin kök dizinde kalmışsa)
                                if (!string.Equals(currentFolder, classFolder, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!Directory.Exists(classFolder)) Directory.CreateDirectory(classFolder);

                                    var fileName = Path.GetFileName(student.KunyePdfPath);
                                    var newFilePath = Path.Combine(classFolder, fileName);

                                    try
                                    {
                                        File.Copy(student.KunyePdfPath, newFilePath, overwrite: true);
                                        File.Delete(student.KunyePdfPath);

                                        // Veritabanını yeni yolla güncelle
                                        _studentRepository.UpdateKunyePdfPath(student.StudentNumber, newFilePath);
                                        
                                        // Bellekteki yolu da güncelle ki GC yetim sanıp silmesin
                                        student.KunyePdfPath = newFilePath;
                                        
                                        LogToFile($"Klasör Düzenleme: {fileName} -> {dbClassFormat} klasörüne taşındı.");
                                    }
                                    catch (Exception ex)
                                    {
                                        LogToFile($"UYARI: Klasör düzenleme başarısız ({fileName}): {ex.Message}");
                                    }
                                }
                            }
                        }
                    }

                    // Diskte bulunması gereken geçerli dosya yollarını topla (Büyük-küçük harf duyarsız kıyaslama için HashSet)
                    // Önemli: DB'deki yollar boş olabilir veya dosya zaten silinmiş olabilir. Bizim amacımız diskte olup da DB'de OLMAYANLARI bulmak.
                    var validPhotoPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var validPdfPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var student in allStudents)
                    {
                        if (!string.IsNullOrWhiteSpace(student.PhotoPath))
                            validPhotoPaths.Add(Path.GetFullPath(student.PhotoPath));

                        if (!string.IsNullOrWhiteSpace(student.KunyePdfPath))
                            validPdfPaths.Add(Path.GetFullPath(student.KunyePdfPath));
                    }

                    // 2. Fotoğraflar klasörünü temizle
                    int deletedPhotos = CleanFolder(DatabaseConnection.GetPhotosFolder(), validPhotoPaths, new[] { "default_avatar.jpg" });

                    // 3. Öğrenciler (Künye PDF'leri) klasörünü temizle
                    int deletedPdfs = CleanFolder(DatabaseConnection.GetStudentsPdfFolder(), validPdfPaths);

                    // 4. Boş klasörleri (Class/Sınıf klasörleri dahil) temizle
                    int deletedFolders = CleanEmptyFolders(DatabaseConnection.GetStudentsPdfFolder());

                    LogToFile($"--- Garbage Collector Tamamlandı --- [Silinen Fotoğraf: {deletedPhotos}, Silinen PDF: {deletedPdfs}, Silinen Klasör: {deletedFolders}]");
                }
                catch (Exception ex)
                {
                    LogToFile($"HATA (Garbage Collector): {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Belirtilen klasördeki (ve alt klasörlerdeki) dosyaları tarar, geçerli dosyalar setinde (validPaths) olmayanları siler.
        /// </summary>
        private int CleanFolder(string folderPath, HashSet<string> validPaths, string[]? ignoreFiles = null)
        {
            if (!Directory.Exists(folderPath)) return 0;

            int deletedCount = 0;
            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);

                // Yoksayılacak dosyalar (Örn: default_avatar.jpg)
                if (ignoreFiles != null && ignoreFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fullPath = Path.GetFullPath(file);

                // Eğer dosya veritabanındaki kayıtlarda yoksaysa "yetim"dir, silinmelidir.
                if (!validPaths.Contains(fullPath))
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                        LogToFile($"Yetim Dosya Silindi: {file}");
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"UYARI: Dosya silinemedi ({file}). Hata: {ex.Message}");
                    }
                }
            }

            return deletedCount;
        }

        /// <summary>
        /// Belirtilen dizin altındaki içi boş klasörleri saptar ve siler.
        /// İşlem alt klasörlerden yukarıya (bottom-up) doğru yapılır.
        /// </summary>
        private int CleanEmptyFolders(string startLocation)
        {
            if (!Directory.Exists(startLocation)) return 0;

            int deletedCount = 0;

            foreach (var dir in Directory.GetDirectories(startLocation, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
            {
                if (Directory.Exists(dir))
                {
                    try
                    {
                        if (Directory.GetFileSystemEntries(dir).Length == 0)
                        {
                            Directory.Delete(dir);
                            deletedCount++;
                            LogToFile($"Boş Klasör Silindi: {dir}");
                        }
                    }
                    catch (Exception ex)
                    {
                         LogToFile($"UYARI: Klasör silinemedi ({dir}). Hata: {ex.Message}");
                    }
                }
            }

            return deletedCount;
        }

        private void LogToFile(string message)
        {
            try
            {
                File.AppendAllText(_logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Log failed, ignore
            }
        }
    }
}
