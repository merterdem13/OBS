using System;

namespace OBS.Services
{
    public sealed record RecoverySetupStartupState(
        string Title,
        string Message,
        bool IsCurrentRecoveryPinRequired,
        string CurrentRecoveryPinInput,
        string NewRecoveryPinInput,
        bool HasRecoveryPinError);

    public sealed record RecoverySetupStartupResult(
        bool ShouldShowModal,
        RecoverySetupStartupState? Payload);

    public class RecoverySetupService
    {
        private readonly Func<string, string?> _getSetting;

        public RecoverySetupService(Func<string, string?>? getSetting = null)
        {
            _getSetting = getSetting ?? (key => new DataAccess.SettingsRepository().GetSetting(key));
        }

        public RecoverySetupStartupResult BuildStartupState()
        {
            try
            {
                var hasSeenModal = _getSetting("HasSeenRecoveryModal");
                if (hasSeenModal == "true")
                {
                    return new RecoverySetupStartupResult(false, null);
                }

                return new RecoverySetupStartupResult(
                    true,
                    new RecoverySetupStartupState(
                        "İlk Kurulum - Kurtarma Kodu",
                        "Uygulamaya hoş geldiniz! \nVarsayılan şifre sıfırlama (kurtarma) kodunuz '0000' olarak belirlenmiştir.\n\nGüvenliğiniz için bu kodu şimdi kişiselleştirebilirsiniz veya 'Vazgeç' diyerek daha sonra ayarlardan değiştirebilirsiniz.",
                        false,
                        string.Empty,
                        string.Empty,
                        false));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Recovery modal hazırlanırken hata oluştu: {ex.Message}");
                return new RecoverySetupStartupResult(false, null);
            }
        }
    }
}
