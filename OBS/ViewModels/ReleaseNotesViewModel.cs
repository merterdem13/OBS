using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;

namespace OBS.ViewModels
{
    public partial class ReleaseNotesViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _version = string.Empty;

        [ObservableProperty]
        private string _date = string.Empty;

        [ObservableProperty]
        private List<string> _features = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasCriticalWarning))]
        private string? _criticalWarning;

        public bool HasCriticalWarning => !string.IsNullOrWhiteSpace(CriticalWarning);

        public Action? CloseAction { get; set; }

        [RelayCommand]
        private void Close()
        {
            CloseAction?.Invoke();
        }
    }
}
