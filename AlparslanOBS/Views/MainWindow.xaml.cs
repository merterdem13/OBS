using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using AlparslanOBS.ViewModels;
using Wpf.Ui.Controls;

namespace AlparslanOBS.Views
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainViewModel();
            vm.ConfirmAsync = ShowConfirmDialogAsync;
            DataContext = vm;

            Loaded += MainWindow_Loaded;
        }

        private async Task<bool> ShowConfirmDialogAsync(
            string title, string message, string confirmText, string cancelText)
        {
            return await ConfirmDialog.ShowAsync(
                title, message, confirmText, cancelText,
                title.Contains("Son") ? SymbolRegular.ErrorCircle24 : SymbolRegular.Warning24,
                title.Contains("Son") ? "#ef4444" : "#f97316");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
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
        }

        private void OnStudentListScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 100)
            {
                if (DataContext is MainViewModel vm)
                    vm.LoadMoreStudentsCommand.Execute(null);
            }
        }
    }
}