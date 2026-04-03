using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OBS.Helpers;

namespace OBS.ViewModels
{
    public partial class GlobalState : ObservableObject
    {
        private static readonly Lazy<GlobalState> _instance = new(() => new GlobalState());
        public static GlobalState Instance => _instance.Value;

        [ObservableProperty]
        private string _currentTheme;

        private GlobalState()
        {
            var settingsRepo = new OBS.DataAccess.SettingsRepository();
            _currentTheme = settingsRepo.GetSetting("Theme") ?? "Light";
        }

        [ObservableProperty]
        private bool _isSettingsOverlayVisible = false;

        [ObservableProperty]
        private int _selectedSettingsIndex = 0;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private double _loadingProgress = 0;

        [ObservableProperty]
        private string _loadingProgressText = "%0";

        [ObservableProperty]
        private bool _isUpdateAvailable = false;

        [ObservableProperty]
        private string _updateVersion = string.Empty;

        [ObservableProperty]
        private bool _isUpdateDownloading = false;

        [ObservableProperty]
        private int _updateDownloadProgress = 0;

        [ObservableProperty]
        private bool _isForceUpdateRequired = false;

        [ObservableProperty]
        private string _forceUpdateMessage = string.Empty;

        [ObservableProperty]
        private int _forceUpdateProgress = 0;

        public Func<string, string, string, string, Task<bool>>? ConfirmAsync { get; set; }

        public void ShowLoading(string message = "Yükleniyor...", double progress = 0)
        {
            LoadingProgressText = message;
            LoadingProgress = progress;
            IsLoading = true;
        }

        public void HideLoading()
        {
            IsLoading = false;
        }

        [RelayCommand]
        private void CloseSettingsOverlay()
        {
            IsSettingsOverlayVisible = false;
        }

        [ObservableProperty]
        private bool _isStudentNotesOverlayVisible = false;

        [ObservableProperty]
        private StudentViewModel? _selectedStudentForNotes = null;

        [RelayCommand]
        private void OpenStudentNotes(StudentViewModel student)
        {
            if (student == null) return;
            SelectedStudentForNotes = student;
            IsStudentNotesOverlayVisible = true;
        }

        [RelayCommand]
        private void CloseStudentNotes()
        {
            IsStudentNotesOverlayVisible = false;
            SelectedStudentForNotes = null;
        }

        [RelayCommand]
        private void ChangeTheme(string themeString)
        {
            if (CurrentTheme == themeString) return;

            CurrentTheme = themeString;
            
            var settingsRepo = new OBS.DataAccess.SettingsRepository();
            settingsRepo.SetSetting("Theme", themeString);
            
            Helpers.ThemeManager.ApplyTheme(themeString);

            // Hard Reset!
            var currentWindow = System.Windows.Application.Current.MainWindow as Views.MainWindow;
            if (currentWindow != null)
            {
                var newWindow = new Views.MainWindow();
                newWindow.Left = currentWindow.Left;
                newWindow.Top = currentWindow.Top;

                IsSettingsOverlayVisible = true;
                SelectedSettingsIndex = 2; // 2 = DisplaySettingsPage

                System.Windows.Application.Current.MainWindow = newWindow;
                newWindow.Opacity = 0;
                newWindow.Show();
                currentWindow.Close();
            }
        }

        public Func<Task>? OnCheckForUpdateAction { get; set; }
        public Func<Task>? OnResetSystemAction { get; set; }
        public Func<Task>? OnImportKunyePdfAction { get; set; }

        [RelayCommand]
        private async Task CheckForUpdate()
        {
            if (OnCheckForUpdateAction != null)
                await OnCheckForUpdateAction.Invoke();
        }

        [RelayCommand]
        private async Task ResetSystem()
        {
            if (OnResetSystemAction != null)
                await OnResetSystemAction.Invoke();
        }

        [RelayCommand]
        private async Task ImportKunyePdf()
        {
            if (OnImportKunyePdfAction != null)
                await OnImportKunyePdfAction.Invoke();
        }

        // --- Kurtarma Kodu Ayarlari ---
        [ObservableProperty]
        private bool _isChangeRecoveryPinOverlayVisible = false;

        [ObservableProperty]
        private string _changeRecoveryPinTitle = "Kurtarma Kodunu Ayarla";

        [ObservableProperty]
        private string _changeRecoveryPinMessage = "Uygulama şifrenizi unuttuğunuzda sıfırlayabilmek için kullandığınız 4 haneli kurtarma kodunu güvenliğiniz için güncelleyin.";

