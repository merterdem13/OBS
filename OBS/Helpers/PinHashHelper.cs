using System;
using System.Security.Cryptography;
using System.Text;

namespace OBS.Helpers
{
    /// <summary>
    /// PIN hash'leme ve doğrulama için yardımcı sınıf.
    /// İSTEM METNİ GEREĞİ: PIN hash'lenerek DB'de saklanır.
    /// SHA256 kullanarak güvenli hash oluşturur.
    /// </summary>
    public static class PinHashHelper
    {
        /// <summary>
        /// PIN'i SHA256 ile hash'ler.
        /// </summary>
        /// <param name="pin">4 haneli PIN</param>
        /// <returns>Hash'lenmiş PIN (hex string)</returns>
        public static string HashPin(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
                throw new ArgumentException("PIN boş olamaz", nameof(pin));

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(pin);
            var hash = sha256.ComputeHash(bytes);
            
            // Byte array'i hex string'e çevir
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
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

            var inputHash = HashPin(pin);
            return inputHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
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
