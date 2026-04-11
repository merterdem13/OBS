using System.Windows.Controls;
using System.Windows.Input;
using OBS.ViewModels;

namespace OBS.Views.Components
{
    public partial class SettingsOverlay : UserControl
    {
        public SettingsOverlay()
        {
            InitializeComponent();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.F9)
            {
                return;
            }

            GlobalState.Instance.RegisterDebugShortcutKeyCommand.Execute(null);
            e.Handled = true;
        }
    }
}
