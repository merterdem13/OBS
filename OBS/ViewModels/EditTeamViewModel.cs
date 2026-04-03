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
    public sealed class EditTeamToastRequestedEventArgs : EventArgs
    {
        public EditTeamToastRequestedEventArgs(string message, bool isError)
        {
            Message = message;
            IsError = isError;
        }

        public string Message { get; }

        public bool IsError { get; }
    }

    public partial class EditTeamViewModel : ObservableObject
    {
        private readonly Func<string, bool> _teamExists;
        private readonly Action<int, string> _updateName;
        private readonly Func<int, IEnumerable<Student>> _getMembers;
        private readonly Action<int, string> _removeMember;
        private readonly Action<int, string> _addMember;
        private readonly Func<string, string?> _getTeamNameForStudent;
        private readonly Func<string, IEnumerable<Student>> _searchStudents;
        private readonly Func<IEnumerable<Student>> _getAllStudents;
        private readonly Func<IReadOnlyCollection<string>> _getFavoriteStudentNumbers;
        private readonly Func<CancellationToken, Task> _searchDelayAsync;
        private CancellationTokenSource? _searchCts;

        [ObservableProperty]
        private int teamId;

        [ObservableProperty]
        private string originalName = string.Empty;

        [ObservableProperty]
        private string teamName = string.Empty;

        [ObservableProperty]
        private string category = string.Empty;

        [ObservableProperty]
        private string? requiredGender;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool hasChanges;

        [ObservableProperty]
        private bool isFavoritesMode;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Student> members = [];

        [ObservableProperty]
        private ObservableCollection<Student> searchResults = [];

        public EditTeamViewModel()
            : this(new TeamRepository(), new StudentRepository(), new FavoriteRepository())
        {
        }

        public EditTeamViewModel(
            TeamRepository teamRepo,
            StudentRepository studentRepo,
            FavoriteRepository favoriteRepo)
            : this(
                teamRepo.Exists,
                teamRepo.UpdateName,
                teamRepo.GetMembers,
                teamRepo.RemoveMember,
                teamRepo.AddMember,
                teamRepo.GetTeamNameForStudent,
                studentRepo.Search,
                studentRepo.GetAll,
                favoriteRepo.GetAllFavoriteStudentNumbers,
                cancellationToken => Task.Delay(300, cancellationToken))
        {
        }

        public EditTeamViewModel(
            Func<string, bool> teamExists,
            Action<int, string> updateName,
            Func<int, IEnumerable<Student>> getMembers,
            Action<int, string> removeMember,
            Action<int, string> addMember,
            Func<string, string?> getTeamNameForStudent,
            Func<string, IEnumerable<Student>> searchStudents,
            Func<IEnumerable<Student>> getAllStudents,
            Func<IReadOnlyCollection<string>> getFavoriteStudentNumbers,
            Func<CancellationToken, Task>? searchDelayAsync = null)
        {
            _teamExists = teamExists;
            _updateName = updateName;
            _getMembers = getMembers;
            _removeMember = removeMember;
            _addMember = addMember;
            _getTeamNameForStudent = getTeamNameForStudent;
            _searchStudents = searchStudents;
            _getAllStudents = getAllStudents;
            _getFavoriteStudentNumbers = getFavoriteStudentNumbers;
            _searchDelayAsync = searchDelayAsync ?? (cancellationToken => Task.Delay(300, cancellationToken));

            AttachMembersCollection(Members);
            AttachSearchResultsCollection(SearchResults);
        }

        public event EventHandler<EditTeamToastRequestedEventArgs>? ToastRequested;

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public bool HasSearchResults => SearchResults.Count > 0;

        public string MembersHeaderText => $"Mevcut Üyeler ({Members.Count})";

        partial void OnSearchTextChanged(string value)
        {
            _ = RefreshSearchResultsSafelyAsync();
        }

        partial void OnMembersChanging(ObservableCollection<Student>? oldValue, ObservableCollection<Student> newValue)
        {
            DetachMembersCollection(oldValue);
        }

        partial void OnMembersChanged(ObservableCollection<Student> value)
        {
            AttachMembersCollection(value);
            OnPropertyChanged(nameof(MembersHeaderText));
        }

        partial void OnSearchResultsChanging(ObservableCollection<Student>? oldValue, ObservableCollection<Student> newValue)
        {
            DetachSearchResultsCollection(oldValue);
        }

        partial void OnSearchResultsChanged(ObservableCollection<Student> value)
        {
            AttachSearchResultsCollection(value);
            OnPropertyChanged(nameof(HasSearchResults));
        }

        partial void OnErrorMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        public void LoadTeam(TeamCardViewModel team)
        {
            TeamId = team.Id;
            OriginalName = team.TeamName;
            TeamName = team.TeamName;
            Category = team.Category;
            RequiredGender = ResolveRequiredGender(team.Category);
            HasChanges = false;
            IsFavoritesMode = false;
            ErrorMessage = string.Empty;
            SearchText = string.Empty;

            ReplaceMembers(_getMembers(TeamId));
            ClearSearchResults();
        }

        public static string? ResolveRequiredGender(string category)
        {
            if (category.Contains("(Kadınlar)", StringComparison.OrdinalIgnoreCase))
            {
                return "Kız";
            }

            if (category.Contains("(Karma)", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return "Erkek";
        }

        [RelayCommand]
        public Task SaveNameAsync()
        {
            var trimmedName = TeamName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                ErrorMessage = "Takım adı boş bırakılamaz.";
                RequestToast(ErrorMessage, true);
                return Task.CompletedTask;
            }

            if (!string.Equals(trimmedName, OriginalName, StringComparison.Ordinal) && _teamExists(trimmedName))
            {
                ErrorMessage = "Bu isimde bir takım zaten mevcut.";
                RequestToast(ErrorMessage, true);
                return Task.CompletedTask;
            }

            _updateName(TeamId, trimmedName);
            TeamName = trimmedName;
            OriginalName = trimmedName;
            HasChanges = true;
            ErrorMessage = string.Empty;
            RequestToast("Takım adı güncellendi.", false);
            return Task.CompletedTask;
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

            try
            {
                var existingTeam = _getTeamNameForStudent(student.StudentNumber);
                if (existingTeam != null)
                {
                    RequestToast($"Bu öğrenci zaten \"{existingTeam}\" takımında.", true);
                    return;
                }

                _addMember(TeamId, student.StudentNumber);
                HasChanges = true;
                ReplaceMembers(_getMembers(TeamId));
                RefreshSearchResults();
                RequestToast("Öğrenci takıma eklendi.", false);
            }
            catch (Exception ex)
            {
                RequestToast($"Ekleme hatası: {ex.Message}", true);
            }
        }

        [RelayCommand]
        public void RemoveMember(Student? student)
        {
            if (student == null)
            {
                return;
            }

            _removeMember(TeamId, student.StudentNumber);
            HasChanges = true;
            ReplaceMembers(_getMembers(TeamId));
            RefreshSearchResults();
            RequestToast("Öğrenci takımdan çıkarıldı.", false);
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
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                RequestToast(ex.Message, true);
            }
        }

        private Task RefreshSearchResultsSafelyAsync() => RefreshSearchResultsAsync();

        public void RefreshSearchResults()
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

        private List<Student> FilterSearchResults(IEnumerable<Student> rawResults)
        {
            var memberNumbers = Members.Select(member => member.StudentNumber).ToHashSet(StringComparer.Ordinal);

            return rawResults
                .Where(student => !memberNumbers.Contains(student.StudentNumber))
                .Where(student => RequiredGender == null || string.Equals(student.Gender, RequiredGender, StringComparison.OrdinalIgnoreCase))
                .Where(student => _getTeamNameForStudent(student.StudentNumber) == null)
                .Take(10)
                .ToList();
        }

        private void ReplaceMembers(IEnumerable<Student> members)
        {
            Members.Clear();
            foreach (var member in members)
            {
                Members.Add(member);
            }

            OnPropertyChanged(nameof(MembersHeaderText));
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
            ToastRequested?.Invoke(this, new EditTeamToastRequestedEventArgs(message, isError));
        }

        private void CancelSearch()
        {
            _searchCts?.Cancel();
            _searchCts = null;
        }

        private void AttachMembersCollection(ObservableCollection<Student>? collection)
        {
            if (collection != null)
            {
                collection.CollectionChanged += OnMembersCollectionChanged;
            }
        }

        private void DetachMembersCollection(ObservableCollection<Student>? collection)
        {
            if (collection != null)
            {
                collection.CollectionChanged -= OnMembersCollectionChanged;
            }
        }

        private void AttachSearchResultsCollection(ObservableCollection<Student>? collection)
        {
            if (collection != null)
            {
                collection.CollectionChanged += OnSearchResultsCollectionChanged;
            }
        }

        private void DetachSearchResultsCollection(ObservableCollection<Student>? collection)
        {
            if (collection != null)
            {
                collection.CollectionChanged -= OnSearchResultsCollectionChanged;
            }
        }

        private void OnMembersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(MembersHeaderText));
        }

        private void OnSearchResultsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasSearchResults));
        }
    }
}
