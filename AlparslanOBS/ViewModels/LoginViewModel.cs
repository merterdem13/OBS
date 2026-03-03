using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AlparslanOBS.DataAccess;
using AlparslanOBS.Helpers;
using AlparslanOBS.Services;

namespace AlparslanOBS.ViewModels
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
            var loginWindow = Application.Current.Windows.OfType<Views.LoginWindow>().FirstOrDefault();

            if (loginWindow != null)
            {
                var sb = new System.Windows.Media.Animation.Storyboard();
                var anim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = new System.Windows.Duration(System.TimeSpan.FromMilliseconds(400)),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
                };

                sb.Children.Add(anim);
                System.Windows.Media.Animation.Storyboard.SetTarget(anim, loginWindow);
                System.Windows.Media.Animation.Storyboard.SetTargetProperty(anim, new PropertyPath("Opacity"));

                var left = loginWindow.Left;
                var top = loginWindow.Top;

                sb.Completed += (s, e) =>
                {
                    var mainWindow = new Views.MainWindow();
                    mainWindow.Left = left;
                    mainWindow.Top = top;
                    Application.Current.MainWindow = mainWindow;
                    mainWindow.Show();
                    loginWindow.Close();

                    if (!string.IsNullOrEmpty(toastMessage))
                        ToastService.ShowSuccess(toastMessage, mainWindow);
                };

                sb.Begin();
            }
            else
            {
                var mainWindow = new Views.MainWindow();
                mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();

                if (!string.IsNullOrEmpty(toastMessage))
                    ToastService.ShowSuccess(toastMessage, mainWindow);
            }
        }
    }
}
