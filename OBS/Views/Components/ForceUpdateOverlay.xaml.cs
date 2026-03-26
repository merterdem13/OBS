using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using OBS.ViewModels;

namespace OBS.Views.Components
{
    public partial class ForceUpdateOverlay : UserControl
    {
        public ForceUpdateOverlay()
        {
            InitializeComponent();

            GlobalState.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GlobalState.IsForceUpdateRequired))
                {
                    if (GlobalState.Instance.IsForceUpdateRequired)
                        ShowOverlay();
                }
                else if (e.PropertyName == nameof(GlobalState.ForceUpdateMessage))
                {
                    MessageText.Text = GlobalState.Instance.ForceUpdateMessage;
                }
                else if (e.PropertyName == nameof(GlobalState.ForceUpdateProgress))
                {
                    var progress = GlobalState.Instance.ForceUpdateProgress;
                    DownloadBar.Value = progress;
                    ProgressText.Text = $"%{progress}";
                }
            };
        }

        private void ShowOverlay()
        {
            MessageText.Text = GlobalState.Instance.ForceUpdateMessage;
            Visibility = Visibility.Visible;
            var sb = (Storyboard)Resources["FadeInStoryboard"];
            sb.Begin();
        }
    }
}
