using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using OBS.DataAccess;
using OBS.Helpers;
using OBS.Services;

namespace OBS.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private const string RecoveryPin = "0000";

        private readonly SettingsRepository _settingsRepo;
        private bool _isCreateMode = false;
        private bool _isRecoveryMode = false;
        private string _confirmPin = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PinDisplay))]
        private string _pinInput = string.Empty;

        [ObservableProperty]
        private string _titleMessage = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public string PinDisplay => new string('\u25CF', PinInput.Length);

        public LoginViewModel()
        {
            _settingsRepo = new SettingsRepository();
            CheckInitialState();
        }

        [RelayCommand]
        private void Numpad(string digit)
        {
            if (PinInput.Length < 4 && int.TryParse(digit, out _))
            {
                PinInput += digit;
                ErrorMessage = string.Empty;

                if (PinInput.Length == 4)
                {
                    Task.Delay(200).ContinueWith(_ =>
                        Application.Current.Dispatcher.Invoke(Confirm));
                }
            }
        }

        [RelayCommand]
        private void Delete()
        {
            if (PinInput.Length > 0)
            {
                PinInput = PinInput[..^1];
                ErrorMessage = string.Empty;
            }
        }

        [RelayCommand]
        private void Confirm()
        {
            if (!PinHashHelper.IsValidPinFormat(PinInput))
            {
                ErrorMessage = "PIN 4 haneli olmalidir!";
                PinInput = string.Empty;
                return;
            }

            if (_isCreateMode)
            {
                if (string.IsNullOrEmpty(_confirmPin))
                {
                    if (PinInput == RecoveryPin)
                    {
                        ErrorMessage = "Bu PIN kullanilamaz!";
                        PinInput = string.Empty;
                        return;
                    }

                    _confirmPin = PinInput;
                    PinInput = string.Empty;
                    TitleMessage = "PIN'i Tekrar Girin";
                    ErrorMessage = string.Empty;
                }
                else
                {
                    if (_confirmPin == PinInput)
                    {
                        var hashedPin = PinHashHelper.HashPin(PinInput);
                        _settingsRepo.SetSetting("PIN", hashedPin);
                        _isRecoveryMode = false;
                        OpenMainWindow("PIN basariyla olusturuldu!");
                    }
                    else
                    {
                        ErrorMessage = "PIN'ler eslesmiyor!";
                        PinInput = string.Empty;
                        _confirmPin = string.Empty;
                        TitleMessage = _isRecoveryMode
                            ? "Kurtarma - Yeni PIN Olusturun"
                            : "Yeni PIN Olusturun";
                    }
                }
            }
            else
            {
                if (PinInput == RecoveryPin)
                {
                    _isCreateMode = true;
                    _isRecoveryMode = true;
                    _confirmPin = string.Empty;
                    PinInput = string.Empty;
                    TitleMessage = "Kurtarma - Yeni PIN Olusturun";
                    ErrorMessage = string.Empty;
                    return;
                }

                var storedHash = _settingsRepo.GetSetting("PIN");
                if (PinHashHelper.VerifyPin(PinInput, storedHash ?? string.Empty))
                {
                    OpenMainWindow();
                }
                else
                {
                    ErrorMessage = "Hatali PIN!";
                    PinInput = string.Empty;
                }
            }
        }

        private void CheckInitialState()
        {
            if (PinResetHelper.ResetFileExists())
            {
                _isCreateMode = true;
                TitleMessage = "PIN Sifirlama - Yeni PIN Olusturun";
                PinResetHelper.DeleteResetFile();
                return;
            }

            var storedPin = _settingsRepo.GetSetting("PIN");
            if (string.IsNullOrWhiteSpace(storedPin))
            {
                _isCreateMode = true;
                TitleMessage = "Hos Geldiniz - Yeni PIN Olusturun";
            }
            else
            {
                _isCreateMode = false;
                TitleMessage = "PIN Girisi";
            }
        }

        private void OpenMainWindow(string? toastMessage = null)
        {
            OBS.App.NavigationService.NavigateTo<MainViewModel>();

            var mainWindow = Application.Current.MainWindow as OBS.Views.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.CheckAndShowReleaseNotes();

                if (!string.IsNullOrEmpty(toastMessage))
                {
                    ToastService.ShowSuccess(toastMessage, mainWindow);
                }
            }
        }
    }
}
