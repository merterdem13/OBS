using OBS.Services;
using OBS.ViewModels;
using Xunit;

namespace OBS.Tests.Services;

public class ReleaseNotesServiceTests
{
    [Fact]
    public void GetReleaseNotesToShow_returns_null_when_release_notes_file_is_missing()
    {
        var service = new ReleaseNotesService(
            baseDirectoryProvider: () => @"C:\app",
            fileExists: _ => false,
            readAllText: _ => throw new InvalidOperationException("should not read"),
            getLastSeenVersion: () => null);

        var result = service.GetReleaseNotesToShow();

        Assert.Null(result);
    }

    [Fact]
    public void GetReleaseNotesToShow_returns_view_model_when_release_notes_version_is_new()
    {
        var service = new ReleaseNotesService(
            baseDirectoryProvider: () => @"C:\app",
            fileExists: path => path == @"C:\app\ReleaseNotes.json",
            readAllText: _ => """
                {
                  "Version": "2.4.5",
                  "Date": "29-03-2026",
                  "CriticalWarning": "",
                  "Features": ["Tema ayarlari guncellendi."]
                }
                """,
            getLastSeenVersion: () => "2.4.4");

        var result = service.GetReleaseNotesToShow();

        Assert.NotNull(result);
        Assert.Equal("2.4.5", result.Version);
        Assert.Equal("29-03-2026", result.Date);
        Assert.Single(result.Features);
    }

    [Fact]
    public void GetReleaseNotesToShow_returns_null_when_version_was_already_seen()
    {
        var service = new ReleaseNotesService(
            baseDirectoryProvider: () => @"C:\app",
            fileExists: _ => true,
            readAllText: _ => "{" + "\"Version\":\"2.4.5\"}",
            getLastSeenVersion: () => "2.4.5");

        var result = service.GetReleaseNotesToShow();

        Assert.Null(result);
    }

    [Fact]
    public void MarkReleaseNotesAsSeen_updates_local_settings_and_saves()
    {
        string? savedVersion = null;
        var saveCalls = 0;

        var service = new ReleaseNotesService(
            baseDirectoryProvider: () => @"C:\app",
            getLastSeenVersion: () => null,
            setLastSeenVersion: version => savedVersion = version,
            saveSettings: () => saveCalls++);

        service.MarkReleaseNotesAsSeen("2.4.5");

        Assert.Equal("2.4.5", savedVersion);
        Assert.Equal(1, saveCalls);
    }
}
