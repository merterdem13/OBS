using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OBS.ViewModels
{
    public partial class GlobalState : ObservableObject
    {
        private static readonly Lazy<GlobalState> _instance = new(() => new GlobalState());
        public static GlobalState Instance => _instance.Value;

        [ObservableProperty]
        private string _currentTheme;

        private GlobalState()
        {
            var settingsRepo = new OBS.DataAccess.SettingsRepository();
            _currentTheme = settingsRepo.GetSetting("Theme") ?? "Light";
        }

        [ObservableProperty]
        private bool _isSettingsOverlayVisible = false;

        [ObservableProperty]
        private int _selectedSettingsIndex = 0;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private double _loadingProgress = 0;

        [ObservableProperty]
        private string _loadingProgressText = "%0";

        [ObservableProperty]
        private bool _isUpdateAvailable = false;

        [ObservableProperty]
        private string _updateVersion = string.Empty;

        [ObservableProperty]
        private bool _isUpdateDownloading = false;

        [ObservableProperty]
        private int _updateDownloadProgress = 0;

        public Func<string, string, string, string, Task<bool>>? ConfirmAsync { get; set; }

        public void ShowLoading(string message = "Yükleniyor...", double progress = 0)
        {
            LoadingProgressText = message;
            LoadingProgress = progress;
            IsLoading = true;
        }

        public void HideLoading()
        {
            IsLoading = false;
        }

        [RelayCommand]
        private void CloseSettingsOverlay()
        {
            IsSettingsOverlayVisible = false;
        }

        [RelayCommand]
        private void ChangeTheme(string themeString)
        {
            if (CurrentTheme == themeString) return;

            CurrentTheme = themeString;
            
            var settingsRepo = new OBS.DataAccess.SettingsRepository();
            settingsRepo.SetSetting("Theme", themeString);
            
            Helpers.ThemeManager.ApplyTheme(themeString);

            // Hard Reset!
            var currentWindow = System.Windows.Application.Current.MainWindow as Views.MainWindow;
            if (currentWindow != null)
            {
                var newWindow = new Views.MainWindow();
                newWindow.Left = currentWindow.Left;
                newWindow.Top = currentWindow.Top;

                IsSettingsOverlayVisible = true;
                SelectedSettingsIndex = 2; // 2 = DisplaySettingsPage

                System.Windows.Application.Current.MainWindow = newWindow;
                newWindow.Opacity = 0;
                newWindow.Show();
                currentWindow.Close();
            }
        }

        public Func<Task>? OnCheckForUpdateAction { get; set; }
        public Func<Task>? OnResetSystemAction { get; set; }
        public Func<Task>? OnImportKunyePdfAction { get; set; }

        [RelayCommand]
        private async Task CheckForUpdate()
        {
            if (OnCheckForUpdateAction != null)
                await OnCheckForUpdateAction.Invoke();
        }

        [RelayCommand]
        private async Task ResetSystem()
        {
            if (OnResetSystemAction != null)
                await OnResetSystemAction.Invoke();
        }

        [RelayCommand]
        private async Task ImportKunyePdf()
        {
            if (OnImportKunyePdfAction != null)
                await OnImportKunyePdfAction.Invoke();
        }
    }
}