        [ObservableProperty]
        private string _currentRecoveryPinInput = string.Empty;

        [ObservableProperty]
        private string _newRecoveryPinInput = string.Empty;

        [ObservableProperty]
        private string _recoveryPinErrorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasRecoveryPinError = false;

        [ObservableProperty]
        private bool _isCurrentRecoveryPinRequired = false;

        [RelayCommand]
        private void OpenChangeRecoveryPinOverlay()
        {
            var settingsRepo = new OBS.DataAccess.SettingsRepository();
            var storedRecovery = settingsRepo.GetSetting("RecoveryPIN");
            
            IsCurrentRecoveryPinRequired = !string.IsNullOrWhiteSpace(storedRecovery) && storedRecovery != "0000";
            
            CurrentRecoveryPinInput = string.Empty;
            NewRecoveryPinInput = string.Empty;
            HasRecoveryPinError = false;
            RecoveryPinErrorMessage = string.Empty;
            
            IsChangeRecoveryPinOverlayVisible = true;
        }

        [RelayCommand]
        private void CancelChangeRecoveryPin()
        {
            IsChangeRecoveryPinOverlayVisible = false;
            
            // Eğer ilk oturum açma sırasındaki bildirimi geçiyorsa da flag ekleyebiliriz
            var settingsRepo = new OBS.DataAccess.SettingsRepository();
            settingsRepo.SetSetting("HasSeenRecoveryModal", "true");
        }

        [RelayCommand]
        private void SaveRecoveryPin()
        {
            HasRecoveryPinError = false;
            RecoveryPinErrorMessage = string.Empty;

            var settingsRepo = new OBS.DataAccess.SettingsRepository();
            var storedRecovery = settingsRepo.GetSetting("RecoveryPIN");
            var currentPin = string.IsNullOrWhiteSpace(storedRecovery) ? "0000" : storedRecovery;

            if (IsCurrentRecoveryPinRequired && CurrentRecoveryPinInput != currentPin)
            {
                HasRecoveryPinError = true;
                RecoveryPinErrorMessage = "Mevcut kurtarma kodu hatalı!";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewRecoveryPinInput) || NewRecoveryPinInput.Length != 4 || !int.TryParse(NewRecoveryPinInput, out _))
            {
                HasRecoveryPinError = true;
                RecoveryPinErrorMessage = "Yeni kurtarma kodu 4 haneli bir sayı olmalıdır!";
                return;
            }

            if (NewRecoveryPinInput == "1923")
            {
                HasRecoveryPinError = true;
                RecoveryPinErrorMessage = "Bu kurtarma kodu güvenlik gerekçesiyle kullanılamaz!";
                return;
            }

            var storedUserPinHash = settingsRepo.GetSetting("PIN");
            if (!string.IsNullOrEmpty(storedUserPinHash) && OBS.Helpers.PinHashHelper.VerifyPin(NewRecoveryPinInput, storedUserPinHash))
            {
                HasRecoveryPinError = true;
                RecoveryPinErrorMessage = "Kurtarma kodu, giriş şifreniz ile aynı olamaz!";
                return;
            }

            settingsRepo.SetSetting("RecoveryPIN", NewRecoveryPinInput);
            settingsRepo.SetSetting("HasSeenRecoveryModal", "true");

            IsChangeRecoveryPinOverlayVisible = false;
            
            var mainWindow = System.Windows.Application.Current.MainWindow as OBS.Views.MainWindow;
            if (mainWindow != null)
            {
                 OBS.Services.ToastService.ShowSuccess("Kurtarma kodu başarıyla güncellendi.", mainWindow);
            }
        }

        // --- Geliştirici PIN Modalı ---
        [ObservableProperty]
        private bool _isDeveloperPinOverlayVisible = false;

        [ObservableProperty]
        private string _developerPinInput = string.Empty;

        [ObservableProperty]
        private bool _hasDeveloperPinError = false;

        // "Login" veya "Settings" - hangi ekrandan tetiklendi
        [ObservableProperty]
        private string _developerPinSource = string.Empty;

        [RelayCommand]
        private void OpenDeveloperPinOverlay(string source)
        {
            DeveloperPinInput = string.Empty;
            HasDeveloperPinError = false;
            DeveloperPinSource = source;
            IsDeveloperPinOverlayVisible = true;
        }

        [RelayCommand]
        private void CancelDeveloperPin()
        {
            IsDeveloperPinOverlayVisible = false;
        }

