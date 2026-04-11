using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using OBS.Models;
using Velopack;
using Velopack.Sources;

namespace OBS.Services
{
    public class UpdateService
    {
        private const string GitHubRepoUrl = "https://github.com/merterdem13/OBS";

        private const string RsaPublicKeyBase64 = "MIICCgKCAgEA7KL87IVLL5JXZ026CcArG+JwQtHIYSi2czf8ttFLl88uePMJUbA6+hwBLKsdAHWd12uY6g4OwgJgJ+ZNpogAMVKLlqLk6Y7PMhLSwFLYs2lB3fVAap78l2jsNDp50bAvOZ1ZPAOSDE8S4Q88fryOk6pDjH+BuAOCdcucf6w1dYIxOfPuv1+sOBeiSGaLyC2JyMMZ8xdw3/3O61cwadj/KPl6OmtoDbSM3lcDP8qXQNBdw92lhX2u7lyKXvPluRkdeOueXzvRgYdEgrR1kVGquwhyUtG0481Y8AuWX+3fCQ/996mutliBaGW2k/+7vwwyAkUFxefsk6gzAQdRch135awRqVqv9yblPncLq997a9At5Jus15x6elox5Jgbi1lIZzthwiJ+nsn4HsvAxcTaWjzlHW8GKFI0FDWTOKst7aItPzibYxL5u2Mc4S25SxMEHeEVwH3ZhkB43Nnx7oeXAOCZNja7AdbKMT/7bxd0oOGI911rH/BcC7UcpoMynPSV+uPSmTLjzwTKZQBR9nukye8U+HUrDSeFiQuRhY0MLPaL69VE2tJxvjJwqGJ6C/eQkUSOqMhU9exdM6cXVIRBoCc4Q6kNUiZYpheKx+ITb0vGCjYzTiaC1e4DbyEFf/5Z+xY0lMIf0gsZW3gUyQtomVJpS0beNH/UGYuvkigaJlECAwEAAQ==";

        private readonly UpdateManager _updateManager;
        private readonly HttpClient _httpClient;
        private UpdateInfo? _pendingUpdate;

        public UpdateService()
        {
            _updateManager = new UpdateManager(new GithubSource(GitHubRepoUrl, null, false));
            _httpClient = new HttpClient();
        }

        public bool IsInstalled => _updateManager.IsInstalled;

        public bool HasPendingUpdate => _pendingUpdate != null;

        public string? GetCurrentVersion()
        {
            return _updateManager.IsInstalled
                ? _updateManager.CurrentVersion?.ToString()
                : null;
        }

        public async Task<bool> CheckForUpdateAsync()
        {
            try
            {
                _pendingUpdate = await _updateManager.CheckForUpdatesAsync();
                LogUpdatePlan();
                return _pendingUpdate != null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Update] Check failed: {ex}");
                _pendingUpdate = null;
                return false;
            }
        }

        public string? GetPendingVersion()
        {
            return _pendingUpdate?.TargetFullRelease?.Version?.ToString();
        }

