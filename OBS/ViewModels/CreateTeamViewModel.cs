using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OBS.DataAccess;
using OBS.Models;

namespace OBS.ViewModels
{
    public sealed class CreateTeamToastRequestedEventArgs : EventArgs
    {
        public CreateTeamToastRequestedEventArgs(string message, bool isError)
        {
            Message = message;
            IsError = isError;
        }

        public string Message { get; }

        public bool IsError { get; }
    }

    public partial class CreateTeamViewModel : ObservableObject
    {
        private readonly Func<string, bool> _teamExists;
        private readonly Action<Team> _insertTeam;
        private readonly Action<int, string> _addMember;
        private readonly Func<string, string?> _getTeamNameForStudent;
        private readonly Func<string, IEnumerable<Student>> _searchStudents;
        private readonly Func<IEnumerable<Student>> _getAllStudents;
        private readonly Func<string, Student?> _getStudentByNumber;
        private readonly Func<IReadOnlyCollection<string>> _getFavoriteStudentNumbers;
        private readonly Func<CancellationToken, Task> _searchDelayAsync;
        private CancellationTokenSource? _searchCts;
        private bool _isResettingForm;

        [ObservableProperty]
        private string teamName = string.Empty;

        [ObservableProperty]
        private string selectedCategory = "Belirtilmemiş";

        [ObservableProperty]
        private string selectedGenderMode = "Erkek";

        [ObservableProperty]
        private string? requiredGender = "Erkek";

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool isFavoritesMode;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Student> pendingMembers = [];

        [ObservableProperty]
        private ObservableCollection<Student> searchResults = [];

        public CreateTeamViewModel()
            : this(new TeamRepository(), new StudentRepository(), new FavoriteRepository())
        {
        }

        public CreateTeamViewModel(
            TeamRepository teamRepo,
            StudentRepository studentRepo,
            FavoriteRepository favoriteRepo)
            : this(
                teamRepo.Exists,
                team => teamRepo.Insert(team),
                teamRepo.AddMember,
                teamRepo.GetTeamNameForStudent,
                studentRepo.Search,
                studentRepo.GetAll,
                studentRepo.GetByStudentNumber,
                favoriteRepo.GetAllFavoriteStudentNumbers,
                cancellationToken => Task.Delay(300, cancellationToken))
        {
        }

        public CreateTeamViewModel(
            Func<string, bool> teamExists,
            Action<Team> insertTeam,
            Action<int, string> addMember,
            Func<string, string?> getTeamNameForStudent,
            Func<string, IEnumerable<Student>> searchStudents,
            Func<IEnumerable<Student>> getAllStudents,
            Func<string, Student?> getStudentByNumber,
            Func<IReadOnlyCollection<string>> getFavoriteStudentNumbers,
            Func<CancellationToken, Task>? searchDelayAsync = null)
        {
            _teamExists = teamExists;
            _insertTeam = insertTeam;
            _addMember = addMember;
            _getTeamNameForStudent = getTeamNameForStudent;
            _searchStudents = searchStudents;
            _getAllStudents = getAllStudents;
            _getStudentByNumber = getStudentByNumber;
            _getFavoriteStudentNumbers = getFavoriteStudentNumbers;
            _searchDelayAsync = searchDelayAsync ?? (cancellationToken => Task.Delay(300, cancellationToken));

            PendingMembers.CollectionChanged += OnPendingMembersCollectionChanged;
            SearchResults.CollectionChanged += OnSearchResultsCollectionChanged;
        }

        public event EventHandler<bool>? CloseRequested;

        public event EventHandler<CreateTeamToastRequestedEventArgs>? ToastRequested;

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public bool HasSearchResults => SearchResults.Count > 0;

        public string MembersHeaderText => $"Eklenecek Üyeler ({PendingMembers.Count})";

        partial void OnSelectedCategoryChanged(string value)
        {
            UpdateRequiredGender();
        }

        partial void OnSelectedGenderModeChanged(string value)
        {
            UpdateRequiredGender();
        }

        partial void OnSearchTextChanged(string value)
        {
            _ = RefreshSearchResultsAsync();
        }

