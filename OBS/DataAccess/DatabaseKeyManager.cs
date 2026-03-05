using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OBS.DataAccess
{
    /// <summary>
    /// DPAPI (Data Protection API) kullanarak SQLite (SQLCipher) şifreleme anahtarını
    /// üreten ve mevcut Windows kullanıcısı oturumuna özel olarak güvenli şekilde saklayan sınıf.
    /// </summary>
    public static class DatabaseKeyManager
    {
        private static readonly string _appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OBS_System"
        );
        private static readonly string _keyFilePath = Path.Combine(_appFolder, "db.key");

        /// <summary>
        /// DPAPI üzerinden korunmuş SQLite şifreleme anahtarını döndürür.
        /// Eğer anahtar henüz yoksa, yeni bir rastgele anahtar üretip kaydeder.
        /// </summary>
        /// <returns>AES-256 uyumlu Base64 string SQLite şifresi</returns>
        public static string GetOrCreateKey()
        {
            if (!Directory.Exists(_appFolder))
            {
                Directory.CreateDirectory(_appFolder);
            }

            if (File.Exists(_keyFilePath))
            {
                // Mevcut korumalı anahtarı oku ve çöz
                var encryptedBytes = File.ReadAllBytes(_keyFilePath);
                try
                {
                    var decryptedBytes = ProtectedData.Unprotect(
                        encryptedBytes,
                        null,
                        DataProtectionScope.CurrentUser); // Sadece bu Windows oturumu çözebilir
                        
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
                catch (CryptographicException)
                {
                    // DPAPI şifresi çözülemezse (ör: farklı bilgisayara kopyalanmışsa) hata fırlat
                    throw new UnauthorizedAccessException(
                        "Güvenlik İhlali: Veritabanı anahtarına erişim reddedildi. Bu dosya sadece orijinal oluşturulduğu Windows oturumunda açılabilir.");
                }
            }
            else
            {
                // Yeni, 256-bit (32 byte) kriptografik rastgele bir anahtar üret
                var rawKeyBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(rawKeyBytes);
                }

                // SQLite şifreleme için string formata (Base64) çevir
                var newKeyString = Convert.ToBase64String(rawKeyBytes);
                var newKeyStringBytes = Encoding.UTF8.GetBytes(newKeyString);

                // Anahtarı DPAPI ile şifrele ve kaydet
                var encryptedBytes = ProtectedData.Protect(
                    newKeyStringBytes,
                    null,
                    DataProtectionScope.CurrentUser);

                File.WriteAllBytes(_keyFilePath, encryptedBytes);

                // Anahtarı saklamak için dosya niteliklerini gizli yapalım
                var fileInfo = new FileInfo(_keyFilePath);
                fileInfo.Attributes |= FileAttributes.Hidden;

                return newKeyString;
            }
        }
    }
}
