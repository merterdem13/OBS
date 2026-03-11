using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OBS.DataAccess;
using OBS.Models;
using OBS.Services;

namespace OBS.Views.Components
{
    public partial class CreateTeamComponent : UserControl
    {
        private readonly TeamRepository _teamRepo;
        private readonly StudentRepository _studentRepo;
        private readonly FavoriteRepository _favoriteRepo;
        private CancellationTokenSource? _searchCts;
        private ObservableCollection<Student> _pendingMembers = new();
        private bool _isFavoritesMode;
        private string? _requiredGender = "Erkek"; // Default based on UI

        public event EventHandler<bool>? CreateClosed;

        public CreateTeamComponent()
        {
            InitializeComponent();
            _teamRepo = new TeamRepository();
            _studentRepo = new StudentRepository();
            _favoriteRepo = new FavoriteRepository();
            MembersList.ItemsSource = _pendingMembers;
            UpdateMembersHeader();
        }

        private void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            var teamName = TeamNameBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(teamName))
            {
                ShowError("Takım adı boş bırakılamaz.");
                return;
            }

            if (_teamRepo.Exists(teamName))
            {
                ShowError("Bu isimde bir takım zaten mevcut.");
                return;
            }

            var selectedCategory = (CategoryCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Belirtilmemiş";

            if (RadioKiz.IsChecked == true)
            {
                selectedCategory += " (Kadınlar)";
            }
            else if (RadioBelirtilmemis.IsChecked == true)
            {
                selectedCategory += " (Karma)";
            }

            var team = new Team
            {
                TeamName = teamName,
                Category = selectedCategory
            };

            _teamRepo.Insert(team);
            var newTeamId = team.Id;

            // Öğrencileri takıma ekle
            foreach (var student in _pendingMembers)
            {
                try
                {
                    _teamRepo.AddMember(newTeamId, student.StudentNumber);
                }
                catch (Exception ex)
                {
                    ToastService.ShowError($"'{student.FirstName}' eklenirken hata: {ex.Message}");
                }
            }

            ToastService.ShowSuccess($"\"{teamName}\" takımı başarıyla oluşturuldu.");

            ClearForm();
            CreateClosed?.Invoke(this, true);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            ClearForm();
            CreateClosed?.Invoke(this, false);
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void ClearForm()
        {
            TeamNameBox.Text = string.Empty;
            ErrorText.Visibility = Visibility.Collapsed;
            CategoryCombo.SelectedIndex = 0;
            RadioErkek.IsChecked = true;
            _isFavoritesMode = false;
            SearchBox.Text = string.Empty;
            SearchResultsBorder.Visibility = Visibility.Collapsed;
            _pendingMembers.Clear();
            UpdateMembersHeader();
            UpdateFavoriteIcon();
        }

        private void OnCategoryChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            UpdateRequiredGender();
        }

