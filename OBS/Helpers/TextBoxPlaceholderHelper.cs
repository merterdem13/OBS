using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OBS.Helpers
{
    /// <summary>
    /// TextBox için placeholder (watermark) özelliği sağlar.
    /// İSTEM METNİ GEREĞİ: Arama çubuğunda "Öğrenci ara: Ad, Soyad, No" placeholder'ı olacak.
    /// </summary>
    public static class TextBoxPlaceholderHelper
    {
        // Attached Property: Placeholder
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.RegisterAttached(
                "Placeholder",
                typeof(string),
                typeof(TextBoxPlaceholderHelper),
                new PropertyMetadata(string.Empty, OnPlaceholderChanged));

        public static string GetPlaceholder(DependencyObject obj)
        {
            return (string)obj.GetValue(PlaceholderProperty);
        }

        public static void SetPlaceholder(DependencyObject obj, string value)
        {
            obj.SetValue(PlaceholderProperty, value);
        }

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.GotFocus -= RemovePlaceholder;
                textBox.LostFocus -= ShowPlaceholder;
                textBox.Loaded -= OnTextBoxLoaded;

                if (!string.IsNullOrEmpty((string)e.NewValue))
                {
                    textBox.GotFocus += RemovePlaceholder;
                    textBox.LostFocus += ShowPlaceholder;
                    textBox.Loaded += OnTextBoxLoaded;

                    if (!textBox.IsFocused)
                        ShowPlaceholderInternal(textBox);
                }
            }
        }

        private static void OnTextBoxLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrEmpty(textBox.Text))
            {
                ShowPlaceholderInternal(textBox);
            }
        }

        private static void ShowPlaceholder(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrEmpty(textBox.Text))
            {
                ShowPlaceholderInternal(textBox);
            }
        }

        private static void RemovePlaceholder(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var placeholder = GetPlaceholder(textBox);
                if (textBox.Text == placeholder)
                {
                    textBox.Text = string.Empty;
                    textBox.Foreground = Brushes.Black;
                }
            }
        }

        private static void ShowPlaceholderInternal(TextBox textBox)
        {
            var placeholder = GetPlaceholder(textBox);
            if (!string.IsNullOrEmpty(placeholder) && string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = placeholder;
                textBox.Foreground = Brushes.Gray;
            }
        }
    }
}
