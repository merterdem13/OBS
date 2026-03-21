using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace OBS.Views
{
    public partial class TeamDetailView : UserControl
    {
        public TeamDetailView()
        {
            InitializeComponent();
            Loaded += TeamDetailView_Loaded;
        }

        private void TeamDetailView_Loaded(object sender, RoutedEventArgs e)
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
