using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using OBS.ViewModels;

namespace OBS.Views
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
            Loaded += HomeView_Loaded;
        }

        private static bool _isFirstLoad = true;

        private void HomeView_Loaded(object sender, RoutedEventArgs e)
        {
            // Enter animation for view change
            if (_isFirstLoad)
            {
                Helpers.AnimationHelper.PlaySlideUpTransition(this);
                _isFirstLoad = false;
            }
            else
            {
                Helpers.AnimationHelper.PlaySlideFromLeftTransition(this);
            }

            FloatingButtonTranslate.Y = Helpers.LocalSettings.Current.FloatingButtonVerticalOffset;
            
            if (Helpers.LocalSettings.Current.ClearFavoritesButtonVerticalOffset == -9999 || Helpers.LocalSettings.Current.ClearFavoritesButtonVerticalOffset == -150)
            {
                double minMove = -(this.ActualHeight / 2) + (48 / 2.0) + 60; // 48 is button Height
                ClearFavButtonTranslate.Y = minMove;
                Helpers.LocalSettings.Current.ClearFavoritesButtonVerticalOffset = minMove;
                Helpers.LocalSettings.Save();
            }
            else
            {
                ClearFavButtonTranslate.Y = Helpers.LocalSettings.Current.ClearFavoritesButtonVerticalOffset;
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

                if (!_isDragging && Math.Abs(deltaY) > 5)
                {
                    _isDragging = true;
                }

                if (_isDragging)
                {
                    double newTranslateY = _initialTranslateY + deltaY;

                    var parentGrid = FloatingButton.Parent as Grid;
                    if (parentGrid != null)
                    {
                        double maxMove = (this.ActualHeight / 2) - (FloatingButton.ActualHeight / 2) - 30;
                        double minMove = -(this.ActualHeight / 2) + (FloatingButton.ActualHeight / 2) + 60;

                        if (newTranslateY > maxMove) newTranslateY = maxMove;
                        if (newTranslateY < minMove) newTranslateY = minMove;
                        
                        // Kesişme Önleme
                        if (ClearFavoritesFloatingButton.Visibility == Visibility.Visible)
                        {
                            if (Math.Abs(newTranslateY - ClearFavButtonTranslate.Y) < 55)
                            {
                                if (FloatingButtonTranslate.Y > ClearFavButtonTranslate.Y)
                                    newTranslateY = ClearFavButtonTranslate.Y + 55;
                                else
                                    newTranslateY = ClearFavButtonTranslate.Y - 55;
                            }
                        }
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
                    if (FloatingButton.Command != null && FloatingButton.Command.CanExecute(FloatingButton.CommandParameter))
                    {
                        FloatingButton.Command.Execute(FloatingButton.CommandParameter);
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

        // ── Sürükleme Mantığı — Clear Favorites Floating Buton ────────────────
        private bool _isFavDragging = false;
        private Point _favClickPosition;
        private double _favInitialTranslateY;

        private void OnClearFavButtonIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool isVisible && isVisible)
            {
                if (FloatingButton.Visibility == Visibility.Visible && this.ActualHeight > 0)
                {
                    if (Math.Abs(ClearFavButtonTranslate.Y - FloatingButtonTranslate.Y) < 55)
                    {
                        double newTranslateY;
                        if (ClearFavButtonTranslate.Y >= FloatingButtonTranslate.Y)
                            newTranslateY = FloatingButtonTranslate.Y + 55;
                        else
                            newTranslateY = FloatingButtonTranslate.Y - 55;

                        var parentGrid = ClearFavoritesFloatingButton.Parent as Grid;
                        if (parentGrid != null)
                        {
                            double buttonHeight = ClearFavoritesFloatingButton.ActualHeight > 0 ? ClearFavoritesFloatingButton.ActualHeight : 48;
                            double maxMove = (this.ActualHeight / 2) - (buttonHeight / 2) - 30;
                            double minMove = -(this.ActualHeight / 2) + (buttonHeight / 2) + 60;

                            if (newTranslateY > maxMove) newTranslateY = maxMove;
                            if (newTranslateY < minMove) newTranslateY = minMove;

                            if (Math.Abs(newTranslateY - FloatingButtonTranslate.Y) < 55)
                            {
                                if (newTranslateY == maxMove) newTranslateY = FloatingButtonTranslate.Y - 55;
                                else if (newTranslateY == minMove) newTranslateY = FloatingButtonTranslate.Y + 55;
                            }
                        }

                        ClearFavButtonTranslate.Y = newTranslateY;
                        Helpers.LocalSettings.Current.ClearFavoritesButtonVerticalOffset = newTranslateY;
                        Helpers.LocalSettings.Save();
                    }
                }
            }
        }

        private void OnClearFavButtonPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                _isFavDragging = false;
                _favClickPosition = e.GetPosition(this);
                _favInitialTranslateY = ClearFavButtonTranslate.Y;
                ClearFavoritesFloatingButton.CaptureMouse();
            }
        }

        private void OnClearFavButtonPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ClearFavoritesFloatingButton.IsMouseCaptured)
            {
                Point currentPosition = e.GetPosition(this);
                double deltaY = currentPosition.Y - _favClickPosition.Y;

                if (!_isFavDragging && Math.Abs(deltaY) > 5)
                {
                    _isFavDragging = true;
                }

                if (_isFavDragging)
                {
                    double newTranslateY = _favInitialTranslateY + deltaY;

                    var parentGrid = ClearFavoritesFloatingButton.Parent as Grid;
                    if (parentGrid != null)
                    {
                        double maxMove = (this.ActualHeight / 2) - (ClearFavoritesFloatingButton.ActualHeight / 2) - 30;
                        double minMove = -(this.ActualHeight / 2) + (ClearFavoritesFloatingButton.ActualHeight / 2) + 60;

                        if (newTranslateY > maxMove) newTranslateY = maxMove;
                        if (newTranslateY < minMove) newTranslateY = minMove;

                        // Kesişme Önleme
                        if (FloatingButton.Visibility == Visibility.Visible)
                        {
                            if (Math.Abs(newTranslateY - FloatingButtonTranslate.Y) < 55)
                            {
                                if (ClearFavButtonTranslate.Y > FloatingButtonTranslate.Y)
                                    newTranslateY = FloatingButtonTranslate.Y + 55;
                                else
                                    newTranslateY = FloatingButtonTranslate.Y - 55;
                            }
                        }
                    }

                    ClearFavButtonTranslate.Y = newTranslateY;
                    Helpers.LocalSettings.Current.ClearFavoritesButtonVerticalOffset = newTranslateY;
                }
            }
        }

        private void OnClearFavButtonPreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ClearFavoritesFloatingButton.IsMouseCaptured)
            {
                ClearFavoritesFloatingButton.ReleaseMouseCapture();

                if (!_isFavDragging)
                {
                    if (ClearFavoritesFloatingButton.Command != null && ClearFavoritesFloatingButton.Command.CanExecute(null))
                    {
                        ClearFavoritesFloatingButton.Command.Execute(null);
                    }
                }
                else
                {
                    Helpers.LocalSettings.Save();
                }

                e.Handled = true;
                _isFavDragging = false;
            }
        }
    }
}
