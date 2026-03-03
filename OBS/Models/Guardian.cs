namespace OBS.Models
{
    /// <summary>
    /// Veritabanındaki "Guardians" (Veliler) tablosunu temsil eden saf C# sınıfı.
    /// Öğrencilerin veli bilgilerini tutar.
    /// </summary>
    public class Guardian
    {
        /// <summary>
        /// Velinin veritabanındaki benzersiz kimliği (Primary Key).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Velinin tam adı (Adı ve Soyadı).
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Velinin iletişim numarası.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Velinin mesleği (İsteğe bağlı veri).
        /// </summary>
        public string JobTitle { get; set; }

        /// <summary>
        /// Velinin e-posta adresi (İsteğe bağlı veri).
        /// </summary>
        public string Email { get; set; }
    }
}