        [RelayCommand]
        private void VerifyDeveloperPin()
        {
            if (DeveloperPinInput == "1923")
            {
                IsDeveloperPinOverlayVisible = false;

                if (DeveloperPinSource == "Login")
                {
                    // Geliştirici bypass: Login'i atlayıp HomeView'e geç
                    var settingsRepo = new OBS.DataAccess.SettingsRepository();
                    
                    // PIN ve Recovery kodunu sıfırla (bir sonraki girişte yeni PIN oluşturulacak)
                    settingsRepo.SetSetting("PIN", "");
                    settingsRepo.SetSetting("RecoveryPIN", "");
                    settingsRepo.SetSetting("HasSeenRecoveryModal", "");

                    // Doğrudan HomeView'e geç
                    OBS.App.NavigationService.NavigateTo<MainViewModel>();

                    var mainWindow = System.Windows.Application.Current.MainWindow as OBS.Views.MainWindow;
                    if (mainWindow != null)
                    {
                        OBS.Services.ToastService.ShowSuccess("Geliştirici erişimi: Şifre ve kurtarma kodu sıfırlandı.", mainWindow);
                        // Recovery modal'ı göster (yeni kurtarma kodu belirlensin)
                        mainWindow.CheckAndShowRecoveryModalAsync().Forget(nameof(OBS.Views.MainWindow.CheckAndShowRecoveryModalAsync));
                    }
                }
                else if (DeveloperPinSource == "Settings")
                {
                    // Ayarlar: PIN ve kurtarma kodunu sıfırla
                    var settingsRepo = new OBS.DataAccess.SettingsRepository();
                    settingsRepo.SetSetting("PIN", "");
                    settingsRepo.SetSetting("RecoveryPIN", "");
                    settingsRepo.SetSetting("HasSeenRecoveryModal", "");

                    var mainWindow = System.Windows.Application.Current.MainWindow as OBS.Views.MainWindow;
                    if (mainWindow != null)
                    {
                        OBS.Services.ToastService.ShowSuccess("Geliştirici erişimi: Şifre ve kurtarma kodu sıfırlandı. Yeniden giriş gerekecek.", mainWindow);
                    }
                }
            }
            else
            {
                // Yanlış PIN: sessizce kapat
                HasDeveloperPinError = true;
                System.Windows.Threading.DispatcherTimer timer = new()
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    IsDeveloperPinOverlayVisible = false;
                    HasDeveloperPinError = false;
                };
                timer.Start();
            }
        }

        // --- Öğrenci Düzenleme Modal ---
        [ObservableProperty]
        private bool _isStudentEditModalVisible = false;

        [ObservableProperty]
        private StudentViewModel? _selectedStudentForEdit = null;

        [ObservableProperty]
        private System.Collections.ObjectModel.ObservableCollection<string> _editClassList = new();

        // Edit modal form alanları
        [ObservableProperty]
        private string _editFirstName = string.Empty;

        [ObservableProperty]
        private string _editLastName = string.Empty;

        [ObservableProperty]
        private string _editStudentNumber = string.Empty;

        [ObservableProperty]
        private string _editTcNo = string.Empty;

        [ObservableProperty]
        private string _editClassName = string.Empty;

        [ObservableProperty]
        private DateTime? _editBirthDate;

        public string EditBirthDateString => EditBirthDate?.ToString("dd.MM.yyyy") ?? "";

        [ObservableProperty]
        private string _editPhotoPath = string.Empty;

        // Per-field validation errors
        [ObservableProperty]
        private bool _hasEditFirstNameError = false;
        [ObservableProperty]
        private string _editFirstNameError = string.Empty;

        [ObservableProperty]
        private bool _hasEditLastNameError = false;
        [ObservableProperty]
        private string _editLastNameError = string.Empty;

        [ObservableProperty]
        private bool _hasEditStudentNumberError = false;
        [ObservableProperty]
        private string _editStudentNumberError = string.Empty;

        [ObservableProperty]
        private bool _hasEditTcNoError = false;
        [ObservableProperty]
        private string _editTcNoError = string.Empty;

        [ObservableProperty]
        private bool _hasEditClassError = false;
        [ObservableProperty]
        private string _editClassError = string.Empty;

        [RelayCommand]
        private void OpenStudentEditModal(StudentViewModel student)
        {
            if (student == null) return;
            ClearFieldErrors();
            SelectedStudentForEdit = student;
            EditFirstName = student.FirstName;
            EditLastName = student.LastName;
            EditStudentNumber = student.StudentNumber;
            EditTcNo = student.TcNo;
            EditClassName = student.Class;
            EditBirthDate = student.BirthDate;
            EditPhotoPath = student.PhotoPath;
            LoadEditClassList();
            OnPropertyChanged(nameof(EditBirthDateString));
            IsStudentEditModalVisible = true;
        }

        private void LoadEditClassList()
        {
            var repo = new DataAccess.StudentRepository();
            var classes = repo.GetDistinctClasses();
            EditClassList = new System.Collections.ObjectModel.ObservableCollection<string>(classes);
        }

        private void ClearFieldErrors()
        {
            HasEditFirstNameError = false; EditFirstNameError = string.Empty;
            HasEditLastNameError = false; EditLastNameError = string.Empty;
            HasEditStudentNumberError = false; EditStudentNumberError = string.Empty;
            HasEditTcNoError = false; EditTcNoError = string.Empty;
            HasEditClassError = false; EditClassError = string.Empty;
        }

        [RelayCommand]
        private void CloseStudentEditModal()
        {
            IsStudentEditModalVisible = false;
            SelectedStudentForEdit = null;
        }

        [RelayCommand]
        private async Task SaveStudentEditAsync()
        {
            ClearFieldErrors();
            bool hasError = false;

            if (string.IsNullOrWhiteSpace(EditFirstName))
            {
                HasEditFirstNameError = true;
                EditFirstNameError = "Ad alanı boş olamaz!";
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(EditLastName))
            {
                HasEditLastNameError = true;
                EditLastNameError = "Soyad alanı boş olamaz!";
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(EditStudentNumber))
            {
                HasEditStudentNumberError = true;
                EditStudentNumberError = "Numara alanı boş olamaz!";
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(EditTcNo))
            {
                HasEditTcNoError = true;
                EditTcNoError = "TC alanı boş olamaz!";
                hasError = true;
            }
            else if (EditTcNo.Length != 11 || !EditTcNo.All(char.IsDigit))
            {
                HasEditTcNoError = true;
                EditTcNoError = "TC 11 haneli bir sayı olmalıdır!";
                hasError = true;
            }

            if (hasError) return;

            if (SelectedStudentForEdit == null) return;

            var repo = new DataAccess.StudentRepository();

            if (repo.IsStudentNumberTaken(EditStudentNumber, SelectedStudentForEdit.StudentNumber))
            {
                HasEditStudentNumberError = true;
                EditStudentNumberError = "Bu numara başka bir öğrenciye ait!";
                hasError = true;
            }

            if (repo.IsTcNoTaken(EditTcNo, SelectedStudentForEdit.StudentNumber))
            {
                HasEditTcNoError = true;
                EditTcNoError = "Bu TC başka bir öğrenciye ait!";
                hasError = true;
            }

            if (hasError) return;

            try
            {
                var original = SelectedStudentForEdit.GetModel();
                var updated = new Models.Student
                {
                    StudentNumber = EditStudentNumber,
                    FirstName = EditFirstName,
                    LastName = EditLastName,
                    Class = EditClassName,
                    ClassNo = original.ClassNo,
                    TcNo = EditTcNo,
                    BirthDate = EditBirthDate,
                    PhotoPath = EditPhotoPath,
                    Gender = original.Gender,
                    GuardianId = original.GuardianId,
                    KunyePdfPath = original.KunyePdfPath,
                    SpecialNote = original.SpecialNote
                };

                repo.UpdateStudent(original, updated);

                Services.ToastService.ShowSuccess("Öğrenci bilgileri güncellendi.");
                IsStudentEditModalVisible = false;
                SelectedStudentForEdit = null;

                var mainWindow = System.Windows.Application.Current.MainWindow as Views.MainWindow;
                if (mainWindow?.DataContext is ShellViewModel shell && shell.CurrentView is MainViewModel mainVm)
                {
                    mainVm.RefreshDashboard();
                }
            }
            catch (Exception ex)
            {
                HasEditStudentNumberError = true;
                EditStudentNumberError = $"Kayıt hatası: {ex.Message}";
            }
        }

        // --- Öğrenci Düzenleme Modu ---
        [ObservableProperty]
        private bool _isStudentEditModeActive = false;

        [RelayCommand]
        private void EnterStudentEditMode()
        {
            IsStudentEditModeActive = true;
            IsSettingsOverlayVisible = false;
            OBS.App.NavigationService.NavigateTo<MainViewModel>();
        }

        [RelayCommand]
        private void ExitStudentEditMode()
        {
            IsStudentEditModeActive = false;
        }
    }
}
