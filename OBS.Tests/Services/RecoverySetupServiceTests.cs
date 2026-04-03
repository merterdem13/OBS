using OBS.Services;
using Xunit;

namespace OBS.Tests.Services;

public class RecoverySetupServiceTests
{
    [Fact]
    public void BuildStartupState_returns_payload_when_modal_not_seen()
    {
        var service = new RecoverySetupService(key => null);

        var result = service.BuildStartupState();

        Assert.True(result.ShouldShowModal);
        Assert.NotNull(result.Payload);
        Assert.Equal("İlk Kurulum - Kurtarma Kodu", result.Payload.Title);
        Assert.Contains("0000", result.Payload.Message);
        Assert.False(result.Payload.IsCurrentRecoveryPinRequired);
        Assert.Equal(string.Empty, result.Payload.CurrentRecoveryPinInput);
        Assert.Equal(string.Empty, result.Payload.NewRecoveryPinInput);
        Assert.False(result.Payload.HasRecoveryPinError);
    }

    [Fact]
    public void BuildStartupState_returns_no_payload_when_modal_was_already_seen()
    {
        var service = new RecoverySetupService(_ => "true");

        var result = service.BuildStartupState();

        Assert.False(result.ShouldShowModal);
        Assert.Null(result.Payload);
    }
}