        public async Task<UpdateConfig?> FetchRemoteConfigAsync()
        {
            try
            {
                const string configUrl = "https://raw.githubusercontent.com/merterdem13/OBS/main/update-config.json";
                var json = await _httpClient.GetStringAsync(configUrl);
                return JsonSerializer.Deserialize<UpdateConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Update] Config fetch failed: {ex.Message}");
                return null;
            }
        }

        public async Task DownloadUpdateAsync(Action<int>? progressCallback = null)
        {
            if (_pendingUpdate is null)
                return;

            LogUpdatePlan();

            await _updateManager.DownloadUpdatesAsync(
                _pendingUpdate,
                progress => progressCallback?.Invoke(progress));

            LogDownloadedPackages();
            await VerifyUpdateSignatureAsync();
        }

        private async Task VerifyUpdateSignatureAsync()
        {
            if (_pendingUpdate?.TargetFullRelease is null)
                return;

            var package = FindDownloadedPackageToVerify();
            var packagePath = package.Path;
            var packageFileName = package.FileName;
            var version = _pendingUpdate.TargetFullRelease.Version.ToString();
            var sigUrl = $"{GitHubRepoUrl}/releases/download/v{version}/{packageFileName}.sig";

            byte[] signatureBytes;
            try
            {
                signatureBytes = await _httpClient.GetByteArrayAsync(sigUrl);
            }
            catch (Exception ex)
            {
                File.Delete(packagePath);
                throw new System.Security.SecurityException($"Imza dosyasi bulunamadi. Paket: {packageFileName}", ex);
            }

            if (!VerifyRsaSignature(packagePath, signatureBytes, RsaPublicKeyBase64))
            {
                File.Delete(packagePath);
                throw new System.Security.SecurityException("Indirilen guncelleme paketinin RSA imzasi gecersiz.");
            }
        }

        private (string FileName, string Path) FindDownloadedPackageToVerify()
        {
            if (_pendingUpdate?.TargetFullRelease is null)
                throw new InvalidOperationException("Bekleyen guncelleme bulunamadi.");

            var candidates = new List<string> { _pendingUpdate.TargetFullRelease.FileName };
            candidates.AddRange(_pendingUpdate.DeltasToTarget.Select(delta => delta.FileName));

            foreach (var fileName in candidates.Where(name => !string.IsNullOrWhiteSpace(name)))
            {
                foreach (var dir in GetPackageDirectories().Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var path = Path.Combine(dir, fileName);
                    if (File.Exists(path))
                        return (fileName, path);
                }
            }

            var targetVersion = _pendingUpdate.TargetFullRelease.Version.ToString();
            foreach (var dir in GetPackageDirectories().Where(Directory.Exists))
            {
                var package = Directory.GetFiles(dir, $"OBS-{targetVersion}-*.nupkg")
                    .OrderBy(path => path.Contains("-delta.", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .FirstOrDefault();

                if (package != null)
                    return (Path.GetFileName(package), package);
            }

            throw new FileNotFoundException($"Indirilen guncelleme paketi bulunamadi. Aranan paketler: {string.Join(", ", candidates)}");
        }

        private IEnumerable<string> GetPackageDirectories()
        {
            var currentExePath = AppDomain.CurrentDomain.BaseDirectory;
            var installDir = Directory.GetParent(currentExePath)?.Parent?.FullName ?? currentExePath;

            yield return Path.Combine(installDir, "packages");
            yield return Path.Combine(currentExePath, "packages");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OBS", "packages");
        }

        private void LogUpdatePlan()
        {
            if (_pendingUpdate?.TargetFullRelease is null)
                return;

            var target = _pendingUpdate.TargetFullRelease;
            var deltas = _pendingUpdate.DeltasToTarget?.ToArray() ?? Array.Empty<VelopackAsset>();
            var deltaText = deltas.Length == 0
                ? "none"
                : string.Join(", ", deltas.Select(delta => $"{delta.FileName} ({delta.Size} bytes)"));

            Debug.WriteLine($"[Update] Target={target.Version} Full={target.FileName} ({target.Size} bytes) Deltas={deltaText}");
        }

        private void LogDownloadedPackages()
        {
            if (_pendingUpdate?.TargetFullRelease is null)
                return;

            var targetVersion = _pendingUpdate.TargetFullRelease.Version.ToString();
            foreach (var dir in GetPackageDirectories().Where(Directory.Exists))
            {
                foreach (var package in Directory.GetFiles(dir, $"OBS-{targetVersion}-*.nupkg"))
                {
                    var info = new FileInfo(package);
                    Debug.WriteLine($"[Update] DownloadedPackage={info.Name} Size={info.Length} Path={info.FullName}");
                }
            }
        }

        private bool VerifyRsaSignature(string filePath, byte[] signature, string publicKeyBase64)
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKeyBase64), out _);

                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(fs);

                return rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }

        public void ApplyUpdateAndRestart()
        {
            if (_pendingUpdate is null) return;
            _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
        }

        public void ApplyUpdateOnExit()
        {
            if (_pendingUpdate is null) return;
            _updateManager.ApplyUpdatesAndExit(_pendingUpdate);
        }
    }
}
