using System;

namespace OBS.Helpers
{
    /// <summary>
    /// PIN hash'leme ve doğrulama için yardımcı sınıf.
    /// İSTEM METNİ GEREĞİ: PIN hash'lenerek DB'de saklanır.
    /// BCrypt.Net kullanarak güvenli hash oluşturur.
    /// </summary>
    public static class PinHashHelper
    {
        /// <summary>
        /// PIN'i BCrypt ile hash'ler.
        /// </summary>
        /// <param name="pin">4 haneli PIN</param>
        /// <returns>Hash'lenmiş PIN</returns>
        public static string HashPin(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
                throw new ArgumentException("PIN boş olamaz", nameof(pin));

            return BCrypt.Net.BCrypt.EnhancedHashPassword(pin, 12);
        }

        /// <summary>
        /// PIN doğrulaması yapar.
        /// </summary>
        /// <param name="pin">Girilen PIN</param>
        /// <param name="storedHash">Veritabanındaki hash'lenmiş PIN</param>
        /// <returns>Eşleşme durumu</returns>
        public static bool VerifyPin(string pin, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(pin) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.EnhancedVerify(pin, storedHash);
            }
            catch
            {
                // Eski veya geçersiz hash formatları hata fırlattığında false olarak döndürür
                return false;
            }
        }

        /// <summary>
        /// PIN formatını kontrol eder.
        /// İSTEM METNİ GEREĞİ: 4 haneli PIN olmalı.
        /// </summary>
        /// <param name="pin">Kontrol edilecek PIN</param>
        /// <returns>Geçerli mi?</returns>
        public static bool IsValidPinFormat(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
                return false;

            // 4 hane ve sadece rakam olmalı
            return pin.Length == 4 && int.TryParse(pin, out _);
        }
    }
}
