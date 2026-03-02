using System;
using System.Windows;
using System.Windows.Media.Animation;
using AlparslanOBS.ViewModels;

namespace AlparslanOBS.Views
{
    public partial class TeamDetailWindow
    {
        public TeamDetailWindow(int teamId, string teamName, string category)
        {
            InitializeComponent();
            DataContext = new TeamDetailViewModel(teamId, teamName, category);

            Loaded += TeamDetailWindow_Loaded;
        }

        private void TeamDetailWindow_Loaded(object sender, RoutedEventArgs e)
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
    }
}
