using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OBS.Models;
using OBS.Services;
using OBS.ViewModels;

namespace OBS.Views.Components
{
    public partial class EditTeamComponent : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel),
            typeof(EditTeamViewModel),
            typeof(EditTeamComponent),
            new PropertyMetadata(null, OnViewModelChanged));

        public event EventHandler<bool>? EditClosed;

        public EditTeamViewModel? ViewModel
        {
            get => (EditTeamViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public EditTeamComponent()
        {
            InitializeComponent();
            ViewModel ??= new EditTeamViewModel();
        }

        public void LoadTeam(TeamCardViewModel team)
        {
            if (ViewModel is EditTeamViewModel viewModel)
            {
                viewModel.LoadTeam(team);
            }
        }

        private void OnSearchResultDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel is not EditTeamViewModel viewModel || SearchResultsList.SelectedItem is not Student student)
            {
                return;
            }

            if (viewModel.AddStudentCommand.CanExecute(student))
            {
                viewModel.AddStudentCommand.Execute(student);
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            EditClosed?.Invoke(this, ViewModel is EditTeamViewModel viewModel && viewModel.HasChanges);
        }

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not EditTeamComponent component)
            {
                return;
            }

            component.DetachViewModelHandlers(e.OldValue as EditTeamViewModel);
            component.AttachViewModelHandlers(e.NewValue as EditTeamViewModel);
        }

        private void AttachViewModelHandlers(EditTeamViewModel? viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            viewModel.ToastRequested += OnToastRequested;
        }

        private void DetachViewModelHandlers(EditTeamViewModel? viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            viewModel.ToastRequested -= OnToastRequested;
        }

        private static void OnToastRequested(object? sender, EditTeamToastRequestedEventArgs e)
        {
            if (e.IsError)
            {
                ToastService.ShowError(e.Message);
                return;
            }

            ToastService.ShowSuccess(e.Message);
        }
    }
}
