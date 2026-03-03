using System.Windows;
using OBS.DataAccess;
using Wpf.Ui.Appearance;

namespace OBS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ApplicationThemeManager.Apply(ApplicationTheme.Light);

            DatabaseConnection.EnsureDatabase();

            var loginWindow = new Views.LoginWindow();
            loginWindow.Show();
        }
    }
}
