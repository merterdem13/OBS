using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using OBS.ViewModels;

namespace OBS.Views
{
    public partial class TeamManagementView : UserControl
    {
        public TeamManagementView()
        {
            InitializeComponent();
            Loaded += TeamManagementView_Loaded;
        }

        private void TeamManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            Helpers.AnimationHelper.PlaySlideFromRightTransition(this);

            HomeButtonTranslate.Y = Helpers.LocalSettings.Current.FloatingButtonVerticalOffset;
        }

        private void InlineCreateComponent_CreateClosed(object sender, bool hasChanges)
        {
            if (DataContext is TeamManagementViewModel vm)
            {
                vm.CloseCreateView(hasChanges);
            }
        }

        private void InlineEditComponent_EditClosed(object sender, bool hasChanges)
        {
            if (DataContext is TeamManagementViewModel vm)
            {
                vm.CloseEditView(hasChanges);
            }
        }

        private void OnElementIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                var sb = new Storyboard();
                var anim = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                Storyboard.SetTarget(anim, (UIElement)sender);
                Storyboard.SetTargetProperty(anim, new PropertyPath("Opacity"));
                sb.Children.Add(anim);
                sb.Begin();
            }
        }

        // ── Sürükleme Mantığı — Home Buton ──────────────────────────────
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

                    var parentGrid = HomeButton.Parent as Grid;
                    if (parentGrid != null)
                    {
                        double maxMove = (this.ActualHeight / 2) - (HomeButton.ActualHeight / 2) - 30;
                        double minMove = -(this.ActualHeight / 2) + (HomeButton.ActualHeight / 2) + 60;

                        if (newTranslateY > maxMove) newTranslateY = maxMove;
                        if (newTranslateY < minMove) newTranslateY = minMove;
                    }

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
                    if (HomeButton.Command != null && HomeButton.Command.CanExecute(HomeButton.CommandParameter))
                    {
                        HomeButton.Command.Execute(HomeButton.CommandParameter);
                    }
                }
                else
                {
                    Helpers.LocalSettings.Save();
                }
                
                e.Handled = true;
                _isDragging = false;
            }
        }
    }
}
