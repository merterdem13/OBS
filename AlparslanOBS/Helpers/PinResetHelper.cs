using System;
using System.IO;

namespace AlparslanOBS.Helpers
{
    /// <summary>
    /// PIN reset dosyası (admin.reset) kontrolü için yardımcı sınıf.
    /// İSTEM METNİ GEREĞİ: 
    /// - Uygulama klasörüne "admin.reset" dosyası eklenirse PIN bypass edilir
    /// - Yeni PIN oluşturma ekranı açılır
    /// - Reset işlemi sonunda dosya otomatik silinir
    /// </summary>
    public static class PinResetHelper
    {
        private const string ResetFileName = "admin.reset";

        /// <summary>
        /// Uygulama klasöründeki reset dosyasının yolunu döndürür.
        /// </summary>
        private static string GetResetFilePath()
        {
            var appFolder = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location
            );
            return Path.Combine(appFolder ?? "", ResetFileName);
        }

        /// <summary>
        /// Reset dosyasının var olup olmadığını kontrol eder.
        /// </summary>
        public static bool ResetFileExists()
        {
            return File.Exists(GetResetFilePath());
        }

        /// <summary>
        /// Reset dosyasını siler.
        /// İSTEM METNİ GEREĞİ: Reset işlemi tamamlandıktan sonra dosya otomatik silinir.
        /// </summary>
        public static void DeleteResetFile()
        {
            try
            {
                var path = GetResetFilePath();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Silme hatalarını yoksay
            }
        }

        /// <summary>
        /// Test amaçlı reset dosyası oluşturur.
        /// Prodüksiyonda kullanılmaz, sadece test için.
        /// </summary>
        public static void CreateResetFile()
        {
            try
            {
                File.WriteAllText(GetResetFilePath(), "Reset PIN");
            }
            catch
            {
                // Oluşturma hatalarını yoksay
            }
        }
    }
}
