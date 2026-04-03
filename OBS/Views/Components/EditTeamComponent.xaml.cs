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
        public event EventHandler<bool>? EditClosed;

        public EditTeamComponent()
        {
            InitializeComponent();
            DataContext = new EditTeamViewModel();
            DataContextChanged += OnDataContextChanged;
            AttachViewModelHandlers(DataContext as EditTeamViewModel);
        }

        public void LoadTeam(TeamCardViewModel team)
        {
            if (DataContext is EditTeamViewModel viewModel)
            {
                viewModel.LoadTeam(team);
            }
        }

        private void OnSearchResultDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not EditTeamViewModel viewModel || SearchResultsList.SelectedItem is not Student student)
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
            EditClosed?.Invoke(this, DataContext is EditTeamViewModel viewModel && viewModel.HasChanges);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachViewModelHandlers(e.OldValue as EditTeamViewModel);
            AttachViewModelHandlers(e.NewValue as EditTeamViewModel);
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
