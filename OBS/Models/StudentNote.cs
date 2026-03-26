using System;

namespace OBS.Models
{
    /// <summary>
    /// Veritabanındaki "StudentNotes" (Öğrenci Notları) tablosunu temsil eden saf C# sınıfı.
    /// Öğrenci tablosuyla StudentNumber üzerinden ilişikilidir.
    /// </summary>
    public class StudentNote
    {
        /// <summary>
        /// Notun veritabanındaki benzersiz kayıt kimliği (Primary Key).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Notun ait olduğu öğrencinin Okul Numarası (Foreign Key).
        /// </summary>
        public string StudentNumber { get; set; } = string.Empty;

        /// <summary>
        /// Özel Not içeriği.
        /// </summary>
        public string NoteText { get; set; } = string.Empty;

        /// <summary>
        /// Notun eklenme/oluşturulma tarihi.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
