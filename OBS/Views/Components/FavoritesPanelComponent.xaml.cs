using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using OBS.ViewModels;

namespace OBS.Views.Components
{
    /// <summary>
    /// Favoriler Paneli Component - Sol alt köşe pop-up paneli.
    /// İSTEM METNİ GEREĞİ:
    /// - Fade in/out animasyonu
    /// - Parıldama efekti (favoriye eklenince)
    /// - Basınca açılır/kapanır
    /// - Toplu silme özelliği
    /// </summary>
    public partial class FavoritesPanelComponent : UserControl
    {
        public FavoritesPanelComponent()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Paneli gösterir (Fade in animasyonu ile).
        /// İSTEM METNİ GEREĞİ: Fade in/out animasyonu.
        /// </summary>
        public void Show()
        {
            var storyboard = (Storyboard)Resources["FadeInStoryboard"];
            storyboard.Begin(FavoritesPanelBorder);
        }

        /// <summary>
        /// Paneli gizler (Fade out animasyonu ile).
        /// </summary>
        public void Hide()
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(System.TimeSpan.FromMilliseconds(200))
            };

            fadeOut.Completed += (s, e) =>
            {
                FavoritesPanelBorder.Opacity = 0;
            };

            FavoritesPanelBorder.BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>
        /// Parıldama efekti oynatır (favoriye ekleme anında).
        /// İSTEM METNİ GEREĞİ: Parıldayan görsel efekt.
        /// </summary>
        public void PlaySparkleAnimation()
        {
            var storyboard = (Storyboard)Resources["SparkleStoryboard"];
            storyboard.Begin(FavoritesPanelBorder);
        }

        /// <summary>
        /// Kapat butonu tıklama eventi.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // ViewModel'deki kapatma komutunu tetikle
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.CloseFavoritesPanelCommand?.Execute(null);
            }
        }

        /// <summary>
        /// Favoriden çıkar butonu tıklama eventi.
        /// </summary>
        private void RemoveFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is StudentViewModel student)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ToggleFavoriteCommand?.Execute(student);
                }
            }
        }

        /// <summary>
        /// Tümünü temizle butonu tıklama eventi.
        /// İSTEM METNİ GEREĞİ: Toplu silme özelliği.
        /// </summary>
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            // Onay dialogu göster
            var result = MessageBox.Show(
                "Tüm favorileri temizlemek istediğinizden emin misiniz?",
                "Favorileri Temizle",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ClearAllFavoritesCommand?.Execute(null);
                }
            }
        }
    }
}
