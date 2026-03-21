using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using OBS.ViewModels;

namespace OBS.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            Opacity = 0;
            InitializeComponent();
            DataContext = new LoginViewModel();
            Loaded += LoginWindow_Loaded;
        }


        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            Helpers.AnimationHelper.PlaySlideUpTransition(this);
        }
    }
}
