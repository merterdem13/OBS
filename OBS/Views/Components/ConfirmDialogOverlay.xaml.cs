using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Wpf.Ui.Controls;

namespace OBS.Views.Components
{
    public partial class ConfirmDialogOverlay : UserControl
    {
        private TaskCompletionSource<bool>? _tcs;

        public ConfirmDialogOverlay()
        {
            InitializeComponent();
        }

        public Task<bool> ShowAsync(
            string title,
            string message,
            string confirmText = "Evet",
            string cancelText = "Vazgeç",
            SymbolRegular icon = SymbolRegular.Warning24,
            string iconColor = "#f97316")
        {
            _tcs = new TaskCompletionSource<bool>();

            DialogTitle.Text = title;
            DialogMessage.Text = message;
            ConfirmButton.Content = confirmText;
            CancelButton.Content = cancelText;
            DialogIcon.Symbol = icon;
            DialogIcon.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(iconColor));

            Visibility = Visibility.Visible;

            var fadeIn = (Storyboard)Resources["FadeInStoryboard"];
            fadeIn.Begin();

            return _tcs.Task;
        }

        private void Close(bool result)
        {
            var fadeOut = (Storyboard)Resources["FadeOutStoryboard"];
            fadeOut.Completed += OnFadeOutCompleted;
            fadeOut.Begin();

            void OnFadeOutCompleted(object? sender, System.EventArgs e)
            {
                fadeOut.Completed -= OnFadeOutCompleted;
                Visibility = Visibility.Collapsed;
                _tcs?.TrySetResult(result);
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e) => Close(true);

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close(false);

        private void Backdrop_Click(object sender, MouseButtonEventArgs e) => Close(false);
    }
}
