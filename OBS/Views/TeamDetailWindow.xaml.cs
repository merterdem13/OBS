using System;
using System.Windows;
using System.Windows.Media.Animation;
using OBS.ViewModels;

namespace OBS.Views
{
    public partial class TeamDetailWindow
    {
        public TeamDetailWindow(int teamId, string teamName, string category)
        {
            Opacity = 0;
            InitializeComponent();
            DataContext = new TeamDetailViewModel(teamId, teamName, category);

            Loaded += TeamDetailWindow_Loaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Helpers.WindowFlashFixer.Apply(this);
        }

        private void TeamDetailWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
    }
}
