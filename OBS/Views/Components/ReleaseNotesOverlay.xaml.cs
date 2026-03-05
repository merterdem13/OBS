using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using OBS.ViewModels;

namespace OBS.Views.Components
{
    public partial class ReleaseNotesOverlay : UserControl
    {
        private TaskCompletionSource<bool>? _tcs;

        public ReleaseNotesOverlay()
        {
            InitializeComponent();
        }

        public Task<bool> ShowAsync(ReleaseNotesViewModel viewModel)
        {
            _tcs = new TaskCompletionSource<bool>();

            DataContext = viewModel;
            Visibility = Visibility.Visible;

            var fadeIn = (Storyboard)Resources["FadeInStoryboard"];
            fadeIn.Begin();

            return _tcs.Task;
        }

        private void Close()
        {
            var fadeOut = (Storyboard)Resources["FadeOutStoryboard"];
            fadeOut.Completed += OnFadeOutCompleted;
            fadeOut.Begin();

            void OnFadeOutCompleted(object? sender, EventArgs e)
            {
                fadeOut.Completed -= OnFadeOutCompleted;
                Visibility = Visibility.Collapsed;
                _tcs?.TrySetResult(true);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Backdrop_Click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}
