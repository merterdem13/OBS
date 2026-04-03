using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using OBS.Helpers;
using OBS.ViewModels;
using Wpf.Ui.Controls;

namespace OBS.Views
{
    public partial class MainWindow
    {
        private readonly Services.RecoverySetupService _recoverySetupService = new();
        private readonly Services.ReleaseNotesService _releaseNotesService = new();

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
            StartGarbageCollectorRun();
        }

        public async Task CheckAndShowRecoveryModalAsync()
        {
            try
            {
                await ShowRecoveryModalInternal();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CheckAndShowRecoveryModalAsync failed: {ex}");
            }
        }

        public async Task ShowPostLoginModalsAsync()
        {
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowPostLoginModalsAsync failed: {ex}");
            }
        }

        private void StartGarbageCollectorRun()
        {
            RunGarbageCollectorSafelyAsync().Forget(nameof(RunGarbageCollectorSafelyAsync));
        }

        private async Task RunGarbageCollectorSafelyAsync()
        {
            try
            {
                await new OBS.Services.GarbageCollectorService().RunAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GarbageCollectorService.RunAsync failed: {ex}");
            }
        }

        private Task<bool> ShowRecoveryModalInternal()
        {
            var startupState = _recoverySetupService.BuildStartupState();
            if (!startupState.ShouldShowModal || startupState.Payload == null)
            {
                return Task.FromResult(false);
            }

            var state = GlobalState.Instance;
            state.ChangeRecoveryPinTitle = startupState.Payload.Title;
            state.ChangeRecoveryPinMessage = startupState.Payload.Message;
            state.IsCurrentRecoveryPinRequired = startupState.Payload.IsCurrentRecoveryPinRequired;
            state.CurrentRecoveryPinInput = startupState.Payload.CurrentRecoveryPinInput;
            state.NewRecoveryPinInput = startupState.Payload.NewRecoveryPinInput;
            state.HasRecoveryPinError = startupState.Payload.HasRecoveryPinError;
            state.IsChangeRecoveryPinOverlayVisible = true;

            return Task.FromResult(true);
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
            var viewModel = _releaseNotesService.GetReleaseNotesToShow();
            if (viewModel == null)
            {
                return;
            }

            await ReleaseNotesOverlay.ShowAsync(viewModel);
            _releaseNotesService.MarkReleaseNotesAsSeen(viewModel.Version);
        }
    }
}
