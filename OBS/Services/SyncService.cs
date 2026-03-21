using System;
using System.Collections.Generic;
using System.Linq;
using OBS.DataAccess;
using OBS.Models;

namespace OBS.Services
{
    public class SyncResult
    {
        public bool IsUpdate { get; set; }
        public string OldClassName { get; set; } = string.Empty;
        public string NewClassName { get; set; } = string.Empty;
        public List<string> StudentsToDelete { get; set; } = new();
    }

    public interface ISyncService
    {
        SyncResult AnalyzeClassSync(IEnumerable<string> newStudentNumbers, string newClassName);
    }

    public class SyncService : ISyncService
    {
        private readonly StudentRepository _studentRepository;

        public SyncService()
        {
            _studentRepository = new StudentRepository();
        }

        /// <summary>
        /// 5 Ortak Öğrenci Kuralı'nı çalıştırarak, yeni yüklenen sınıfın eski bir sınıfın güncel hali olup olmadığını tespit eder.
        /// Eğer güncellemeyse, eski sınıftaki ama yeni listede olmayan öğrencilerin listesini (silinmek üzere) döndürür.
        /// </summary>
        public SyncResult AnalyzeClassSync(IEnumerable<string> newStudentNumbers, string newClassName)
        {
            var result = new SyncResult { NewClassName = newClassName ?? string.Empty };
            var newNumbersList = newStudentNumbers.Where(n => !string.IsNullOrWhiteSpace(n)).Select(FileNameHelper.NormalizeStudentNumber).ToList();

            if (newNumbersList.Count < 5)
            {
                // Kıyaslamak için çok az öğrenci var, yeni sınıf olarak devam et.
                return result;
            }

            // Mevcut veritabanındaki tüm sınıfları ve o sınıftaki öğrencileri çek
            var allStudents = _studentRepository.GetAll().ToList();
            var classGroups = allStudents
                .Where(s => !string.IsNullOrWhiteSpace(s.Class))
                .GroupBy(s => s.Class)
                .ToList();

            string bestMatchClass = string.Empty;
            int maxCommonStudents = 0;

            // Her bir eski sınıf için ortak öğrenci sayısına bak
            foreach (var group in classGroups)
            {
                var oldNumbers = group.Select(s => s.StudentNumber).ToList();
                var commonCount = oldNumbers.Intersect(newNumbersList, StringComparer.OrdinalIgnoreCase).Count();

                if (commonCount >= 5 && commonCount > maxCommonStudents)
                {
                    maxCommonStudents = commonCount;
                    bestMatchClass = group.Key!;
                }
            }

            if (!string.IsNullOrEmpty(bestMatchClass) && maxCommonStudents >= 5)
            {
                result.IsUpdate = true;
                result.OldClassName = bestMatchClass;
                
                // Eğer yeni liste için verilmiş bir sınıf adı yoksa (örn: Künye PDF'inden toplu geliyorsa),
                // tespit edilen eski sınıfın adını devral (koru).
                if (string.IsNullOrWhiteSpace(result.NewClassName))
                {
                    result.NewClassName = bestMatchClass;
                }

                // Eski sınıfta olup da yeni listede olmayanları bul (Nakil Gidenler / Ayrılanlar)
                var oldClassStudents = classGroups.First(g => g.Key == bestMatchClass).Select(s => s.StudentNumber).ToList();
                result.StudentsToDelete = oldClassStudents.Except(newNumbersList, StringComparer.OrdinalIgnoreCase).ToList();
            }

            return result;
        }
    }
}
