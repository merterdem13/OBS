using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using OBS.ViewModels;

namespace OBS.Views.Components
{
    public partial class DeveloperPinOverlay : UserControl
    {
        private GlobalState _globalState => GlobalState.Instance;

        public DeveloperPinOverlay()
        {
            InitializeComponent();
            _globalState.PropertyChanged += GlobalState_PropertyChanged;
        }

        private void GlobalState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GlobalState.IsDeveloperPinOverlayVisible))
            {
                if (_globalState.IsDeveloperPinOverlayVisible)
                {
                    this.Visibility = Visibility.Visible;
                    var storyboard = (Storyboard)Resources["FadeInStoryboard"];
                    storyboard.Begin();

                    // PasswordBox'a odaklan
                    Dispatcher.InvokeAsync(() => DevPinBox.Focus(), 
                        System.Windows.Threading.DispatcherPriority.Background);
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
            _globalState.CancelDeveloperPinCommand.Execute(null);
        }

        private void DevPinBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _globalState.VerifyDeveloperPinCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                _globalState.CancelDeveloperPinCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
