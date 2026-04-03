using System;
using System.IO;
using Newtonsoft.Json;
using OBS.Helpers;
using OBS.ViewModels;

namespace OBS.Services
{
    public class ReleaseNotesService
    {
        private readonly Func<string> _baseDirectoryProvider;
        private readonly Func<string, bool> _fileExists;
        private readonly Func<string, string> _readAllText;
        private readonly Func<string?> _getLastSeenVersion;
        private readonly Action<string> _setLastSeenVersion;
        private readonly Action _saveSettings;

        public ReleaseNotesService(
            Func<string>? baseDirectoryProvider = null,
            Func<string, bool>? fileExists = null,
            Func<string, string>? readAllText = null,
            Func<string?>? getLastSeenVersion = null,
            Action<string>? setLastSeenVersion = null,
            Action? saveSettings = null)
        {
            _baseDirectoryProvider = baseDirectoryProvider ?? (() => AppDomain.CurrentDomain.BaseDirectory);
            _fileExists = fileExists ?? File.Exists;
            _readAllText = readAllText ?? File.ReadAllText;
            _getLastSeenVersion = getLastSeenVersion ?? (() => LocalSettings.Current.LastSeenReleaseNotesVersion);
            _setLastSeenVersion = setLastSeenVersion ?? (version => LocalSettings.Current.LastSeenReleaseNotesVersion = version);
            _saveSettings = saveSettings ?? LocalSettings.Save;
        }

        public ReleaseNotesViewModel? GetReleaseNotesToShow()
        {
            try
            {
                var releaseNotesPath = Path.Combine(_baseDirectoryProvider(), "ReleaseNotes.json");
                if (!_fileExists(releaseNotesPath))
                {
                    return null;
                }

                var json = _readAllText(releaseNotesPath);
                var viewModel = JsonConvert.DeserializeObject<ReleaseNotesViewModel>(json);
                if (viewModel == null || string.IsNullOrWhiteSpace(viewModel.Version))
                {
                    return null;
                }

                var lastSeenVersion = _getLastSeenVersion();
                if (!string.IsNullOrWhiteSpace(lastSeenVersion) && lastSeenVersion == viewModel.Version)
                {
                    return null;
                }

                return viewModel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Release notes okunurken hata oluştu: {ex.Message}");
                return null;
            }
        }

        public void MarkReleaseNotesAsSeen(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return;
            }

            _setLastSeenVersion(version);
            _saveSettings();
        }
    }
}