        private void OnGenderChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            UpdateRequiredGender();
        }

        private void UpdateRequiredGender()
        {
            if (CategoryCombo == null || RadioKiz == null) return;
            
            var category = (CategoryCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            
            if (RadioKiz.IsChecked == true || category.Contains("(Kadınlar)", StringComparison.OrdinalIgnoreCase))
                _requiredGender = "Kız";
            else if (RadioBelirtilmemis.IsChecked == true || category.Contains("(Karma)", StringComparison.OrdinalIgnoreCase))
                _requiredGender = null;
            else
                _requiredGender = "Erkek";
                
            // Validate pending members against new gender rule
            if (_requiredGender != null)
            {
                var invalidMembers = _pendingMembers.Where(m => !string.Equals(m.Gender, _requiredGender, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var member in invalidMembers)
                {
                    _pendingMembers.Remove(member);
                    ToastService.ShowError($"{member.FirstName} {member.LastName} cinsiyet kuralına uymadığı için listeden çıkarıldı.");
                }
                UpdateMembersHeader();
            }
            RefreshSearchResults();
        }

        private void UpdateFavoriteIcon()
        {
            if (FavoriteIcon != null)
            {
                FavoriteIcon.Filled = _isFavoritesMode;
                if (_isFavoritesMode)
                {
                    FavoriteIcon.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F6AD55"));
                }
                else
                {
                    FavoriteIcon.SetResourceReference(Control.ForegroundProperty, "TextSecondaryBrush");
                }
            }
        }

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            var keyword = SearchBox.Text?.Trim() ?? string.Empty;

            if (_isFavoritesMode && !string.IsNullOrWhiteSpace(keyword))
            {
                _isFavoritesMode = false;
                UpdateFavoriteIcon();
            }

            bool isNumeric = keyword.All(c => char.IsDigit(c));
            int minLength = isNumeric && keyword.Length > 0 ? 1 : 2;

            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < minLength)
            {
                if (!_isFavoritesMode)
                {
                    SearchResultsBorder.Visibility = Visibility.Collapsed;
                    SearchResultsList.ItemsSource = null;
                }
                return;
            }

            try
            {
                await Task.Delay(300, token);
                var results = FilterSearchResults(_studentRepo.Search(keyword));
                
                if (token.IsCancellationRequested) return;

                SearchResultsList.ItemsSource = results;
                SearchResultsBorder.Visibility = results.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (OperationCanceledException) { }
        }

        private void OnSearchResultDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SearchResultsList.SelectedItem is Student student)
            {
                AddStudentToPendingList(student);
            }
        }

        private void OnShowFavoritesClick(object sender, RoutedEventArgs e)
        {
            if (_isFavoritesMode)
            {
                _isFavoritesMode = false;
                UpdateFavoriteIcon();
                SearchBox.Text = string.Empty;
                return;
            }

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
            UpdateFavoriteIcon();

            SearchResultsList.ItemsSource = results;
            SearchResultsBorder.Visibility = results.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (results.Count == 0)
            {
                _isFavoritesMode = false;
                UpdateFavoriteIcon();
                ToastService.ShowError("Eklenebilecek favori öğrenci bulunamadı.");
            }
        }

        private void OnAddStudentClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string studentNumber)
            {
                var student = _studentRepo.GetByStudentNumber(studentNumber);
                if (student != null)
                {
                    AddStudentToPendingList(student);
                }
            }
        }

        private void OnRemoveMemberClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string studentNumber)
            {
                var studentToRemove = _pendingMembers.FirstOrDefault(s => s.StudentNumber == studentNumber);
                if (studentToRemove != null)
                {
                    _pendingMembers.Remove(studentToRemove);
                    UpdateMembersHeader();
                    RefreshSearchResults();
                }
            }
        }

        private void AddStudentToPendingList(Student student)
        {
            var existingTeam = _teamRepo.GetTeamNameForStudent(student.StudentNumber);
            if (existingTeam != null)
            {
                ToastService.ShowError($"Bu öğrenci zaten \"{existingTeam}\" takımında.");
                return;
            }

            if (_pendingMembers.Any(s => s.StudentNumber == student.StudentNumber))
            {
                ToastService.ShowError("Öğrenci zaten listeye eklendi.");
                return;
            }

            _pendingMembers.Add(student);
            UpdateMembersHeader();
            RefreshSearchResults();
        }

        private List<Student> FilterSearchResults(IEnumerable<Student> rawResults)
        {
            return rawResults
                .Where(s => !_pendingMembers.Any(pm => pm.StudentNumber == s.StudentNumber)) // Not already in pending
                .Where(s => _requiredGender == null || string.Equals(s.Gender, _requiredGender, StringComparison.OrdinalIgnoreCase)) // Matches gender
                .Where(s => _teamRepo.GetTeamNameForStudent(s.StudentNumber) == null) // Not in another team
                .Take(10)
                .ToList();
        }

        private void RefreshSearchResults()
        {
            if (!IsLoaded) return;

            if (_isFavoritesMode)
            {
                var favoriteNumbers = _favoriteRepo.GetAllFavoriteStudentNumbers();
                var allStudents = _studentRepo.GetAll().ToDictionary(s => s.StudentNumber);
                var favoriteStudents = favoriteNumbers
                    .Where(sn => allStudents.ContainsKey(sn))
                    .Select(sn => allStudents[sn]);

                var results = FilterSearchResults(favoriteStudents);
                if (SearchResultsList != null) SearchResultsList.ItemsSource = results;
                if (SearchResultsBorder != null) SearchResultsBorder.Visibility = results.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                return;
            }
            
            var keyword = SearchBox?.Text?.Trim() ?? string.Empty;
            bool isNumeric = keyword.All(c => char.IsDigit(c));
            int minLength = isNumeric && keyword.Length > 0 ? 1 : 2;

            if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length >= minLength)
            {
                var results = FilterSearchResults(_studentRepo.Search(keyword));
                if (SearchResultsList != null) SearchResultsList.ItemsSource = results;
                if (SearchResultsBorder != null) SearchResultsBorder.Visibility = results.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                if (SearchResultsBorder != null) SearchResultsBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateMembersHeader()
        {
            if (MembersHeader != null)
            {
                MembersHeader.Text = $"Eklenecek Üyeler ({_pendingMembers.Count})";
            }
        }
    }
}
