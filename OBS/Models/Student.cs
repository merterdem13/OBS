namespace OBS.Models
{
    using System;

    /// <summary>
    /// Veritabanındaki "Students" (Öğrenciler) tablosunu temsil eden saf C# sınıfı.
    /// Merge (UPSERT) operasyonlarında 'StudentNumber' alanı baz alınacaktır.
    /// </summary>
    public class Student
    {
        /// <summary>
        /// Öğrencinin veritabanındaki benzersiz kayıt kimliği (Primary Key).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Öğrencinin Okul Numarası.
        /// KİTİK BİLGİ: Bu alan veritabanında UNIQUE (benzersiz) olacaktır. 
        /// Sisteme aynı numaralı bir öğrenci eklendiğinde (Merge işlemi), yeni kayıt açılmayacak, mevcut kayıt güncellenecektir.
        /// </summary>
        public string StudentNumber { get; set; } = string.Empty;

        /// <summary>
        /// Öğrencinin tam adı (Adı ve Soyadı).
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Öğrencinin adı.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Öğrencinin soyadı.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Öğrencinin şube/sınıf bilgisi (Örn: "10-A", "12-C").
        /// </summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// Öğrencinin sınıf seviyesi (string formatında).
        /// </summary>
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Öğrencinin sınıf numarası.
        /// </summary>
        public int ClassNo { get; set; }

        /// <summary>
        /// Öğrencinin kimlik numarası (TC No).
        /// </summary>
        public string TcNo { get; set; } = string.Empty;

        /// <summary>
        /// Öğrencinin doğum tarihi.
        /// </summary>
        public DateTime? BirthDate { get; set; }

        /// <summary>
        /// Öğrencinin cinsiyeti.
        /// </summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// Öğrencinin fotoğrafının fiziksel diskteki yolu.
        /// KURAL: Fotoğraflar 'AppData\Local\OBS_System\Photos' dizininde 'slugify' (temizlenmiş isim) formatıyla saklanır.
        /// </summary>
        public string PhotoPath { get; set; } = string.Empty;

        /// <summary>
        /// Öğrencinin künye PDF dosyasının fiziksel diskteki yolu.
        /// PDF'ler 'AppData\Local\OBS_System\Students' dizininde saklanır.
        /// </summary>
        public string KunyePdfPath { get; set; } = string.Empty;

        /// <summary>
        /// Öğrenciye ait velinin veritabanındaki kimliği (Foreign Key).
        /// PRAGMA foreign_keys = ON kuralı gereği Guardian tablosuyla sıkı sıkıya bağlıdır.
        /// </summary>
        public int? GuardianId { get; set; }

        /// <summary>
        /// Öğrenciye özel eklenen not.
        /// </summary>
        public string SpecialNote { get; set; } = string.Empty;

        // --- UI (Arayüz) İçin Yardımcı Özellikler ---
        // Not: Bu alan veritabanında bir kolon olarak tutulmaz. INNER JOIN sorgusu ile doldurulur.
        // StudentCardComponent.xaml içerisindeki Veli ismi gösterimini kolaylaştırmak için eklenmiştir.

        /// <summary>
        /// Arayüzde veli adını doğrudan göstermek için kullanılan yardımcı alan (Veritabanında tablo kolonu değildir).
        /// </summary>
        public string GuardianName { get; set; } = string.Empty;
    }
}
