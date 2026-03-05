using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OBS.DataAccess;
using OBS.Models;
using OBS.Services;
using OBS.ViewModels;

namespace OBS.Views.Components
{
    public partial class EditTeamComponent : UserControl
    {
        private TeamRepository? _teamRepo;
        private StudentRepository? _studentRepo;
        private FavoriteRepository? _favoriteRepo;
        private int _teamId;
        private string _originalName = string.Empty;
        private string _category = string.Empty;
        private string? _requiredGender;
        private CancellationTokenSource? _searchCts;
        private HashSet<string> _currentMemberNumbers = new();
        private bool _isFavoritesMode;

        public event EventHandler<bool>? EditClosed; // true if HasChanges
        public bool HasChanges { get; private set; }

        public EditTeamComponent()
        {
            InitializeComponent();
        }

        public void LoadTeam(TeamCardViewModel team)
        {
            _teamRepo = new TeamRepository();
            _studentRepo = new StudentRepository();
            _favoriteRepo = new FavoriteRepository();

            _teamId = team.Id;
            _originalName = team.TeamName;
            _category = team.Category;
            _requiredGender = ResolveRequiredGender(_category);

            TeamNameBox.Text = _originalName;
            HasChanges = false;
            _isFavoritesMode = false;
            SearchBox.Text = string.Empty;
            SearchResultsBorder.Visibility = Visibility.Collapsed;

            RefreshMembers();
        }

        /// <summary>
        /// Kategori string'inden cinsiyet filtresini çıkarır.
        /// "(Kadınlar)" → "Kız", "(Karma)" → null (herkes), suffix yok → "Erkek"
        /// </summary>
        private static string? ResolveRequiredGender(string category)
        {
            if (category.Contains("(Kadınlar)", StringComparison.OrdinalIgnoreCase))
                return "Kız";
            if (category.Contains("(Karma)", StringComparison.OrdinalIgnoreCase))
                return null; // Karma: cinsiyet kısıtlaması yok
            return "Erkek"; // Varsayılan: erkek takımı
        }

        private void OnSaveNameClick(object sender, RoutedEventArgs e)
        {
            var newName = TeamNameBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                ToastService.ShowError("Takım adı boş bırakılamaz.");
                return;
            }

            if (_teamRepo == null) return;

            if (newName != _originalName && _teamRepo.Exists(newName))
            {
                ToastService.ShowError("Bu isimde bir takım zaten mevcut.");
                return;
            }

            _teamRepo.UpdateName(_teamId, newName);
            HasChanges = true;
            ToastService.ShowSuccess("Takım adı güncellendi.");
            _originalName = newName; // Update original name to prevent exist check on subsequent saves
        }

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            _isFavoritesMode = false;

