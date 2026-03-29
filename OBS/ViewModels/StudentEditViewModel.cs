using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OBS.DataAccess;
using OBS.Models;

namespace OBS.ViewModels
{
    /// <summary>
    /// Öğrenci Düzenleme Modu için ViewModel.
    /// MainViewModel'deki arama/filtre/favori/sayfalama mantığının kopyasıdır.
    /// Settings overlay'dan bağımsız, full-screen overlay olarak çalışır.
    /// </summary>
    public partial class StudentEditViewModel : ObservableObject
    {
        private readonly StudentRepository _studentRepo;
        private readonly FavoriteRepository _favoriteRepo;

        // ── Arama & Filtre ──────────────────────────────────────────────────
        [ObservableProperty]
        private string _editSearchText = string.Empty;

        [ObservableProperty]
        private string? _editSelectedClass;

        [ObservableProperty]
        private ObservableCollection<string> _editClassList = new();

        [ObservableProperty]
        private bool _isEditFavoriteMode = false;

        [ObservableProperty]
        private bool _isEditClassFilterMode = false;

        [ObservableProperty]
        private bool _isEditClassSelected = false;

        [ObservableProperty]
        private bool _hasEditFavorites = false;

        // ── Öğrenci Listesi ─────────────────────────────────────────────────
        [ObservableProperty]
        private ObservableCollection<StudentViewModel> _editStudents = new();

        [ObservableProperty]
        private bool _hasMoreEditStudents = false;

        // ── Sayfalama ─────────────────────────────────────────────────────
        private const int PageSize = 6;
        private List<StudentViewModel> _allEditViewModels = new();
        private int _editLoadedCount;
        private CancellationTokenSource? _editSearchDebounceCts;
        private CancellationTokenSource? _editStaggerCts;
        private bool _isUpdatingEditFilters;

        // ── Constructor ─────────────────────────────────────────────────────
        public StudentEditViewModel()
        {
            _studentRepo = new StudentRepository();
            _favoriteRepo = new FavoriteRepository();

            LoadEditClassList();
            UpdateEditFavoriteState();
        }

        // ── Partial Callbacks ───────────────────────────────────────────────

        partial void OnEditSelectedClassChanged(string? value)
        {
            if (_isUpdatingEditFilters) return;
            _isUpdatingEditFilters = true;
            try
            {
                IsEditClassSelected = !string.IsNullOrEmpty(value);

                if (_isEditFavoriteMode && !string.IsNullOrEmpty(value))
                {
                    _isEditFavoriteMode = false;
                    OnPropertyChanged(nameof(IsEditFavoriteMode));
                    EditStudents.Clear();
                }

                if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(_editSearchText))
                {
                    _editSearchDebounceCts?.Cancel();
                    _editSearchText = string.Empty;
                    OnPropertyChanged(nameof(EditSearchText));
                }

                RefreshEditStudents();
            }
            finally
            {
                _isUpdatingEditFilters = false;
            }
        }

