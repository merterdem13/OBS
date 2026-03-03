using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OBS.DataAccess;
using OBS.Services;

namespace OBS.ViewModels
{
    public partial class TeamManagementViewModel : ObservableObject
    {
        private readonly TeamRepository _teamRepo;

        [ObservableProperty]
        private ObservableCollection<TeamCardViewModel> _teams = new();

        [ObservableProperty]
        private bool _isTopmost = true;

        public TeamManagementViewModel()
        {
            _teamRepo = new TeamRepository();
            RefreshTeams();
        }

        [RelayCommand]
        private void ToggleTopmost() => IsTopmost = !IsTopmost;

        [RelayCommand]
        private void OpenCreateTeam()
        {
            var dialog = new Views.CreateTeamWindow();
            dialog.Owner = Application.Current.Windows.OfType<Views.TeamManagementWindow>().FirstOrDefault();
            var result = dialog.ShowDialog();

            if (result == true)
            {
                RefreshTeams();
            }
        }

        [RelayCommand]
        private void OpenTeamDetail(TeamCardViewModel? team)
        {
            if (team is null) return;

            var currentWindow = Application.Current.Windows
                .OfType<Views.TeamManagementWindow>()
                .FirstOrDefault();

            var detailWindow = new Views.TeamDetailWindow(team.Id, team.TeamName, team.Category);
            if (currentWindow != null)
            {
                detailWindow.Left = currentWindow.Left;
                detailWindow.Top = currentWindow.Top;
            }
            detailWindow.Show();

            currentWindow?.Close();
        }

        [RelayCommand]
        private void EditTeam(TeamCardViewModel? team)
        {
            if (team is null) return;

            var dialog = new Views.EditTeamWindow(team.Id, team.TeamName, team.Category);
            dialog.Owner = Application.Current.Windows.OfType<Views.TeamManagementWindow>().FirstOrDefault();
            dialog.ShowDialog();

            if (dialog.HasChanges)
            {
                RefreshTeams();
            }
        }

        [RelayCommand]
        private async Task DeleteTeam(TeamCardViewModel? team)
        {
            if (team is null) return;

            var result = MessageBox.Show(
                $"\"{team.TeamName}\" takımı silinecek.\n\nDevam etmek istiyor musunuz?",
                "Takımı Sil",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                team.IsRemoving = true;
                await Task.Delay(300);
                _teamRepo.Delete(team.Id);
                RefreshTeams();
                ToastService.ShowSuccess($"\"{team.TeamName}\" takımı silindi.");
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Takım silme hatası: {ex.Message}");
            }
        }

        [RelayCommand]
        private void GoBackToMain()
        {
            var currentWindow = Application.Current.Windows.OfType<Views.TeamManagementWindow>().FirstOrDefault();

            var mainWindow = new Views.MainWindow();
            if (currentWindow != null)
            {
                mainWindow.Left = currentWindow.Left;
                mainWindow.Top = currentWindow.Top;
            }
            mainWindow.Show();

            currentWindow?.Close();
        }

        [RelayCommand]
        private void ExportTeamExcel(TeamCardViewModel? team)
        {
            if (team is null) return;

            try
            {
                var members = _teamRepo.GetMembers(team.Id);
                if (members.Count == 0)
                {
                    ToastService.ShowError("Takımda dışa aktarılacak oyuncu yok.");
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"{team.TeamName}",
                    DefaultExt = ".xlsx",
                    Filter = "Excel Dosyası|*.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    var guardianPhones = _teamRepo.GetGuardianPhones(team.Id);
                    var excelService = new ExcelExportService();
                    excelService.ExportTeamToExcel(team.TeamName, members, guardianPhones, dialog.FileName);
                    ToastService.ShowSuccess("Excel dosyası başarıyla oluşturuldu.");
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Excel aktarma hatası: {ex.Message}");
            }
        }

        public void RefreshTeams()
        {
            var allTeams = _teamRepo.GetAll();
            Teams = new ObservableCollection<TeamCardViewModel>(
                allTeams.Select(t => new TeamCardViewModel(t))
            );
        }
    }
}
