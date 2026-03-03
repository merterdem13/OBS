using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OBS.Helpers
{
    /// <summary>
    /// Türkçe karakterleri İngilizceye çevirir ve dosya adı için uygun format oluşturur.
    /// İSTEM METNİ GEREĞİ: Fotoğraf isimleri {Ad_soyad_numara}.jpg formatında olmalı
    /// ve Türkçe karakterler slugify edilmelidir (ç→c, ş→s, ğ→g, boşluk→_).
    /// </summary>
    public static class SlugifyHelper
    {
        /// <summary>
        /// Metni slugify eder: Türkçe karakterleri İngilizceye çevirir,
        /// boşlukları alt tire ile değiştirir, özel karakterleri kaldırır.
        /// </summary>
        /// <param name="text">Slugify edilecek metin</param>
        /// <returns>Slugify edilmiş metin</returns>
        public static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Türkçe karakterleri İngilizceye çevir
            var normalizedText = ReplaceTurkishCharacters(text);

            // Küçük harfe çevir
            normalizedText = normalizedText.ToLowerInvariant();

            // Birden fazla boşluğu tek boşluğa indir
            normalizedText = Regex.Replace(normalizedText, @"\s+", " ");

            // Boşlukları alt tire ile değiştir
            normalizedText = normalizedText.Replace(" ", "_");

            // Sadece harf, rakam ve alt tire bırak
            normalizedText = Regex.Replace(normalizedText, @"[^a-z0-9_]", "");

            // Birden fazla alt tireyi tek alt tire yap
            normalizedText = Regex.Replace(normalizedText, @"_+", "_");

            // Baştaki ve sondaki alt tireleri temizle
            normalizedText = normalizedText.Trim('_');

            return normalizedText;
        }

        /// <summary>
        /// Türkçe karakterleri İngilizce karşılıklarına çevirir.
        /// </summary>
        private static string ReplaceTurkishCharacters(string text)
        {
            var map = new Dictionary<char, char>
            {
                { 'ç', 'c' }, { 'Ç', 'C' },
                { 'ğ', 'g' }, { 'Ğ', 'G' },
                { 'ı', 'i' }, { 'İ', 'I' },
                { 'ö', 'o' }, { 'Ö', 'O' },
                { 'ş', 's' }, { 'Ş', 'S' },
                { 'ü', 'u' }, { 'Ü', 'U' }
            };

            var sb = new StringBuilder();
            foreach (var ch in text)
            {
                sb.Append(map.ContainsKey(ch) ? map[ch] : ch);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Öğrenci bilgilerinden fotoğraf dosya adı oluşturur.
        /// Format: {Ad_soyad_numara}.jpg
        /// Örnek: ali_veli_12345.jpg
        /// </summary>
        public static string CreatePhotoFileName(string firstName, string lastName, string studentNumber)
        {
            var slugifiedFirst = Slugify(firstName);
            var slugifiedLast = Slugify(lastName);
            var slugifiedNumber = Slugify(studentNumber);

            return $"{slugifiedFirst}_{slugifiedLast}_{slugifiedNumber}.jpg";
        }
    }
}
