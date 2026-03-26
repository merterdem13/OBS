using System;
using System.Linq;
using System.Windows;
using Wpf.Ui.Appearance;

namespace OBS.Helpers
{
    public static class ThemeManager
    {
        public static void ApplyTheme(string themeString)
        {
            if (string.IsNullOrEmpty(themeString))
                themeString = "Light";

            // Geriye dönük uyumluluk: Eski "Warm" temasını "Alternative" ile değiştir.
            if (themeString == "Warm")
                themeString = "Alternative";

            // 1. Wpf.Ui temasını güncelle (Light veya Dark)
            var uiTheme = (themeString == "Dark" || themeString == "Alternative") ? ApplicationTheme.Dark : ApplicationTheme.Light;
            ApplicationThemeManager.Apply(uiTheme);

            // 2. Özel renk sözlüğümüzü (Custom ResourceDictionary) bul ve değiştir
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var customThemeDict = dictionaries.FirstOrDefault(d => 
                d.Source != null && d.Source.OriginalString.Contains("/Themes/"));

            if (customThemeDict != null)
            {
                var newSource = new Uri($"pack://application:,,,/Themes/{themeString}Colors.xaml", UriKind.Absolute);
                if (customThemeDict.Source != newSource)
                {
                    customThemeDict.Source = newSource;
                }
            }
            else
            {
                // Eğer bulamazsa ekle
                dictionaries.Add(new ResourceDictionary 
                { 
                    Source = new Uri($"pack://application:,,,/Themes/{themeString}Colors.xaml", UriKind.Absolute) 
                });
            }
        }
    }
}
