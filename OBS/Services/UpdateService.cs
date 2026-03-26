using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using OBS.Models;
using Velopack;
using Velopack.Sources;

namespace OBS.Services
{
    /// <summary>
    /// Velopack üzerinden GitHub Releases'a bağlanarak
    /// güncelleme kontrolü, indirme ve uygulama işlemlerini yönetir.
    /// Dijital imza doğrulamasını destekler.
    /// </summary>
    public class UpdateService
    {
        private const string GitHubRepoUrl = "https://github.com/merterdem13/OBS";
        
        // TODO: Gerçek RSA Public Key bilginizi (Base64) buraya ekleyin.
        // Bu anahtar, indirilen pakedin '.sig' dosyasını doğrulamak için kullanılacak.
        private const string RsaPublicKeyBase64 = "MIICCgKCAgEA7KL87IVLL5JXZ026CcArG+JwQtHIYSi2czf8ttFLl88uePMJUbA6+hwBLKsdAHWd12uY6g4OwgJgJ+ZNpogAMVKLlqLk6Y7PMhLSwFLYs2lB3fVAap78l2jsNDp50bAvOZ1ZPAOSDE8S4Q88fryOk6pDjH+BuAOCdcucf6w1dYIxOfPuv1+sOBeiSGaLyC2JyMMZ8xdw3/3O61cwadj/KPl6OmtoDbSM3lcDP8qXQNBdw92lhX2u7lyKXvPluRkdeOueXzvRgYdEgrR1kVGquwhyUtG0481Y8AuWX+3fCQ/996mutliBaGW2k/+7vwwyAkUFxefsk6gzAQdRch135awRqVqv9yblPncLq997a9At5Jus15x6elox5Jgbi1lIZzthwiJ+nsn4HsvAxcTaWjzlHW8GKFI0FDWTOKst7aItPzibYxL5u2Mc4S25SxMEHeEVwH3ZhkB43Nnx7oeXAOCZNja7AdbKMT/7bxd0oOGI911rH/BcC7UcpoMynPSV+uPSmTLjzwTKZQBR9nukye8U+HUrDSeFiQuRhY0MLPaL69VE2tJxvjJwqGJ6C/eQkUSOqMhU9exdM6cXVIRBoCc4Q6kNUiZYpheKx+ITb0vGCjYzTiaC1e4DbyEFf/5Z+xY0lMIf0gsZW3gUyQtomVJpS0beNH/UGYuvkigaJlECAwEAAQ==";

        private readonly UpdateManager _updateManager;
        private UpdateInfo? _pendingUpdate;
        private readonly HttpClient _httpClient;

        public UpdateService()
        {
            _updateManager = new UpdateManager(new GithubSource(GitHubRepoUrl, null, false));
            _httpClient = new HttpClient();
        }

        public string? GetCurrentVersion()
        {
            return _updateManager.IsInstalled
                ? _updateManager.CurrentVersion?.ToString()
                : null;
        }

        public bool IsInstalled => _updateManager.IsInstalled;

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
            catch
            {
                return null; // Ağ hatası — sessiz geç
            }
        }

        public async Task DownloadUpdateAsync(Action<int>? progressCallback = null)
        {
            if (_pendingUpdate is null) return;

            // 1. Velopack ile güncellemeyi indir
            await _updateManager.DownloadUpdatesAsync(
                _pendingUpdate,
                progress => progressCallback?.Invoke(progress));

            // 2. İndirilen paketin imzasını doğrula
            await VerifyUpdateSignatureAsync();
        }

        private async Task VerifyUpdateSignatureAsync()
        {
            if (_pendingUpdate is null) return;

            // Velopack bir delta güncellemesi indirdiyse onu, yoksa full güncellemeyi doğrulamamız gerekir.
            var isDelta = _pendingUpdate.TargetFullRelease == null;
            // TargetFullRelease null ise, demek ki DeltaRelease kullanılıyor Velopack api'sine göre. Wait, Velopack'ta Delta yoksa ne oluyor?
            // Actually, UpdateInfo has `TargetFullRelease` but it also has `HasDeltaRelease` or similar properties?
            // Velopack'te `UpdateInfo` objesinde `TargetFullRelease` her zaman vardır, indirme işlemi delta yapsa bile
            // son paket genellikle o bilgiyle eşlenir. Ancak indirilen paketin ismi delta isminde olabilir.
            
            // Güncelleme dosyasının indirme sonrasında nereye kaydedildiğini bulmak için 
            // _pendingUpdate içinden Asset almak yerine Velopack cache dizinine bakmalıyız.
            
            // Velopack'in yeni API'sinde indirilen paketin adı genellikle TargetFullRelease.FileName olur
            // Ancak, Velopack arka planda delta uygulayıp FULL bir nupkg çıkarır.
            // Bu nedenle bizim doğrulayacağımız şey daima TargetFullRelease.FileName ismiyle bulunacak tam pakettir.
            var packageFileName = _pendingUpdate.TargetFullRelease?.FileName;
            if (string.IsNullOrEmpty(packageFileName))
                throw new Exception("İndirilecek paket adı bulunamadı.");

            var currentExePath = AppDomain.CurrentDomain.BaseDirectory;
            var installDir = Directory.GetParent(currentExePath)?.Parent?.FullName ?? currentExePath;
            var packagesDir = Path.Combine(installDir, "packages");
            var packagePath = Path.Combine(packagesDir, packageFileName);

            if (!File.Exists(packagePath))
            {
                packagesDir = Path.Combine(currentExePath, "packages");
                packagePath = Path.Combine(packagesDir, packageFileName);

                if (!File.Exists(packagePath))
                {
                    throw new FileNotFoundException($"İndirilen paket dosyası bulunamadı ({packageFileName}). Güvenlik doğrulaması yapılamıyor.", packagePath);
                }
            }

            var version = _pendingUpdate.TargetFullRelease!.Version.ToString();
            var sigUrl = $"{GitHubRepoUrl}/releases/download/v{version}/{packageFileName}.sig";

            byte[] signatureBytes;
            try
            {
                signatureBytes = await _httpClient.GetByteArrayAsync(sigUrl);
            }
            catch (Exception ex)
            {
                File.Delete(packagePath);
                throw new System.Security.SecurityException("İmza dosyası (.sig) bulunamadı. Güncelleme iptal edildi. Delta paketi kullanıldıysa Full paketin imzasının eklendiğinden emin olun.", ex);
            }

            bool isValid = VerifyRsaSignature(packagePath, signatureBytes, RsaPublicKeyBase64);

            if (!isValid)
            {
                 File.Delete(packagePath);
                 throw new System.Security.SecurityException("İndirilen güncelleme paketinin RSA imzası GEÇERSİZ! Güvenlik sebebiyle dosya silindi.");
            }
        }

        private bool VerifyRsaSignature(string filePath, byte[] signature, string publicKeyBase64)
        {
            try
            {
                using var rsa = RSA.Create();
                // Public key'i import et (standart RSA Base64)
                rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKeyBase64), out _);

                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash(fs);

                // Hash'in (SHA256) RSA imzasını doğrula
                return rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false; // Hata (ör: public key formatı hatalıysa) durumunda geçersiz sayılır
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
