using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using OBS.ViewModels;

namespace OBS.Views.Components
{
    public partial class ChangeRecoveryPinOverlay : UserControl
    {
        private GlobalState _globalState => GlobalState.Instance;

        public ChangeRecoveryPinOverlay()
        {
            InitializeComponent();
            _globalState.PropertyChanged += GlobalState_PropertyChanged;
        }

        private void GlobalState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GlobalState.IsChangeRecoveryPinOverlayVisible))
            {
                if (_globalState.IsChangeRecoveryPinOverlayVisible)
                {
                    this.Visibility = Visibility.Visible;
                    var storyboard = (Storyboard)Resources["FadeInStoryboard"];
                    storyboard.Begin();
                }
                else
                {
                    CloseAnimated();
                }
            }
        }

        private void CloseAnimated()
        {
            var storyboard = (Storyboard)Resources["FadeOutStoryboard"];
            if (storyboard == null)
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }

            storyboard.Completed += OnFadeOutCompleted;
            storyboard.Begin();

            void OnFadeOutCompleted(object? sender, System.EventArgs e)
            {
                storyboard.Completed -= OnFadeOutCompleted;
                this.Visibility = Visibility.Collapsed;
            }
        }

        private void Backdrop_Click(object sender, MouseButtonEventArgs e)
        {
            _globalState.CancelChangeRecoveryPinCommand.Execute(null);
        }
    }
}
