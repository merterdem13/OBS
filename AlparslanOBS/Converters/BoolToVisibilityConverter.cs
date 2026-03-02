using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AlparslanOBS.Converters
{
    /// <summary>
    /// Boolean (true/false) değerlerini WPF'in Visibility (Visible/Collapsed) yapısına dönüştürür.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isVisible && isVisible)
            {
                return Visibility.Visible;
            }

            // False ise UI'da yer kaplamaması için Collapsed yapıyoruz. (Hidden yapmıyoruz)
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}