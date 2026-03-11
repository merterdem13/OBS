using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using OBS.ViewModels;

namespace OBS.Views
{
    public partial class TeamManagementWindow
    {
        public TeamManagementWindow()
        {
            Opacity = 0;
            InitializeComponent();
            var vm = new TeamManagementViewModel();
            DataContext = vm;
            vm.PropertyChanged += Vm_PropertyChanged;

            Loaded += TeamManagementWindow_Loaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Helpers.WindowFlashFixer.Apply(this);
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TeamManagementViewModel.EditingTeam))
            {
                if (DataContext is TeamManagementViewModel vm && vm.EditingTeam != null)
                {
                    InlineEditComponent.LoadTeam(vm.EditingTeam);
                }
            }
        }

        private void InlineEditComponent_EditClosed(object sender, bool hasChanges)
        {
            if (DataContext is TeamManagementViewModel vm)
            {
                vm.CloseEditView(hasChanges);
            }
        }

        private void InlineCreateComponent_CreateClosed(object sender, bool hasChanges)
        {
            if (DataContext is TeamManagementViewModel vm)
            {
                vm.CloseCreateView(hasChanges);
            }
        }

        private void TeamManagementWindow_Loaded(object sender, RoutedEventArgs e)
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

            // Yüzer buton konumunu hatırla
            HomeButtonTranslate.Y = Helpers.LocalSettings.Current.FloatingButtonVerticalOffset;
        }

        private void OnElementIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement element && (bool)e.NewValue == true)
            {
                // When an element becomes visible, slide it in from the right / fade it in slightly
                var translate = new TranslateTransform(20, 0);
                element.RenderTransform = translate;
                element.Opacity = 0;

                var sb = new Storyboard();
                
                var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                
                var translateAnim = new DoubleAnimation(20, 0, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                Storyboard.SetTarget(opacityAnim, element);
                Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));

                Storyboard.SetTarget(translateAnim, element);
                Storyboard.SetTargetProperty(translateAnim, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

                sb.Children.Add(opacityAnim);
                sb.Children.Add(translateAnim);
                sb.Begin();
            }
        }

        // ── Sürükleme Mantığı — Home Buton ──────────────────────────────────
        private bool _isDragging = false;
        private Point _clickPosition;
        private double _initialTranslateY;

        private void OnHomeButtonPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                _isDragging = false;
                _clickPosition = e.GetPosition(this);
                _initialTranslateY = HomeButtonTranslate.Y;
                HomeButton.CaptureMouse();
            }
        }

        private void OnHomeButtonPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (HomeButton.IsMouseCaptured)
            {
                Point currentPosition = e.GetPosition(this);
                double deltaY = currentPosition.Y - _clickPosition.Y;

                if (!_isDragging && Math.Abs(deltaY) > 5)
                {
                    _isDragging = true;
                }

                if (_isDragging)
                {
                    double newTranslateY = _initialTranslateY + deltaY;

                    // Alt sınırı — 30px mesafe bıraktık
                    double maxMove = (this.ActualHeight / 2) - (HomeButton.ActualHeight / 2) - 30;
                    double minMove = -(this.ActualHeight / 2) + (HomeButton.ActualHeight / 2) + 60;

                    if (newTranslateY > maxMove) newTranslateY = maxMove;
                    if (newTranslateY < minMove) newTranslateY = minMove;

                    HomeButtonTranslate.Y = newTranslateY;
                    Helpers.LocalSettings.Current.FloatingButtonVerticalOffset = newTranslateY;
                }
            }
        }

        private void OnHomeButtonPreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (HomeButton.IsMouseCaptured)
            {
                HomeButton.ReleaseMouseCapture();

                if (!_isDragging)
                {
                    // Manuel click tetikleme
                    if (HomeButton.Command != null && HomeButton.Command.CanExecute(HomeButton.CommandParameter))
                    {
                        HomeButton.Command.Execute(HomeButton.CommandParameter);
                    }
                }
                else
                {
                    // Sürükleme bittiğinde konumu kalıcı olarak kaydet
                    Helpers.LocalSettings.Save();
                }
                
                e.Handled = true;
                _isDragging = false;
            }
        }
    }
}
