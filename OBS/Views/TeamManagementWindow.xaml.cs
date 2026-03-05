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
            InitializeComponent();
            var vm = new TeamManagementViewModel();
            DataContext = vm;
            vm.PropertyChanged += Vm_PropertyChanged;

            Loaded += TeamManagementWindow_Loaded;
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
    }
}
