using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using OBS.DataAccess;

namespace OBS.Services
{
    public sealed class BackupRestoreService
    {
        private const string BackupRootFolderName = "OBS_System";

        public string CreateBackup(string destinationFilePath)
        {
            if (string.IsNullOrWhiteSpace(destinationFilePath))
            {
                throw new ArgumentException("Yedek dosya yolu belirtilmedi.");
            }

            DatabaseConnection.EnsureDatabase();
            DatabaseConnection.PrepareForFileOperation();

            var sourceFolder = DatabaseConnection.GetAppFolder();
            var stagingRoot = CreateTempDirectory();

            try
            {
                var stagedDataFolder = Path.Combine(stagingRoot, BackupRootFolderName);
                CopyDirectory(sourceFolder, stagedDataFolder);

                var manifestPath = Path.Combine(stagingRoot, "manifest.json");
                var manifest = new BackupManifest
                {
                    AppVersion = typeof(BackupRestoreService).Assembly.GetName().Version?.ToString() ?? "?",
                    CreatedAt = DateTime.Now,
                    DataFolderName = BackupRootFolderName
                };

                File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented), Encoding.UTF8);

                if (File.Exists(destinationFilePath))
                {
                    File.Delete(destinationFilePath);
                }

                ZipFile.CreateFromDirectory(stagingRoot, destinationFilePath, CompressionLevel.Optimal, false);
                return destinationFilePath;
            }
            finally
            {
                TryDeleteDirectory(stagingRoot);
            }
        }

        public void StartRestore(string backupFilePath)
        {
            if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
            {
                throw new FileNotFoundException("Yedek dosyası bulunamadı.", backupFilePath);
            }

            DatabaseConnection.EnsureDatabase();
            DatabaseConnection.PrepareForFileOperation();

            var extractRoot = CreateTempDirectory();
            ZipFile.ExtractToDirectory(backupFilePath, extractRoot, true);

            var manifestPath = Path.Combine(extractRoot, "manifest.json");
            var dataFolder = Path.Combine(extractRoot, BackupRootFolderName);
            if (!File.Exists(manifestPath) || !Directory.Exists(dataFolder))
            {
                TryDeleteDirectory(extractRoot);
                throw new InvalidOperationException("Yedek dosyası geçersiz veya eksik.");
            }

            var safetyBackupPath = Path.Combine(Path.GetTempPath(), $"obs-pre-restore-{DateTime.Now:yyyyMMdd-HHmmss}.obsbackup");
            CreateBackup(safetyBackupPath);

            var scriptPath = Path.Combine(Path.GetTempPath(), $"obs-restore-{Guid.NewGuid():N}.ps1");
            var appFolder = DatabaseConnection.GetAppFolder();
            var executablePath = Process.GetCurrentProcess().MainModule?.FileName
                ?? throw new InvalidOperationException("Uygulama yolu alınamadı.");
            var currentProcessId = Environment.ProcessId;

            var script = $@"
$ErrorActionPreference = 'Stop'
$pidToWait = {currentProcessId}
$sourceDir = '{EscapePowerShellString(dataFolder)}'
$targetDir = '{EscapePowerShellString(appFolder)}'
$exePath = '{EscapePowerShellString(executablePath)}'
$scriptPath = '{EscapePowerShellString(scriptPath)}'

while (Get-Process -Id $pidToWait -ErrorAction SilentlyContinue) {{ Start-Sleep -Milliseconds 300 }}

if (Test-Path $targetDir) {{ Remove-Item $targetDir -Recurse -Force }}
New-Item -ItemType Directory -Path $targetDir | Out-Null
Copy-Item -Path (Join-Path $sourceDir '*') -Destination $targetDir -Recurse -Force
Start-Process -FilePath $exePath
Remove-Item $scriptPath -Force
";

            File.WriteAllText(scriptPath, script, Encoding.UTF8);

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), $"obs-backup-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var filePath in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(filePath);
                File.Copy(filePath, Path.Combine(targetDir, fileName), true);
            }

            foreach (var directoryPath in Directory.GetDirectories(sourceDir))
            {
                var directoryName = Path.GetFileName(directoryPath);
                CopyDirectory(directoryPath, Path.Combine(targetDir, directoryName));
            }
        }

        private static void TryDeleteDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
            }
            catch
            {
                // Temp cleanup best-effort.
            }
        }

        private static string EscapePowerShellString(string value)
        {
            return value.Replace("'", "''");
        }

        private sealed class BackupManifest
        {
            public string AppVersion { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public string DataFolderName { get; set; } = string.Empty;
        }
    }
}
