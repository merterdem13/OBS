using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using OBS.ViewModels;
using Wpf.Ui.Controls;

namespace OBS.Views
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            Opacity = 0;
            InitializeComponent();

            var vm = new ShellViewModel();
            GlobalState.Instance.ConfirmAsync = ShowConfirmDialogAsync;
            DataContext = vm;

            Loaded += MainWindow_Loaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Helpers.WindowFlashFixer.Apply(this);
        }

        private async Task<bool> ShowConfirmDialogAsync(
            string title, string message, string confirmText, string cancelText)
        {
            return await ConfirmDialog.ShowAsync(
                title, message, confirmText, cancelText,
                title.Contains("Son") ? SymbolRegular.ErrorCircle24 : SymbolRegular.Warning24,
                title.Contains("Son") ? "#ef4444" : "#f97316");
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Opacity = 0;
            var sb = new Storyboard();
            var anim = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            sb.Children.Add(anim);
            Storyboard.SetTarget(anim, this);
            Storyboard.SetTargetProperty(anim, new PropertyPath("Opacity"));
            sb.Begin();

            // Çöp Toplayıcı ve Klasör Düzenleyicisini Uygulama Açılışında Çalıştır
            _ = new OBS.Services.GarbageCollectorService().RunAsync();
        }

        public async void CheckAndShowReleaseNotes()
        {
            await Task.Delay(500); // Giriş animasyonundan biraz sonra çıkması için
            try
            {
                var releaseNotesPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ReleaseNotes.json");
                if (System.IO.File.Exists(releaseNotesPath))
                {
                    var json = System.IO.File.ReadAllText(releaseNotesPath);
                    var viewModel = Newtonsoft.Json.JsonConvert.DeserializeObject<ViewModels.ReleaseNotesViewModel>(json);

                    if (viewModel != null && !string.IsNullOrEmpty(viewModel.Version))
                    {
                        var lastSeenVersion = Helpers.LocalSettings.Current.LastSeenReleaseNotesVersion;

                        if (string.IsNullOrEmpty(lastSeenVersion) || lastSeenVersion != viewModel.Version)
                        {
                            await ReleaseNotesOverlay.ShowAsync(viewModel);

                            Helpers.LocalSettings.Current.LastSeenReleaseNotesVersion = viewModel.Version;
                            Helpers.LocalSettings.Save();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Release notes gösterilirken hata oluştu: {ex.Message}");
            }
        }
    }
}