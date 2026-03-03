using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace OBS.Converters
{
    /// <summary>
    /// Bu sınıf, dosya yolundan (string) gelen görselleri UI'da göstermek üzere BitmapImage'e dönüştürür.
    /// Bellek sızıntılarını (Memory Leak) önlemek için DecodePixelWidth kullanılarak görseller optimize edilir.
    /// </summary>
    public class PathToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string imagePath = value as string;

            // Eğer dosya yolu boşsa veya dosya diskte yoksa null döndür (Fallback senaryosu)
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return null;

            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();

                // CacheOption.OnLoad: Görsel belleğe alındıktan sonra dosya üzerindeki kilidi kaldırır.
                // Bu sayede program çalışırken diskteki fotoğraf manuel olarak silinebilir veya değiştirilebilir.
                bitmap.CacheOption = BitmapCacheOption.OnLoad;

                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);

                // KİTSEL PERFORMANS ADIMI: Görüntüyü orijinal boyutuyla değil, max 200px genişliğinde belleğe al.
                // Listelerde yüzlerce öğrenci fotoğrafı yüklendiğinde RAM şişmesini engeller.
                bitmap.DecodePixelWidth = 200;

                bitmap.EndInit();

                // Freezable nesneleri dondurmak, WPF'in render performansını ciddi şekilde artırır.
                bitmap.Freeze();

                return bitmap;
            }
            catch (Exception)
            {
                // Okuma sırasında olası bir dosya bozulması veya erişim engeli durumunda çökmeyi önle
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Tek yönlü (OneWay) binding yapacağımız için ConvertBack metoduna ihtiyacımız yok.
            throw new NotImplementedException();
        }
    }
}