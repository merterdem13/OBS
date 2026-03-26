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

        public async void CheckAndShowRecoveryModal()
        {
            // Standalone çağrı (geliştirici erişimi vb.) için
            await ShowRecoveryModalInternal();
        }

        public async void ShowPostLoginModals()
        {
            await Task.Delay(1000); // Açılış animasyonunun bitmesini bekleyelim

            // 1. Öncelik: Recovery Modal
            bool recoveryShown = await ShowRecoveryModalInternal();

            // Recovery modal gösterildiyse, kapanmasını bekle
            if (recoveryShown)
            {
                await WaitForRecoveryModalClose();
                await Task.Delay(300); // Kapanış animasyonu için kısa bekleme
            }

            // 2. Sonra: Release Notes
            await ShowReleaseNotesInternal();
        }

        private async Task<bool> ShowRecoveryModalInternal()
        {
            try
            {
                var settingsRepo = new OBS.DataAccess.SettingsRepository();
                var hasSeenModal = settingsRepo.GetSetting("HasSeenRecoveryModal");
                
                if (string.IsNullOrWhiteSpace(hasSeenModal) || hasSeenModal != "true")
                {
                    GlobalState.Instance.ChangeRecoveryPinTitle = "İlk Kurulum - Kurtarma Kodu";
                    GlobalState.Instance.ChangeRecoveryPinMessage = "Uygulamaya hoş geldiniz! \nVarsayılan şifre sıfırlama (kurtarma) kodunuz '0000' olarak belirlenmiştir.\n\nGüvenliğiniz için bu kodu şimdi kişiselleştirebilirsiniz veya 'Vazgeç' diyerek daha sonra ayarlardan değiştirebilirsiniz.";
                    
                    GlobalState.Instance.IsCurrentRecoveryPinRequired = false;
                    GlobalState.Instance.CurrentRecoveryPinInput = string.Empty;
                    GlobalState.Instance.NewRecoveryPinInput = string.Empty;
                    GlobalState.Instance.HasRecoveryPinError = false;
                    
                    GlobalState.Instance.IsChangeRecoveryPinOverlayVisible = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Recovery modal gösterilirken hata oluştu: {ex.Message}");
            }
            return false;
        }

        private async Task WaitForRecoveryModalClose()
        {
            var tcs = new TaskCompletionSource<bool>();
            
            void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(GlobalState.IsChangeRecoveryPinOverlayVisible) 
                    && !GlobalState.Instance.IsChangeRecoveryPinOverlayVisible)
                {
                    tcs.TrySetResult(true);
                }
            }

            GlobalState.Instance.PropertyChanged += OnPropertyChanged;
            
            // Eğer zaten kapandıysa
            if (!GlobalState.Instance.IsChangeRecoveryPinOverlayVisible)
            {
                GlobalState.Instance.PropertyChanged -= OnPropertyChanged;
                return;
            }

            await tcs.Task;
            GlobalState.Instance.PropertyChanged -= OnPropertyChanged;
        }

        private async Task ShowReleaseNotesInternal()
        {
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