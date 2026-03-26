using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OBS.DataAccess;
using OBS.Services;
using Microsoft.Win32;

namespace OBS.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // ── Servisler ────────────────────────────────────────────────────────
        private readonly StudentRepository _studentRepo;
        private readonly FavoriteRepository _favoriteRepo;
        private readonly IPdfExtractionService _pdfService;
        private readonly IMergeService _mergeService;
        private readonly ResetSystemService _resetService;
        private readonly PdfExportService _pdfExportService;
        private readonly UpdateService _updateService;

        // ── Pencere Durumu ──────────────────────────────────────────────────
        [ObservableProperty]
        private bool _isTopmost = true;

        // ── Panel Görünürlükleri ────────────────────────────────────────────
        [ObservableProperty]
        private bool _isFavoriteMode = false;

        [ObservableProperty]
        private bool _isClassSelected = false;
        [ObservableProperty]
        private bool _isClassFilterMode = false;

        public bool IsSettingsOverlayVisible
        {
            get => GlobalState.Instance.IsSettingsOverlayVisible;
            set => GlobalState.Instance.IsSettingsOverlayVisible = value;
        }

        public int SelectedSettingsIndex
        {
            get => GlobalState.Instance.SelectedSettingsIndex;
            set => GlobalState.Instance.SelectedSettingsIndex = value;
        }

        public bool IsLoading
        {
            get => GlobalState.Instance.IsLoading;
            set => GlobalState.Instance.IsLoading = value;
        }

        public double LoadingProgress
        {
            get => GlobalState.Instance.LoadingProgress;
            set => GlobalState.Instance.LoadingProgress = value;
        }

        public string LoadingProgressText
        {
            get => GlobalState.Instance.LoadingProgressText;
            set => GlobalState.Instance.LoadingProgressText = value;
        }



        // ── Arama & Filtre ──────────────────────────────────────────────────
        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string? _selectedClass;

        [ObservableProperty]
        private ObservableCollection<string> _classList = new();

        // ── İstatistikler ───────────────────────────────────────────────────
        [ObservableProperty]
        private int _totalStudentCount = 0;

        [ObservableProperty]
        private int _totalClassCount = 0;

        // ── Öğrenci Listesi ─────────────────────────────────────────────────
        [ObservableProperty]
        private ObservableCollection<StudentViewModel> _students = new();

        [ObservableProperty]
        private bool _hasFavorites = false;

        [ObservableProperty]
        private bool _hasMoreStudents = false;
        public bool IsUpdateAvailable
        {
            get => GlobalState.Instance.IsUpdateAvailable;
            set
            {
                if (GlobalState.Instance.IsUpdateAvailable != value)
                {
                    GlobalState.Instance.IsUpdateAvailable = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UpdateVersion
        {
            get => GlobalState.Instance.UpdateVersion;
            set
            {
                if (GlobalState.Instance.UpdateVersion != value)
                {
                    GlobalState.Instance.UpdateVersion = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsUpdateDownloading
        {
            get => GlobalState.Instance.IsUpdateDownloading;
            set
            {
                if (GlobalState.Instance.IsUpdateDownloading != value)
                {
                    GlobalState.Instance.IsUpdateDownloading = value;
                    OnPropertyChanged();
                }
            }
        }

        public int UpdateDownloadProgress
        {
            get => GlobalState.Instance.UpdateDownloadProgress;
            set
            {
                if (GlobalState.Instance.UpdateDownloadProgress != value)
                {
                    GlobalState.Instance.UpdateDownloadProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsForceUpdateRequired
        {
            get => GlobalState.Instance.IsForceUpdateRequired;
            set
            {
                if (GlobalState.Instance.IsForceUpdateRequired != value)
                {
                    GlobalState.Instance.IsForceUpdateRequired = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ForceUpdateMessage
        {
            get => GlobalState.Instance.ForceUpdateMessage;
            set
            {
                if (GlobalState.Instance.ForceUpdateMessage != value)
                {
                    GlobalState.Instance.ForceUpdateMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ForceUpdateProgress
        {
            get => GlobalState.Instance.ForceUpdateProgress;
            set
            {
                if (GlobalState.Instance.ForceUpdateProgress != value)
                {
                    GlobalState.Instance.ForceUpdateProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        // ── Sayfalama (Infinite Scroll)
        private const int PageSize = 6;
        private List<StudentViewModel> _allViewModels = new();
        private int _loadedCount;
        private CancellationTokenSource? _searchDebounceCts;

        // ── Onay Diyaloğu Delegate ──────────────────────────────────────────

        // ── Constructor ─────────────────────────────────────────────────────

        public MainViewModel()
        {
            _studentRepo = new StudentRepository();
            _favoriteRepo = new FavoriteRepository();
            _pdfService = new PdfExtractionService();
            _mergeService = new MergeService();
            _resetService = new ResetSystemService();
            _pdfExportService = new PdfExportService();
            _updateService = new UpdateService();

            LoadClassList();
            UpdateFavoriteState();
            _ = CheckForUpdateSilentlyAsync();

            GlobalState.Instance.OnCheckForUpdateAction = CheckForUpdateAsync;
            GlobalState.Instance.OnResetSystemAction = ResetSystemAsync;
            GlobalState.Instance.OnImportKunyePdfAction = ImportKunyePdfAsync;
            _ = CheckForUpdateSilentlyAsync();
        }

        // ── Partial Callbacks ───────────────────────────────────────────────
        // Kardeş property'leri değiştirirken backing field'a doğrudan yazılır.
        // _isUpdatingFilters bayrağı, WPF UI TextBox'ın OnPropertyChanged
        // sonrası geri-bildirim yaparak OnSearchTextChanged'ı yeniden
        // tetiklemesini engeller. Böylece her kullanıcı aksiyonu tam olarak
        // BİR RefreshStudents() tetikler.

        private bool _isUpdatingFilters;

        partial void OnSelectedClassChanged(string? value)
        {
            Debug.WriteLine($"[FILTER] OnSelectedClassChanged: value='{value}', _searchText='{_searchText}', _isUpdatingFilters={_isUpdatingFilters}");
            if (_isUpdatingFilters) return;
            _isUpdatingFilters = true;
            try
            {
                IsClassSelected = !string.IsNullOrEmpty(value);

                if (_isFavoriteMode && !string.IsNullOrEmpty(value))
                {
                    _isFavoriteMode = false;
                    OnPropertyChanged(nameof(IsFavoriteMode));
                    Students.Clear();
                }

                if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(_searchText))
                {
                    Debug.WriteLine($"[FILTER] Clearing searchText from '{_searchText}'");
                    _searchDebounceCts?.Cancel();
                    _searchText = string.Empty;
                    OnPropertyChanged(nameof(SearchText));
                }

                Debug.WriteLine($"[FILTER] About to call RefreshStudents from OnSelectedClassChanged");
                RefreshStudents();
            }
            finally
            {
                _isUpdatingFilters = false;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            Debug.WriteLine($"[FILTER] OnSearchTextChanged: value='{value}', _selectedClass='{_selectedClass}', _isUpdatingFilters={_isUpdatingFilters}");
            if (_isUpdatingFilters) return;
            _isUpdatingFilters = true;
            try
            {
                if (_isFavoriteMode && !string.IsNullOrWhiteSpace(value))
                {
                    _isFavoriteMode = false;
                    OnPropertyChanged(nameof(IsFavoriteMode));
                    Students.Clear();
                }

                if (!string.IsNullOrWhiteSpace(value) && _selectedClass != null)
                {
                    _selectedClass = null;
                    IsClassSelected = false;
                    OnPropertyChanged(nameof(SelectedClass));
                }

                // Boş girdi → listeyi sıfırla.
                var trimmed = value?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    _searchDebounceCts?.Cancel();

                    // Sınıf seçiliyken arama kutusu programatik olarak temizlendiyse
                    // (OnSelectedClassChanged tarafından), RefreshStudents zaten
                    // sınıf filtresi üzerinden tetiklendi — tekrar çağırmaya gerek yok.
                    // Tekrar çağrılırsa _staggerCts iptal edilir ve ilk sonuç kaybolur.
                    if (_selectedClass != null)
                        return;

                    RefreshStudents();
                    return;
                }

                // Minimum karakter kontrolü:
                //   • Sayısal girdi (numara araması) → 1 karakter yeterli (tek haneli numaralar mevcut).
                //   • Alfabetik girdi (ad/soyad araması) → 2 karakter gerekli.
                bool isNumeric = trimmed.All(char.IsDigit);
                int minLength = isNumeric ? 1 : 2;

                if (trimmed.Length < minLength)
                {
                    _searchDebounceCts?.Cancel();
                    return;
                }

                // Debounce: Her tuşa basmada DB sorgusu yerine 300ms bekle.
                // Kullanıcı yazmayı bitirince tek sorgu gider.
                DebouncedRefreshStudents();
            }
            finally
            {
                _isUpdatingFilters = false;
            }
        }

        private async void DebouncedRefreshStudents()
        {
            _searchDebounceCts?.Cancel();
            _searchDebounceCts = new CancellationTokenSource();
            try
            {
                await Task.Delay(300, _searchDebounceCts.Token);
                RefreshStudents();
            }
            catch (OperationCanceledException) { }
        }

        partial void OnIsFavoriteModeChanged(bool value)
        {
            if (_isUpdatingFilters) return;
            _isUpdatingFilters = true;
            try
            {
                if (value)
                {
                    if (_selectedClass != null)
                    {
                        _selectedClass = null;
                        IsClassSelected = false;
                        OnPropertyChanged(nameof(SelectedClass));
                    }
                    if (!string.IsNullOrEmpty(_searchText))
                    {
                        _searchDebounceCts?.Cancel();
                        _searchText = string.Empty;
                        OnPropertyChanged(nameof(SearchText));
                    }
                }

                RefreshStudents();
            }
            finally
            {
                _isUpdatingFilters = false;
            }
        }

        // ── Komutlar — Toggle / Panel ───────────────────────────────────────

        [RelayCommand]
        private void ToggleTopmost() => IsTopmost = !IsTopmost;

        [RelayCommand]
        private void ToggleFavorite()
        {
            IsFavoriteMode = !IsFavoriteMode;
        }

        [RelayCommand]
        private void OpenSettingsOverlay()
        {
            SelectedSettingsIndex = 0;
            IsSettingsOverlayVisible = true;
        }

        [RelayCommand]
        private void CloseSettingsOverlay()
        {
            IsSettingsOverlayVisible = false;
        }

        [RelayCommand]
        private void OpenTeamManagement()
        {
            OBS.App.NavigationService.NavigateTo<TeamManagementViewModel>();
        }

        [RelayCommand]
        private void CloseFavoritesPanel() => IsFavoriteMode = false;

        [RelayCommand]
        private void ClearClassFilter() => SelectedClass = null;

        [RelayCommand]
        private void ToggleFilterMode()
        {
            IsClassFilterMode = !IsClassFilterMode;

            // Search moduna dönünce sınıf filtresi seçiliyse temizle,
            // böylece liste hemen güncellenir.
            if (!IsClassFilterMode && SelectedClass != null)
            {
                SelectedClass = null;
            }

            // Sınıf moduna geçince arama metni varsa temizle,
            // böylece liste hemen güncellenir.
            if (IsClassFilterMode && !string.IsNullOrEmpty(SearchText))
            {
                SearchText = string.Empty;
            }
        }



        // ── Komutlar — Güncelleme ───────────────────────────────────────────

        private async Task CheckForUpdateSilentlyAsync()
        {
            try
            {
                await Task.Delay(2000); // Uygulama açılışını yavaşlatma

                // 1. Uzak config'i ve Velopack güncelleme kontrolünü paralel çalıştır
                var configTask = _updateService.FetchRemoteConfigAsync();
                var hasUpdateTask = _updateService.CheckForUpdateAsync();
                await Task.WhenAll(configTask, hasUpdateTask);

                var remoteConfig = configTask.Result;
                var hasUpdate = hasUpdateTask.Result;

                // 2. Force update kontrolü
                if (remoteConfig is { ForceUpdate: true } && hasUpdate)
                {
                    var currentVer = _updateService.GetCurrentVersion() ?? "0.0.0";
                    var minVer = remoteConfig.MinRequiredVersion;

                    bool isOutdated = !string.IsNullOrEmpty(minVer)
                        && System.Version.TryParse(currentVer, out var cv)
                        && System.Version.TryParse(minVer, out var mv)
                        && cv < mv;

                    if (isOutdated)
                    {
                        ForceUpdateMessage = string.IsNullOrWhiteSpace(remoteConfig.ForceUpdateMessage)
                            ? "Bu güncelleme zorunludur. Lütfen bekleyin..."
                            : remoteConfig.ForceUpdateMessage;
                        IsForceUpdateRequired = true;

                        // Arka planda indir
                        await _updateService.DownloadUpdateAsync(progress =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                                GlobalState.Instance.ForceUpdateProgress = progress);
                        });

                        // İndirme tamam — yeniden başlat
                        _updateService.ApplyUpdateAndRestart();
                        return;
                    }
                }

                // 3. Normal (isteğe bağlı) güncelleme
                if (hasUpdate)
                {
                    IsUpdateAvailable = true;
                    UpdateVersion = _updateService.GetPendingVersion() ?? "?";
                }
            }
            catch
            {
                // Sessiz kontrol — hata durumunda kullanıcıyı rahatsız etme
            }
        }

        private async Task CheckForUpdateAsync()
        {

            try
            {
                SetLoading(true, "Güncellemeler kontrol ediliyor...");

                var hasUpdate = await _updateService.CheckForUpdateAsync();

                if (hasUpdate)
                {
                    IsUpdateAvailable = true;
                    UpdateVersion = _updateService.GetPendingVersion() ?? "?";
                    ToastService.ShowInfo($"Yeni sürüm mevcut: v{UpdateVersion}");
                }
                else
                {
                    ToastService.ShowSuccess("Uygulama güncel.");
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Güncelleme kontrolü hatası: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        [RelayCommand]
        private async Task DownloadAndApplyUpdateAsync()
        {
            if (!IsUpdateAvailable) return;

            try
            {
                IsUpdateDownloading = true;
                UpdateDownloadProgress = 0;

                await _updateService.DownloadUpdateAsync(progress =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        UpdateDownloadProgress = progress);
                });

                var result = MessageBox.Show(
                    $"v{UpdateVersion} sürümü indirildi.\n\nUygulama yeniden başlatılarak güncellensin mi?",
                    "Güncelleme Hazır",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _updateService.ApplyUpdateAndRestart();
                }
                else
                {
                    _updateService.ApplyUpdateOnExit();
                    ToastService.ShowInfo("Güncelleme uygulama kapatıldığında yüklenecek.");
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Güncelleme indirme hatası: {ex.Message}");
            }
            finally
            {
                IsUpdateDownloading = false;
                IsUpdateAvailable = false;
            }
        }

        // ── Komutlar — Favoriler ────────────────────────────────────────────

        [RelayCommand]
        private async Task ToggleFavoriteItem(StudentViewModel? student)
        {
            if (student is null) return;

            student.IsFavorite = !student.IsFavorite;

            if (student.IsFavorite)
                _favoriteRepo.AddFavorite(student.StudentNumber);
            else
                _favoriteRepo.RemoveFavorite(student.StudentNumber);

            UpdateFavoriteState();

            if (IsFavoriteMode && !student.IsFavorite)
            {
                student.IsRemoving = true;
                await Task.Delay(500); // 500ms animation duration
                if (Students.Contains(student))
                    Students.Remove(student);
            }
        }

        [RelayCommand]
        private async Task ClearAllFavorites()
        {
            _favoriteRepo.ClearAllFavorites();

            foreach (var student in Students)
                student.IsFavorite = false;

            if (IsFavoriteMode)
            {
                foreach (var student in Students)
                    student.IsRemoving = true;

                await Task.Delay(500);
                Students.Clear();
            }

            IsFavoriteMode = false;
            UpdateFavoriteState();
        }

        // ── Komutlar — PDF Yükleme ──────────────────────────────────────────

        private async Task ImportKunyePdfAsync()
        {

            var dialog = new OpenFileDialog
            {
                Title = "PDF Dosyalarını Seçin",
                Filter = "PDF Dosyaları|*.pdf",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true) return;

            var files = dialog.FileNames;
            int totalFiles = files.Length;
            int processed = 0;
            int successCount = 0;
            int failCount = 0;

            try
            {
                SetLoading(true, $"0/{totalFiles} dosya sınıflandırılıyor...");

                // ── Faz 1: PDF türlerini paralel tespit et (salt okunur, güvenli) ──
                var fileTypes = new (string Path, PdfDocumentType Type)[totalFiles];
                await Task.Run(() =>
                {
                    Parallel.For(0, totalFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                        i => fileTypes[i] = (files[i], _pdfService.GetPdfDocumentType(files[i])));
                });

                // ── Faz 2: Türe göre grupla ve işle ─────────────────────────────
                var kunyeFiles = fileTypes.Where(f => f.Type == PdfDocumentType.KunyeDefteri).Select(f => f.Path).ToList();
                var sinifFiles = fileTypes.Where(f => f.Type == PdfDocumentType.SinifListesi).Select(f => f.Path).ToList();
                var resimFiles = fileTypes.Where(f => f.Type == PdfDocumentType.ResimListesi).Select(f => f.Path).ToList();
                failCount = fileTypes.Count(f => f.Type == PdfDocumentType.Unknown);

                SetLoading(true, $"0/{totalFiles} dosya işleniyor...");

                // ── Faz 2a: Künye PDF'leri — I/O ağırlıklı, kontrollü paralellik ──
                var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 2));
                var kunyeTasks = kunyeFiles.Select(async pdfPath =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await ImportKunyeDefteri(pdfPath, silent: true);
                        Interlocked.Increment(ref successCount);
                    }
                    catch
                    {
                        Interlocked.Increment(ref failCount);
                    }
                    finally
                    {
                        semaphore.Release();

                        var current = Interlocked.Increment(ref processed);
                        var progress = (double)current / totalFiles * 100;
                        Application.Current.Dispatcher.Invoke(() =>
                            SetLoading(true, $"{current}/{totalFiles} — {Path.GetFileName(pdfPath)}", progress));
                    }
                });

                await Task.WhenAll(kunyeTasks);

                // Tüm paralel task'lar bittikten sonra temp dosyaları temizle.
                // Paralel döngü içinde yapılırsa Task A, Task B'nin henüz
                // kopyalanmamış temp fotoğrafını silebilir (ConcurrentBag.TryTake race condition).
                _pdfService.CleanupTempFiles();

                // ── Faz 2b: Sınıf listeleri — DB yazma yoğun, sıralı ──────────
                foreach (var pdfPath in sinifFiles)
                {
                    try
                    {
                        await ImportSinifListesi(pdfPath, silent: true);
                        successCount++;
                    }
                    catch { failCount++; }
                    finally
                    {
                        processed++;
                        var progress = (double)processed / totalFiles * 100;
                        SetLoading(true, $"{processed}/{totalFiles} — {Path.GetFileName(pdfPath)}", progress);
                    }
                }

                // ── Faz 2c: Resim listeleri — hafif dosya kopyalama ────────────
                foreach (var pdfPath in resimFiles)
                {
                    try
                    {
                        await ImportResimListesi(pdfPath, silent: true);
                        successCount++;
                    }
                    catch { failCount++; }
                    finally
                    {
                        processed++;
                        var progress = (double)processed / totalFiles * 100;
                        SetLoading(true, $"{processed}/{totalFiles} — {Path.GetFileName(pdfPath)}", progress);
                    }
                }

                LoadClassList();
                RefreshStudents();

                if (failCount == 0)
                    ToastService.ShowSuccess($"{successCount} PDF başarıyla işlendi.");
                else
                    ToastService.ShowInfo($"{successCount} başarılı, {failCount} tanınamadı/hatalı.");

                // Arka planda silinen/taşınan dosyalar için çöp toplayıcıyı çalıştır
                _ = new GarbageCollectorService().RunAsync();
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"PDF yükleme hatası: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task ImportKunyeDefteri(string pdfPath, bool silent = false)
        {
            var students = await Task.Run(() => _pdfService.ExtractStudentsFromKunyePdf(pdfPath).ToList());

            if (students.Count == 0)
            {
                if (!silent) ToastService.ShowError("PDF'den öğrenci verisi çıkarılamadı.");
                return;
            }

            // Künye PDF'lerini Students klasörüne böl
            var studentsPdfFolder = DatabaseConnection.GetStudentsPdfFolder();
            var savedPdfPaths = await Task.Run(() => _pdfService.SplitAndSaveStudentPdfs(pdfPath, studentsPdfFolder).ToList());

            // Öğrenci numarası -> Künye PDF yolu eşleştirmesi
            var kunyePdfMap = new Dictionary<string, string>();
            foreach (var savedPath in savedPdfPaths)
            {
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(savedPath);
                var matchingStudent = students.FirstOrDefault(s =>
                {
                    var expectedName = FileNameHelper.BuildStudentFileName(s.StudentNumber, s.FirstName, s.LastName);
                    return fileNameWithoutExt.StartsWith(expectedName, StringComparison.OrdinalIgnoreCase);
                });
                if (matchingStudent != null && !string.IsNullOrWhiteSpace(matchingStudent.StudentNumber))
                {
                    kunyePdfMap[FileNameHelper.NormalizeStudentNumber(matchingStudent.StudentNumber)] = savedPath;
                }
            }

            await Task.Run(() => _mergeService.MergeStudents(students, kunyePdfMap));

            if (!silent)
            {
                LoadClassList();
                RefreshStudents();
                ToastService.ShowSuccess($"{students.Count} öğrenci başarıyla aktarıldı.");
            }
        }

        private async Task ImportSinifListesi(string pdfPath, bool silent = false)
        {
            var classList = await Task.Run(() => _pdfService.ExtractClassList(pdfPath).ToList());

            if (classList.Count == 0)
            {
                if (!silent) ToastService.ShowError("PDF'den sınıf listesi verisi çıkarılamadı.");
                return;
            }

            // Sınıf listesi PDF'ini (veya çok sayfalı toplu listeyi) Class klasörüne parçalayarak kaydet
            await Task.Run(() =>
            {
                var classFolder = DatabaseConnection.GetClassPdfFolder();
                _pdfService.SplitAndSaveClassListPdf(pdfPath, classFolder, isImageList: false);
            });

            await Task.Run(() => _mergeService.MergeClassList(classList));

            if (!silent)
            {
                LoadClassList();
                RefreshStudents();
                ToastService.ShowSuccess($"{classList.Count} öğrencinin sınıf bilgisi güncellendi.");
            }
        }

        private async Task ImportResimListesi(string pdfPath, bool silent = false)
        {
            // Resim listesi PDF'ini (veya sayfalarını) Class klasörüne parçalayarak kaydet
            await Task.Run(() =>
            {
                var classFolder = DatabaseConnection.GetClassPdfFolder();
                _pdfService.SplitAndSaveClassListPdf(pdfPath, classFolder, isImageList: true);
            });

            if (!silent)
            {
                ToastService.ShowSuccess("Resim listesi başarıyla kaydedildi.");
            }
        }

        // ── Komutlar — Sistem Sıfırlama ─────────────────────────────────────

        private async Task ResetSystemAsync()
        {

            if (GlobalState.Instance.ConfirmAsync != null)
            {
                var firstConfirm = await GlobalState.Instance.ConfirmAsync(
                    "Sistemi Sıfırla",
                    "Sistemi sıfırlamak istediğinizden emin misiniz?\n\n" +
                    "• Tüm öğrenci verileri\n" +
                    "• Tüm takımlar\n" +
                    "• Tüm favoriler\n" +
                    "• Tüm fotoğraflar\n\n" +
                    "kalıcı olarak silinecektir.\n(PIN ayarı korunacaktır)",
                    "Devam Et",
                    "Vazgeç");

                if (!firstConfirm) return;

                var secondConfirm = await GlobalState.Instance.ConfirmAsync(
                    "Son Uyarı",
                    "Bu işlem GERİ ALINAMAZ!\n\nDevam etmek istediğinizden emin misiniz?",
                    "Evet, Sıfırla",
                    "İptal");

                if (!secondConfirm) return;
            }

            try
            {
                SetLoading(true, "Sistem sıfırlanıyor...");

                // Önce UI'daki öğrenci listesini temizle — Image kontrollerinin dosya handle'larını serbest bırakması için
                Students.Clear();
                _allViewModels.Clear();
                _loadedCount = 0;
                HasMoreStudents = false;

                await Task.Run(() => _resetService.ResetSystem());
                ClassList.Clear();
                SelectedClass = null;
                SearchText = string.Empty;
                IsFavoriteMode = false;
                TotalStudentCount = 0;
                TotalClassCount = 0;
                UpdateFavoriteState();

                ToastService.ShowSuccess("Sistem başarıyla sıfırlandı.");
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Sıfırlama hatası: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        // ── Komutlar — PDF / Excel Dışa Aktarım ────────────────────────────

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
                OpenFile(kunyePath);
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"PDF açma hatası: {ex.Message}");
            }
        }

        [RelayCommand]
        private void PrintClassList()
        {
            if (string.IsNullOrEmpty(SelectedClass)) return;

            try
            {
                var classFolder = DatabaseConnection.GetClassPdfFolder();
                var fileName = FileNameHelper.BuildClassListFileName(SelectedClass, isImageList: false) + ".pdf";
                var pdfPath = Path.Combine(classFolder, fileName);

                if (!File.Exists(pdfPath))
                {
                    ToastService.ShowError($"{SelectedClass} sınıfına ait sınıf listesi PDF'i bulunamadı.");
                    return;
                }

                OpenFile(pdfPath);
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Sınıf listesi hatası: {ex.Message}");
            }
        }

        [RelayCommand]
        private void PrintPhotoList()
        {
            if (string.IsNullOrEmpty(SelectedClass)) return;

            try
            {
                var classFolder = DatabaseConnection.GetClassPdfFolder();
                var fileName = FileNameHelper.BuildClassListFileName(SelectedClass, isImageList: true) + ".pdf";
                var pdfPath = Path.Combine(classFolder, fileName);

                if (!File.Exists(pdfPath))
                {
                    ToastService.ShowError($"{SelectedClass} sınıfına ait resim listesi PDF'i bulunamadı.");
                    return;
                }

                OpenFile(pdfPath);
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Fotoğraf listesi hatası: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ExportExcel()
        {
            if (string.IsNullOrEmpty(SelectedClass)) return;

            var dialog = new SaveFileDialog
            {
                Title = "Excel Dosyası Kaydet",
                Filter = "Excel Dosyası|*.xlsx",
                FileName = $"{SelectedClass} Sınıf Listesi.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var students = _studentRepo.GetByClass(SelectedClass).ToList();
                if (students.Count == 0)
                {
                    ToastService.ShowError("Seçili sınıfta dışa aktarılacak öğrenci yok.");
                    return;
                }

                var guardianPhones = _studentRepo.GetGuardianPhonesByClass(SelectedClass);
                var title = $"{SelectedClass} Sınıf Listesi";

                var excelService = new ExcelExportService();
                excelService.ExportTeamToExcel(title, students, guardianPhones, dialog.FileName);
                ToastService.ShowSuccess($"{students.Count} öğrenci Excel olarak aktarıldı.");
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Excel aktarım hatası: {ex.Message}");
            }
        }

        // ── Yardımcı Metotlar ───────────────────────────────────────────────

        private CancellationTokenSource? _staggerCts;

        public async void RefreshStudents()
        {
            Debug.WriteLine($"[REFRESH] RefreshStudents called. SearchText='{SearchText}', SelectedClass='{SelectedClass}', IsFavoriteMode={IsFavoriteMode}, Students.Count={Students.Count}");
            // Bekleyen debounce aramasını da iptal et — yarış durumunu önler.
            _searchDebounceCts?.Cancel();

            _staggerCts?.Cancel();
            _staggerCts = new CancellationTokenSource();
            var token = _staggerCts.Token;

            var favoriteNumbersList = _favoriteRepo.GetAllFavoriteStudentNumbers();
            var favoriteNumbers = new HashSet<string>(favoriteNumbersList);
            IEnumerable<Models.Student> rawStudents;

            if (IsFavoriteMode)
            {
                Debug.WriteLine($"[REFRESH] Branch: FavoriteMode");
                rawStudents = favoriteNumbersList
                    .Select(sn => _studentRepo.GetByStudentNumber(sn))
                    .Where(s => s != null)!;
            }
            else if (!string.IsNullOrWhiteSpace(SearchText))
            {
                Debug.WriteLine($"[REFRESH] Branch: Search '{SearchText}'");
                rawStudents = _studentRepo.Search(SearchText);
            }
            else if (!string.IsNullOrEmpty(SelectedClass))
            {
                Debug.WriteLine($"[REFRESH] Branch: Class '{SelectedClass}'");
                rawStudents = _studentRepo.GetByClass(SelectedClass);
            }
            else
            {
                Debug.WriteLine($"[REFRESH] Branch: CLEAR ALL");
                _allViewModels.Clear();
                _loadedCount = 0;
                HasMoreStudents = false;

                if (Students.Count > 0)
                {
                    try
                    {
                        foreach (var s in Students) s.IsRemoving = true;
                        // Öğrencilerin aşağı doğru kaybolma animasyonu (0.5 sn) = 500 ms
                        await Task.Delay(500, token);
                        Students.Clear();
                    }
                    catch (OperationCanceledException) { Debug.WriteLine($"[REFRESH] CLEAR ALL cancelled"); }
                }
                return;
            }

            _allViewModels = rawStudents
                .Select(s => new StudentViewModel(s, favoriteNumbers.Contains(s.StudentNumber)))
                .ToList();
            _loadedCount = 0;
            Debug.WriteLine($"[REFRESH] _allViewModels.Count={_allViewModels.Count}, Students.Count={Students.Count}");

            try
            {
                if (Students.Count > 0)
                {
                    foreach (var s in Students) s.IsRemoving = true;
                    // Eğer arama yapılıyorsa veya sınıf değiştiriliyorsa da kaybolma animasyonu için bekleyiş ekleyebiliriz
                    // Eskiden 150 veya 50 idi. Bunu da çok bekletmemek adına 300ms falan yapabiliriz ama kullanıcı animasyon süresini
                    // üstteki gibi aynı (500ms) tutalım dediği için bunu da 500ms yapıyoruz.
                    int fadeDelay = 500; 
                    Debug.WriteLine($"[REFRESH] Awaiting fadeDelay={fadeDelay}ms");
                    await Task.Delay(fadeDelay, token);
                    Debug.WriteLine($"[REFRESH] fadeDelay completed");
                }

                Students.Clear();
                LoadNextPage();
                Debug.WriteLine($"[REFRESH] LoadNextPage done, Students.Count={Students.Count}");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[REFRESH] *** CANCELLED during fade! Students NOT loaded. ***");
            }
        }

        /// <summary>
        /// Sonraki sayfayı (PageSize kadar) Students koleksiyonuna ekler.
        /// Scroll-to-bottom tetiklemesiyle çağrılır.
        /// </summary>
        [RelayCommand]
        private void LoadMoreStudents()
        {
            if (_loadedCount >= _allViewModels.Count) return;
            LoadNextPage();
        }

        private void LoadNextPage()
        {
            var nextBatch = _allViewModels.Skip(_loadedCount).Take(PageSize).ToList();
            int delay = 0;
            foreach (var vm in nextBatch)
            {
                vm.StaggerDelay = delay;
                delay += 50; // Her kart 50ms sonrasında görünsün (Şelale effekti)
                Students.Add(vm);
                _loadedCount++;
            }
            HasMoreStudents = _loadedCount < _allViewModels.Count;
        }

        public void LoadClassList()
        {
            var classes = _studentRepo.GetDistinctClasses();
            ClassList = new ObservableCollection<string>(classes);
            TotalClassCount = classes.Count;
            TotalStudentCount = _studentRepo.GetCount();
        }

        public void RefreshDashboard()
        {
            LoadClassList();
            RefreshStudents();
        }

        private void UpdateFavoriteState()
        {
            HasFavorites = _favoriteRepo.GetFavoriteCount() > 0;
            if (!HasFavorites && IsFavoriteMode)
            {
                IsFavoriteMode = false;
            }
        }

        private void SetLoading(bool isLoading, string text = "", double progress = 0)
        {
            IsLoading = isLoading;
            LoadingProgressText = isLoading ? text : "%0";
            LoadingProgress = progress;
        }

        private static void OpenFile(string filePath)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }

        public void ClearAllStudents()
        {
            Students.Clear();
        }
    }
}