        partial void OnEditSearchTextChanged(string value)
        {
            if (_isUpdatingEditFilters) return;
            _isUpdatingEditFilters = true;
            try
            {
                if (_isEditFavoriteMode && !string.IsNullOrWhiteSpace(value))
                {
                    _isEditFavoriteMode = false;
                    OnPropertyChanged(nameof(IsEditFavoriteMode));
                    EditStudents.Clear();
                }

                if (!string.IsNullOrWhiteSpace(value) && _editSelectedClass != null)
                {
                    _editSelectedClass = null;
                    IsEditClassSelected = false;
                    OnPropertyChanged(nameof(EditSelectedClass));
                }

                var trimmed = value?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    _editSearchDebounceCts?.Cancel();

                    if (_editSelectedClass != null)
                        return;

                    RefreshEditStudents();
                    return;
                }

                bool isNumeric = trimmed.All(char.IsDigit);
                int minLength = isNumeric ? 1 : 2;

                if (trimmed.Length < minLength)
                {
                    _editSearchDebounceCts?.Cancel();
                    return;
                }

                DebouncedRefreshEditStudents();
            }
            finally
            {
                _isUpdatingEditFilters = false;
            }
        }

        partial void OnIsEditFavoriteModeChanged(bool value)
        {
            if (_isUpdatingEditFilters) return;
            _isUpdatingEditFilters = true;
            try
            {
                if (value)
                {
                    if (_editSelectedClass != null)
                    {
                        _editSelectedClass = null;
                        IsEditClassSelected = false;
                        OnPropertyChanged(nameof(EditSelectedClass));
                    }
                    if (!string.IsNullOrEmpty(_editSearchText))
                    {
                        _editSearchDebounceCts?.Cancel();
                        _editSearchText = string.Empty;
                        OnPropertyChanged(nameof(EditSearchText));
                    }
                }

                RefreshEditStudents();
            }
            finally
            {
                _isUpdatingEditFilters = false;
            }
        }

        // ── Komutlar ────────────────────────────────────────────────────────

        [RelayCommand]
        private void ToggleEditFavorite()
        {
            IsEditFavoriteMode = !IsEditFavoriteMode;
        }

        [RelayCommand]
        private void ToggleEditFilterMode()
        {
            IsEditClassFilterMode = !IsEditClassFilterMode;

            if (!IsEditClassFilterMode && EditSelectedClass != null)
            {
                EditSelectedClass = null;
            }

            if (IsEditClassFilterMode && !string.IsNullOrEmpty(EditSearchText))
            {
                EditSearchText = string.Empty;
            }
        }

        [RelayCommand]
        private void ClearEditClassFilter() => EditSelectedClass = null;

        [RelayCommand]
        private void CloseEditFavoritesPanel() => IsEditFavoriteMode = false;

        [RelayCommand]
        private async Task ToggleEditFavoriteItem(StudentViewModel? student)
        {
            if (student is null) return;

            student.IsFavorite = !student.IsFavorite;

            if (student.IsFavorite)
                _favoriteRepo.AddFavorite(student.StudentNumber);
            else
                _favoriteRepo.RemoveFavorite(student.StudentNumber);

            UpdateEditFavoriteState();

            if (IsEditFavoriteMode && !student.IsFavorite)
            {
                student.IsRemoving = true;
                await Task.Delay(500);
                if (EditStudents.Contains(student))
                    EditStudents.Remove(student);
            }
        }

        [RelayCommand]
        private void LoadMoreEditStudents()
        {
            LoadNextEditPage();
        }

        // ── Debounce ────────────────────────────────────────────────────────

        private async void DebouncedRefreshEditStudents()
        {
            _editSearchDebounceCts?.Cancel();
            _editSearchDebounceCts = new CancellationTokenSource();
            try
            {
                await Task.Delay(300, _editSearchDebounceCts.Token);
                RefreshEditStudents();
            }
            catch (OperationCanceledException) { }
        }

        // ── RefreshEditStudents ─────────────────────────────────────────────

        public async void RefreshEditStudents()
        {
            _editSearchDebounceCts?.Cancel();
            _editStaggerCts?.Cancel();
            _editStaggerCts = new CancellationTokenSource();
            var token = _editStaggerCts.Token;

            var favoriteNumbersList = _favoriteRepo.GetAllFavoriteStudentNumbers();
            var favoriteNumbers = new HashSet<string>(favoriteNumbersList);
            IEnumerable<Student> rawStudents;

            if (IsEditFavoriteMode)
            {
                rawStudents = favoriteNumbersList
                    .Select(sn => _studentRepo.GetByStudentNumber(sn))
                    .Where(s => s != null)!;
            }
            else if (!string.IsNullOrWhiteSpace(EditSearchText))
            {
                rawStudents = _studentRepo.Search(EditSearchText);
            }
            else if (!string.IsNullOrEmpty(EditSelectedClass))
            {
                rawStudents = _studentRepo.GetByClass(EditSelectedClass);
            }
            else
            {
                _allEditViewModels.Clear();
                _editLoadedCount = 0;
                HasMoreEditStudents = false;

                if (EditStudents.Count > 0)
                {
                    try
                    {
                        foreach (var s in EditStudents) s.IsRemoving = true;
                        await Task.Delay(500, token);
                        EditStudents.Clear();
                    }
                    catch (OperationCanceledException) { }
                }
                return;
            }

            // Veli telefon bilgilerini yükle (sınıf bazlı)
            Dictionary<int, string>? guardianPhones = null;
            if (!string.IsNullOrEmpty(EditSelectedClass))
            {
                guardianPhones = _studentRepo.GetGuardianPhonesByClass(EditSelectedClass);
            }

            _allEditViewModels = rawStudents
                .Select(s =>
                {
                    var vm = new StudentViewModel(s, favoriteNumbers.Contains(s.StudentNumber));
                    if (guardianPhones != null && s.GuardianId.HasValue && guardianPhones.TryGetValue(s.GuardianId.Value, out var phone))
                    {
                        vm.GuardianPhone = phone;
                    }
                    return vm;
                })
                .ToList();
            _editLoadedCount = 0;

            try
            {
                if (EditStudents.Count > 0)
                {
                    foreach (var s in EditStudents) s.IsRemoving = true;
                    await Task.Delay(500, token);
                }

                EditStudents.Clear();
                LoadNextEditPage();
            }
            catch (OperationCanceledException) { }
        }

        // ── Sayfalama ───────────────────────────────────────────────────────

        private void LoadNextEditPage()
        {
            var nextBatch = _allEditViewModels
                .Skip(_editLoadedCount)
                .Take(PageSize)
                .ToList();

            int delay = 0;
            foreach (var vm in nextBatch)
            {
                vm.StaggerDelay = delay;
                EditStudents.Add(vm);
                delay += 50;
            }

            _editLoadedCount += nextBatch.Count;
            HasMoreEditStudents = _editLoadedCount < _allEditViewModels.Count;
        }

        // ── Yardımcı ────────────────────────────────────────────────────────

        public void LoadEditClassList()
        {
            EditClassList.Clear();
            foreach (var c in _studentRepo.GetDistinctClasses())
                EditClassList.Add(c);
        }

        private void UpdateEditFavoriteState()
        {
            HasEditFavorites = _favoriteRepo.GetFavoriteCount() > 0;
        }
    }
}
