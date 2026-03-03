using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OBS.DataAccess;
using OBS.Services;

namespace OBS.ViewModels
{
    public partial class TeamDetailViewModel : ObservableObject
    {
        private readonly TeamRepository _teamRepo;
        private readonly FavoriteRepository _favoriteRepo;
        private readonly int _teamId;

        [ObservableProperty]
        private string _teamName;

        [ObservableProperty]
        private string _category;

        [ObservableProperty]
        private string _memberCountText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<StudentViewModel> _members = new();

        [ObservableProperty]
        private bool _isTopmost = true;

        public TeamDetailViewModel(int teamId, string teamName, string category)
        {
            _teamRepo = new TeamRepository();
            _favoriteRepo = new FavoriteRepository();
            _teamId = teamId;
            _teamName = teamName;
            _category = category;

            LoadMembers();
        }

        [RelayCommand]
        private void GoBackToTeams()
        {
            var current = Application.Current.Windows
                .OfType<Views.TeamDetailWindow>()
                .FirstOrDefault();

            var teamWindow = new Views.TeamManagementWindow();
            if (current != null)
            {
                teamWindow.Left = current.Left;
                teamWindow.Top = current.Top;
            }
            teamWindow.Show();

            current?.Close();
        }

        [RelayCommand]
        private void ToggleFavoriteItem(StudentViewModel? student)
        {
            if (student is null) return;

            try
            {
                student.IsFavorite = !student.IsFavorite;

                if (student.IsFavorite)
                    _favoriteRepo.AddFavorite(student.StudentNumber);
                else
                    _favoriteRepo.RemoveFavorite(student.StudentNumber);
            }
            catch (Exception ex)
            {
                student.IsFavorite = !student.IsFavorite;
                ToastService.ShowError($"Favori işlemi başarısız: {ex.Message}");
            }
        }

        [RelayCommand]
        private void DownloadImage(StudentViewModel? student)
        {
            if (student is null || string.IsNullOrEmpty(student.PhotoPath) || student.PhotoPath.StartsWith("pack://"))
            {
                ToastService.ShowError("İndirilecek geçerli bir fotoğraf bulunamadı.");
                return;
            }

            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"{student.StudentNumber}-{student.FirstName}-{student.LastName}",
                    DefaultExt = ".png",
                    Filter = "Image Files|*.png;*.jpg;*.jpeg|All Files|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.Copy(student.PhotoPath, dialog.FileName, overwrite: true);
                    ToastService.ShowSuccess("Fotoğraf başarıyla kaydedildi.");
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Fotoğraf kaydedilirken hata oluştu: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ExportStudentPdf(StudentViewModel? student)
        {
            if (student is null) return;

            var kunyePath = student.KunyePdfPath;

            if (string.IsNullOrWhiteSpace(kunyePath) || !File.Exists(kunyePath))
            {
                ToastService.ShowError("Bu öğrenciye ait künye PDF dosyası bulunamadı.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(kunyePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"PDF açma hatası: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ExportTeamExcel()
        {
            try
            {
                var members = _teamRepo.GetMembers(_teamId);
                if (members.Count == 0)
                {
                    ToastService.ShowError("Takımda dışa aktarılacak oyuncu yok.");
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"{TeamName}",
                    DefaultExt = ".xlsx",
                    Filter = "Excel Dosyası|*.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    var guardianPhones = _teamRepo.GetGuardianPhones(_teamId);
                    var excelService = new ExcelExportService();
                    excelService.ExportTeamToExcel(TeamName, members, guardianPhones, dialog.FileName);
                    ToastService.ShowSuccess("Excel dosyası başarıyla oluşturuldu.");
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Excel aktarma hatası: {ex.Message}");
            }
        }

        private void LoadMembers()
        {
            var favoriteNumbers = _favoriteRepo.GetAllFavoriteStudentNumbers()
                .ToHashSet();

            var students = _teamRepo.GetMembers(_teamId);
            Members = new ObservableCollection<StudentViewModel>(
                students.Select(s => new StudentViewModel(s, favoriteNumbers.Contains(s.StudentNumber)))
            );

            MemberCountText = $"{Members.Count} Oyuncu";
        }
    }
}