            var keyword = SearchBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2)
            {
                SearchResultsBorder.Visibility = Visibility.Collapsed;
                SearchResultsList.ItemsSource = null;
                return;
            }

            if (_studentRepo == null) return;

            try
            {
                await Task.Delay(300, token);

                var results = FilterSearchResults(_studentRepo.Search(keyword));

                if (token.IsCancellationRequested) return;

                SearchResultsList.ItemsSource = results;
                SearchResultsBorder.Visibility = results.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            catch (OperationCanceledException) { }
        }

        private void OnSearchResultDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SearchResultsList.SelectedItem is Student student)
            {
                AddStudentToTeam(student.StudentNumber);
            }
        }

        private void OnShowFavoritesClick(object sender, RoutedEventArgs e)
        {
            if (_favoriteRepo == null || _studentRepo == null) return;

            var favoriteNumbers = _favoriteRepo.GetAllFavoriteStudentNumbers();
            if (favoriteNumbers.Count == 0)
            {
                ToastService.ShowError("Favori listeniz boş.");
                return;
            }

            var allStudents = _studentRepo.GetAll().ToDictionary(s => s.StudentNumber);
            var favoriteStudents = favoriteNumbers
                .Where(sn => allStudents.ContainsKey(sn))
                .Select(sn => allStudents[sn]);

            var results = FilterSearchResults(favoriteStudents);

            SearchBox.Text = string.Empty;
            _isFavoritesMode = true;
            SearchResultsList.ItemsSource = results;
            SearchResultsBorder.Visibility = results.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (results.Count == 0)
            {
                _isFavoritesMode = false;
                ToastService.ShowError("Eklenebilecek favori öğrenci bulunamadı.");
            }
        }

        private void OnAddStudentClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string studentNumber)
            {
                AddStudentToTeam(studentNumber);
            }
        }

        private void OnRemoveMemberClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string studentNumber)
            {
                if (_teamRepo == null) return;

                _teamRepo.RemoveMember(_teamId, studentNumber);
                HasChanges = true;
                RefreshMembers();
                RefreshSearchResults();
                ToastService.ShowSuccess("Öğrenci takımdan çıkarıldı.");
            }
        }

        private void AddStudentToTeam(string studentNumber)
        {
            if (_teamRepo == null) return;

            try
            {
                // Kural 4: Başka takımda mı kontrol et
                var existingTeam = _teamRepo.GetTeamNameForStudent(studentNumber);
                if (existingTeam != null)
                {
                    ToastService.ShowError($"Bu öğrenci zaten \"{existingTeam}\" takımında.");
                    return;
                }

                _teamRepo.AddMember(_teamId, studentNumber);
                HasChanges = true;
                RefreshMembers();
                RefreshSearchResults();
                ToastService.ShowSuccess("Öğrenci takıma eklendi.");
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Ekleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Arama sonuçlarını takım kurallarına göre filtreler:
        /// - Zaten bu takımda olanları hariç tutar
        /// - Cinsiyet kuralına uymayan öğrencileri hariç tutar (Kural 1, 2, 3)
        /// - Başka takımda olan öğrencileri hariç tutar (Kural 4)
        /// </summary>
        private List<Student> FilterSearchResults(IEnumerable<Student> rawResults)
        {
            if (_teamRepo == null) return new List<Student>();

            return rawResults
                .Where(s => !_currentMemberNumbers.Contains(s.StudentNumber))
                .Where(s => _requiredGender == null || string.Equals(s.Gender, _requiredGender, StringComparison.OrdinalIgnoreCase))
                .Where(s => _teamRepo.GetTeamNameForStudent(s.StudentNumber) == null)
                .Take(10)
                .ToList();
        }

        private void RefreshMembers()
        {
            if (_teamRepo == null) return;

            var members = _teamRepo.GetMembers(_teamId);
            _currentMemberNumbers = new HashSet<string>(members.Select(m => m.StudentNumber));
            MembersList.ItemsSource = members;
            MembersHeader.Text = $"Mevcut Üyeler ({members.Count})";
        }

        private void RefreshSearchResults()
        {
            if (_isFavoritesMode)
            {
                RefreshFavoriteResults();
                return;
            }

            var keyword = SearchBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2)
            {
                SearchResultsBorder.Visibility = Visibility.Collapsed;
                SearchResultsList.ItemsSource = null;
                return;
            }

            if (_studentRepo == null) return;

            var results = FilterSearchResults(_studentRepo.Search(keyword));

            SearchResultsList.ItemsSource = results;
            SearchResultsBorder.Visibility = results.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void RefreshFavoriteResults()
        {
            if (_favoriteRepo == null || _studentRepo == null) return;

            var favoriteNumbers = _favoriteRepo.GetAllFavoriteStudentNumbers();
            var allStudents = _studentRepo.GetAll().ToDictionary(s => s.StudentNumber);
            var favoriteStudents = favoriteNumbers
                .Where(sn => allStudents.ContainsKey(sn))
                .Select(sn => allStudents[sn]);

            var results = FilterSearchResults(favoriteStudents);

            SearchResultsList.ItemsSource = results;
            SearchResultsBorder.Visibility = results.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (results.Count == 0)
            {
                _isFavoritesMode = false;
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            EditClosed?.Invoke(this, HasChanges);
        }
    }
}
