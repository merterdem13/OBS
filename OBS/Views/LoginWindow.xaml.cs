using System.Windows;
using OBS.ViewModels;

namespace OBS.Views
{
    public partial class LoginWindow
    {
        public LoginWindow()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();
        }
    }
}
