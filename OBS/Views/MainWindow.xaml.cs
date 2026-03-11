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

            var vm = new MainViewModel();
            vm.ConfirmAsync = ShowConfirmDialogAsync;
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

            // Sürüm Notları (Release Notes) Kontrolü (Giriş animasyonundan hemen sonra)
            await Task.Delay(500);
            CheckAndShowReleaseNotes();

            // Yüzer buton konumunu hatırla
            FloatingButtonTranslate.Y = Helpers.LocalSettings.Current.FloatingButtonVerticalOffset;
        }

        private async void CheckAndShowReleaseNotes()
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

        private void OnStudentListScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 100)
            {
                if (DataContext is MainViewModel vm)
                    vm.LoadMoreStudentsCommand.Execute(null);
            }
        }

        // ── Sürükleme Mantığı — Floating Buton ──────────────────────────────
        private bool _isDragging = false;
        private Point _clickPosition;
        private double _initialTranslateY;

        private void OnFloatingButtonPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                _isDragging = false;
                _clickPosition = e.GetPosition(this);
                _initialTranslateY = FloatingButtonTranslate.Y;
                FloatingButton.CaptureMouse();
            }
        }

        private void OnFloatingButtonPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (FloatingButton.IsMouseCaptured)
            {
                Point currentPosition = e.GetPosition(this);
                double deltaY = currentPosition.Y - _clickPosition.Y;

                // Tıklama ile sürüklemeyi ayırt etmek için küçük bir eşik (threshold)
                if (!_isDragging && Math.Abs(deltaY) > 5)
                {
                    _isDragging = true;
                }

                if (_isDragging)
                {
                    double newTranslateY = _initialTranslateY + deltaY;

                    // Pencere sınırları kontrolü
                    var parentGrid = FloatingButton.Parent as Grid;
                    if (parentGrid != null)
                    {
                        // Alt sınırı (maxMove) — 30px mesafe bıraktık
                        double maxMove = (this.ActualHeight / 2) - (FloatingButton.ActualHeight / 2) - 30;
                        double minMove = -(this.ActualHeight / 2) + (FloatingButton.ActualHeight / 2) + 60;

                        if (newTranslateY > maxMove) newTranslateY = maxMove;
                        if (newTranslateY < minMove) newTranslateY = minMove;
                    }

                    FloatingButtonTranslate.Y = newTranslateY;
                    Helpers.LocalSettings.Current.FloatingButtonVerticalOffset = newTranslateY;
                }
            }
        }

        private void OnFloatingButtonPreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (FloatingButton.IsMouseCaptured)
            {
                FloatingButton.ReleaseMouseCapture();

                if (!_isDragging)
                {
                    // Eğer sürükleme yapılmadıysa butona tıklanmış demektir.
                    // Click olayını veya Command'i manuel tetikleyelim çünkü CaptureMouse click'i bozabiliyor.
                    if (FloatingButton.Command != null && FloatingButton.Command.CanExecute(FloatingButton.CommandParameter))
                    {
                        FloatingButton.Command.Execute(FloatingButton.CommandParameter);
                    }
                }
                else
                {
                    // Sürükleme bittiğinde konumu kalıcı olarak kaydet
                    Helpers.LocalSettings.Save();
                }
                
                e.Handled = true; // Sürükleme veya manuel click işlendi, event'i durdur.
                _isDragging = false;
            }
        }
    }
}