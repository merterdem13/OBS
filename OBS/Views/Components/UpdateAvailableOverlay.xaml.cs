using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using OBS.ViewModels;

namespace OBS.Views.Components
{
    public partial class UpdateAvailableOverlay : UserControl
    {
        public UpdateAvailableOverlay()
        {
            InitializeComponent();
            DataContext = GlobalState.Instance;

            GlobalState.Instance.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(GlobalState.IsUpdateModalVisible))
                {
                    if (GlobalState.Instance.IsUpdateModalVisible)
                        Show();
                    else
                        Hide();
                }
            };
        }

        private void Show()
        {
            Visibility = Visibility.Visible;
            ((Storyboard)Resources["FadeInStoryboard"]).Begin();
        }

        private void Hide()
        {
            if (Visibility != Visibility.Visible)
                return;

            var fadeOut = (Storyboard)Resources["FadeOutStoryboard"];
            fadeOut.Completed += OnCompleted;
            fadeOut.Begin();

            void OnCompleted(object? sender, EventArgs e)
            {
                fadeOut.Completed -= OnCompleted;
                Visibility = Visibility.Collapsed;
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            App.UpdateCoordinator.DownloadFromNormalUpdateModal();
        }

        private void DeferButton_Click(object sender, RoutedEventArgs e)
        {
            App.UpdateCoordinator.DeferNormalUpdate();
        }
    }
}
