using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OBS.Models;
using OBS.Services;
using OBS.ViewModels;

namespace OBS.Views.Components
{
    public partial class CreateTeamComponent : UserControl
    {
        public event EventHandler<bool>? CreateClosed;

        public CreateTeamComponent()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            AttachViewModelHandlers(DataContext as CreateTeamViewModel);
        }

        private void OnSearchResultDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not CreateTeamViewModel viewModel || SearchResultsList.SelectedItem is not Student student)
            {
                return;
            }

            if (viewModel.AddStudentCommand.CanExecute(student))
            {
                viewModel.AddStudentCommand.Execute(student);
            }
        }

        private void OnCloseRequested(object? sender, bool hasChanges)
        {
            CreateClosed?.Invoke(this, hasChanges);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachViewModelHandlers(e.OldValue as CreateTeamViewModel);
            AttachViewModelHandlers(e.NewValue as CreateTeamViewModel);
        }

        private void AttachViewModelHandlers(CreateTeamViewModel? viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            viewModel.CloseRequested += OnCloseRequested;
            viewModel.ToastRequested += OnToastRequested;
        }

        private void DetachViewModelHandlers(CreateTeamViewModel? viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            viewModel.CloseRequested -= OnCloseRequested;
            viewModel.ToastRequested -= OnToastRequested;
        }

        private static void OnToastRequested(object? sender, CreateTeamToastRequestedEventArgs e)
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
