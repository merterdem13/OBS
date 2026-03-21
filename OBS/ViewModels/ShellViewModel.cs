using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using OBS.Services;

namespace OBS.ViewModels
{
    public partial class ShellViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        public ShellViewModel()
        {
            // Inject from App global service
            _navigationService = OBS.App.NavigationService;
            _navigationService.CurrentViewChanged += () => OnPropertyChanged(nameof(CurrentView));
            
            // Register Navigation as a startup action
        }

        public ObservableObject? CurrentView => _navigationService.CurrentView;

        public GlobalState State => GlobalState.Instance;
    }
}
