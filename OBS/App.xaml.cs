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

            AppDomain.CurrentDomain.UnhandledException += (s, ev) => 
            {
                System.IO.File.WriteAllText("crash3.txt", ev.ExceptionObject.ToString());
            };
            
            DispatcherUnhandledException += (s, ev) => 
            {
                System.IO.File.WriteAllText("crash_disp3.txt", ev.Exception.ToString());
            };

            TaskScheduler.UnobservedTaskException += (s, ev) =>
            {
                System.IO.File.WriteAllText("crash_task3.txt", ev.Exception.ToString());
            };

            var settingsRepo = new SettingsRepository();
            var savedTheme = settingsRepo.GetSetting("Theme") ?? "Light";
            Helpers.ThemeManager.ApplyTheme(savedTheme);

            var loginWindow = new Views.LoginWindow();
            loginWindow.Show();
        }
    }
}
