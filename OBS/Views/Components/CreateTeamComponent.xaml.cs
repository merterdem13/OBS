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
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel),
            typeof(CreateTeamViewModel),
            typeof(CreateTeamComponent),
            new PropertyMetadata(null, OnViewModelChanged));

        public event EventHandler<bool>? CreateClosed;

        public CreateTeamViewModel? ViewModel
        {
            get => (CreateTeamViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public CreateTeamComponent()
        {
            InitializeComponent();
            ViewModel ??= new CreateTeamViewModel();
        }

        private void OnSearchResultDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel is not CreateTeamViewModel viewModel || SearchResultsList.SelectedItem is not Student student)
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

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CreateTeamComponent component)
            {
                return;
            }

            component.DetachViewModelHandlers(e.OldValue as CreateTeamViewModel);
            component.AttachViewModelHandlers(e.NewValue as CreateTeamViewModel);
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
