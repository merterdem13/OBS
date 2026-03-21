using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OBS.Services
{
    public interface INavigationService
    {
        ObservableObject? CurrentView { get; }
        void NavigateTo<TViewModel>() where TViewModel : ObservableObject, new();
        void NavigateTo(ObservableObject viewModel);
        void GoBack();
        bool CanGoBack { get; }
        event Action? CurrentViewChanged;
    }

    public class NavigationService : INavigationService
    {
        private readonly Dictionary<Type, ObservableObject> _viewCache = new();
        private readonly Stack<ObservableObject> _navigationHistory = new();
        
        private ObservableObject? _currentView;

        public ObservableObject? CurrentView
        {
            get => _currentView;
            private set
            {
                _currentView = value;
                CurrentViewChanged?.Invoke();
            }
        }

        public event Action? CurrentViewChanged;

        public bool CanGoBack => _navigationHistory.Count > 0;

        public void NavigateTo<TViewModel>() where TViewModel : ObservableObject, new()
        {
            if (!_viewCache.TryGetValue(typeof(TViewModel), out var viewModel))
            {
                viewModel = new TViewModel();
                _viewCache[typeof(TViewModel)] = viewModel;
            }

            NavigateTo(viewModel);
        }

        public void NavigateTo(ObservableObject viewModel)
        {
            if (CurrentView != null && CurrentView != viewModel)
            {
                _navigationHistory.Push(CurrentView);
            }

            CurrentView = viewModel;
        }

        public void GoBack()
        {
            if (CanGoBack)
            {
                var previousView = _navigationHistory.Pop();
                CurrentView = previousView;
            }
        }
    }
}
