using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace AlparslanOBS.Services
{
    /// <summary>
    /// Velopack üzerinden GitHub Releases'a bağlanarak
    /// güncelleme kontrolü, indirme ve uygulama işlemlerini yönetir.
    /// </summary>
    public class UpdateService
    {
        private const string GitHubRepoUrl = "https://github.com/merterdem13/AlparslanOBS";

        private readonly UpdateManager _updateManager;
        private UpdateInfo? _pendingUpdate;

        public UpdateService()
        {
            _updateManager = new UpdateManager(new GithubSource(GitHubRepoUrl, null, false));
        }

        /// <summary>
        /// Mevcut uygulama sürümünü döndürür.
        /// Velopack tarafından yönetilmiyorsa (geliştirme ortamı) null döner.
        /// </summary>
        public string? GetCurrentVersion()
        {
            return _updateManager.IsInstalled
                ? _updateManager.CurrentVersion?.ToString()
                : null;
        }

        /// <summary>
        /// Uygulamanın Velopack tarafından yönetilip yönetilmediğini döndürür.
        /// Geliştirme ortamında false olur.
        /// </summary>
        public bool IsInstalled => _updateManager.IsInstalled;

        /// <summary>
        /// GitHub Releases'dan yeni sürüm olup olmadığını kontrol eder.
        /// </summary>
        /// <returns>Yeni sürüm varsa true, yoksa false.</returns>
        public async Task<bool> CheckForUpdateAsync()
        {
            try
            {
                _pendingUpdate = await _updateManager.CheckForUpdatesAsync();
                return _pendingUpdate != null;
            }
            catch
            {
                _pendingUpdate = null;
                return false;
            }
        }

        /// <summary>
        /// Bekleyen güncellemenin sürüm bilgisini döndürür.
        /// </summary>
        public string? GetPendingVersion()
        {
            return _pendingUpdate?.TargetFullRelease?.Version?.ToString();
        }

        /// <summary>
        /// Güncellemeyi indirir. İlerleme callback'i ile yüzde bilgisi verir.
        /// </summary>
        public async Task DownloadUpdateAsync(Action<int>? progressCallback = null)
        {
            if (_pendingUpdate is null) return;

            await _updateManager.DownloadUpdatesAsync(
                _pendingUpdate,
                progress => progressCallback?.Invoke(progress));
        }

        /// <summary>
        /// İndirilen güncellemeyi uygular ve uygulamayı yeniden başlatır.
        /// </summary>
        public void ApplyUpdateAndRestart()
        {
            if (_pendingUpdate is null) return;
            _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
        }

        /// <summary>
        /// İndirilen güncellemeyi uygular (yeniden başlatmadan).
        /// Sonraki açılışta yeni sürüm kullanılır.
        /// </summary>
        public void ApplyUpdateOnExit()
        {
            if (_pendingUpdate is null) return;
            _updateManager.ApplyUpdatesAndExit(_pendingUpdate);
        }
    }
}