        partial void OnErrorMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        public void UpdateRequiredGender()
        {
            RequiredGender = SelectedGenderMode switch
            {
                "Kız" => "Kız",
                "Karma" => null,
                _ when SelectedCategory.Contains("(Kadınlar)", StringComparison.OrdinalIgnoreCase) => "Kız",
                _ when SelectedCategory.Contains("(Karma)", StringComparison.OrdinalIgnoreCase) => null,
                _ => "Erkek"
            };

            if (!_isResettingForm && RequiredGender != null)
            {
                var invalidMembers = PendingMembers
                    .Where(student => !string.Equals(student.Gender, RequiredGender, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var member in invalidMembers)
                {
                    PendingMembers.Remove(member);
                    RequestToast($"{member.FirstName} {member.LastName} cinsiyet kuralına uymadığı için listeden çıkarıldı.", true);
                }
            }

            RefreshSearchResults();
        }

        [RelayCommand]
        public async Task CreateAsync()
        {
            var trimmedTeamName = TeamName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedTeamName))
            {
                ErrorMessage = "Takım adı boş bırakılamaz.";
                return;
            }

            if (_teamExists(trimmedTeamName))
            {
                ErrorMessage = "Bu isimde bir takım zaten mevcut.";
                return;
            }

            ErrorMessage = string.Empty;

            var team = new Team
            {
                TeamName = trimmedTeamName,
                Category = BuildCategory()
            };

            _insertTeam(team);

            foreach (var student in PendingMembers.ToList())
            {
                try
                {
                    _addMember(team.Id, student.StudentNumber);
                }
                catch (Exception ex)
                {
                    RequestToast($"'{student.FirstName}' eklenirken hata: {ex.Message}", true);
                }
            }

            RequestToast($"\"{trimmedTeamName}\" takımı başarıyla oluşturuldu.", false);
            ClearForm();
            CloseRequested?.Invoke(this, true);

            await Task.CompletedTask;
        }

        [RelayCommand]
        public void Cancel()
        {
            ClearForm();
            CloseRequested?.Invoke(this, false);
        }

        [RelayCommand]
        public async Task ToggleFavoritesAsync()
        {
            CancelSearch();

            if (IsFavoritesMode)
            {
                IsFavoritesMode = false;
                SearchText = string.Empty;
                ClearSearchResults();
                return;
            }

            var favoriteNumbers = _getFavoriteStudentNumbers();
            if (favoriteNumbers.Count == 0)
            {
                RequestToast("Favori listeniz boş.", true);
                return;
            }

            var allStudents = _getAllStudents().ToDictionary(student => student.StudentNumber);
            var results = FilterSearchResults(
                favoriteNumbers
                    .Where(studentNumber => allStudents.ContainsKey(studentNumber))
                    .Select(studentNumber => allStudents[studentNumber]));

            SearchText = string.Empty;

            if (results.Count == 0)
            {
                IsFavoritesMode = false;
                ClearSearchResults();
                RequestToast("Eklenebilecek favori öğrenci bulunamadı.", true);
                return;
            }

            IsFavoritesMode = true;
            SetSearchResults(results);

            await Task.CompletedTask;
        }

        [RelayCommand]
        public void AddStudent(Student? student)
        {
            if (student == null)
            {
                return;
            }

            var resolvedStudent = _getStudentByNumber(student.StudentNumber) ?? student;
            var existingTeam = _getTeamNameForStudent(resolvedStudent.StudentNumber);
            if (existingTeam != null)
            {
                RequestToast($"Bu öğrenci zaten \"{existingTeam}\" takımında.", true);
                return;
            }

            if (PendingMembers.Any(member => member.StudentNumber == resolvedStudent.StudentNumber))
            {
                RequestToast("Öğrenci zaten listeye eklendi.", true);
                return;
            }

            if (RequiredGender != null && !string.Equals(resolvedStudent.Gender, RequiredGender, StringComparison.OrdinalIgnoreCase))
            {
                RequestToast("Öğrenci mevcut cinsiyet kuralına uymuyor.", true);
                return;
            }

            PendingMembers.Add(resolvedStudent);
            ErrorMessage = string.Empty;
            RefreshSearchResults();
        }

        [RelayCommand]
        public void RemovePendingMember(Student? student)
        {
            if (student == null)
            {
                return;
            }

            var studentToRemove = PendingMembers.FirstOrDefault(member => member.StudentNumber == student.StudentNumber);
            if (studentToRemove == null)
            {
                return;
            }

            PendingMembers.Remove(studentToRemove);
            RefreshSearchResults();
        }

        public async Task RefreshSearchResultsAsync()
        {
            CancelSearch();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            var keyword = SearchText?.Trim() ?? string.Empty;
            if (IsFavoritesMode && !string.IsNullOrWhiteSpace(keyword))
            {
                IsFavoritesMode = false;
            }

            if (IsFavoritesMode)
            {
                RefreshSearchResults();
                return;
            }

            var isNumeric = keyword.All(char.IsDigit);
            var minLength = isNumeric && keyword.Length > 0 ? 1 : 2;

            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < minLength)
            {
                ClearSearchResults();
                return;
            }

            try
            {
                await _searchDelayAsync(token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                SetSearchResults(FilterSearchResults(_searchStudents(keyword)));
            }
            catch (OperationCanceledException)
            {
            }
        }

        private string BuildCategory()
        {
            return SelectedGenderMode switch
            {
                "Kız" => $"{SelectedCategory} (Kadınlar)",
                "Karma" => $"{SelectedCategory} (Karma)",
                _ => SelectedCategory
            };
        }

        private List<Student> FilterSearchResults(IEnumerable<Student> rawResults)
        {
            return rawResults
                .Where(student => PendingMembers.All(member => member.StudentNumber != student.StudentNumber))
                .Where(student => RequiredGender == null || string.Equals(student.Gender, RequiredGender, StringComparison.OrdinalIgnoreCase))
                .Where(student => _getTeamNameForStudent(student.StudentNumber) == null)
                .Take(10)
                .ToList();
        }

        private void RefreshSearchResults()
        {
            if (IsFavoritesMode)
            {
                var allStudents = _getAllStudents().ToDictionary(student => student.StudentNumber);
                var results = FilterSearchResults(
                    _getFavoriteStudentNumbers()
                        .Where(studentNumber => allStudents.ContainsKey(studentNumber))
                        .Select(studentNumber => allStudents[studentNumber]));

                if (results.Count == 0)
                {
                    IsFavoritesMode = false;
                    ClearSearchResults();
                    return;
                }

                SetSearchResults(results);
                return;
            }

            var keyword = SearchText?.Trim() ?? string.Empty;
            var isNumeric = keyword.All(char.IsDigit);
            var minLength = isNumeric && keyword.Length > 0 ? 1 : 2;

            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < minLength)
            {
                ClearSearchResults();
                return;
            }

            SetSearchResults(FilterSearchResults(_searchStudents(keyword)));
        }

        private void ClearForm()
        {
            CancelSearch();
            _isResettingForm = true;

            try
            {
                PendingMembers.Clear();
                ClearSearchResults();
                IsFavoritesMode = false;
                SearchText = string.Empty;
                TeamName = string.Empty;
                ErrorMessage = string.Empty;
                SelectedCategory = "Belirtilmemiş";
                SelectedGenderMode = "Erkek";
                RequiredGender = "Erkek";
            }
            finally
            {
                _isResettingForm = false;
            }
        }

        private void ClearSearchResults()
        {
            SearchResults.Clear();
            OnPropertyChanged(nameof(HasSearchResults));
        }

        private void SetSearchResults(IEnumerable<Student> results)
        {
            SearchResults.Clear();
            foreach (var result in results)
            {
                SearchResults.Add(result);
            }

            OnPropertyChanged(nameof(HasSearchResults));
        }

        private void RequestToast(string message, bool isError)
        {
            ToastRequested?.Invoke(this, new CreateTeamToastRequestedEventArgs(message, isError));
        }

        private void CancelSearch()
        {
            _searchCts?.Cancel();
            _searchCts = null;
        }

        private void OnPendingMembersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(MembersHeaderText));
        }

        private void OnSearchResultsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasSearchResults));
        }
    }
}
