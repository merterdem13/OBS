using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OBS.Helpers;
using OBS.Models;
using OBS.ViewModels;

namespace OBS.Services
{
    public class UpdateCoordinatorService
    {
        private const int MaxDeferredUpdateCount = 3;

        private readonly UpdateService _updateService = new();
        private readonly SemaphoreSlim _downloadLock = new(1, 1);
        private readonly object _checkLock = new();
        private Task<UpdateCheckResult>? _startupCheckTask;
        private TaskCompletionSource<bool>? _updateModalTcs;

        public UpdateCoordinatorService()
        {
            GlobalState.Instance.OnCheckForUpdateAction = CheckForUpdateFromSettingsAsync;
            GlobalState.Instance.OnDownloadAndApplyUpdateAction = DownloadAndApplyUpdateAsync;
        }

        public void StartStartupUpdateCheck()
        {
            EnsureStartupCheck();
            HandleMandatoryUpdateIfNeededAsync().Forget(nameof(HandleMandatoryUpdateIfNeededAsync));
        }

        public async Task ShowNormalUpdateModalIfNeededAsync()
        {
            var result = await EnsureStartupCheck();
            if (!result.HasUpdate || GlobalState.Instance.IsForceUpdateRequired)
                return;

            ApplyPendingUpdateState();
            var version = _updateService.GetPendingVersion() ?? "?";
            var settings = LocalSettings.Current;

            if (!string.Equals(settings.DeferredUpdateVersion, version, StringComparison.OrdinalIgnoreCase))
            {
                settings.DeferredUpdateVersion = version;
                settings.DeferredUpdateCount = 0;
                LocalSettings.Save();
            }

            GlobalState.Instance.IsUpdateModalMandatory = settings.DeferredUpdateCount >= MaxDeferredUpdateCount;
            GlobalState.Instance.IsUpdateModalVisible = true;

            _updateModalTcs = new TaskCompletionSource<bool>();
            await _updateModalTcs.Task;
        }

        public void DeferNormalUpdate()
        {
            if (GlobalState.Instance.IsUpdateModalMandatory)
                return;

            var version = _updateService.GetPendingVersion() ?? GlobalState.Instance.UpdateVersion;
            var settings = LocalSettings.Current;

            if (!string.Equals(settings.DeferredUpdateVersion, version, StringComparison.OrdinalIgnoreCase))
            {
                settings.DeferredUpdateVersion = version;
                settings.DeferredUpdateCount = 0;
            }

            settings.DeferredUpdateCount = Math.Min(MaxDeferredUpdateCount, settings.DeferredUpdateCount + 1);
            LocalSettings.Save();

            CloseUpdateModal();
        }

        public void DownloadFromNormalUpdateModal()
        {
            CloseUpdateModal();
            DownloadAndApplyUpdateAsync().Forget(nameof(DownloadAndApplyUpdateAsync));
        }

        private async Task CheckForUpdateFromSettingsAsync()
        {
            try
            {
                SetLoading(true, "Guncellemeler kontrol ediliyor...");
                var result = await RunFreshUpdateCheckAsync();

                if (result.HasUpdate)
                {
                    ApplyPendingUpdateState();
                    ToastService.ShowInfo($"Yeni surum mevcut: v{GlobalState.Instance.UpdateVersion}");
                }
                else
                {
                    ClearPendingUpdateState();
                    ToastService.ShowSuccess("Uygulama guncel.");
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Guncelleme kontrolu hatasi: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task DownloadAndApplyUpdateAsync()
        {
            if (!GlobalState.Instance.HasPendingUpdate && !_updateService.HasPendingUpdate)
                return;

            await _downloadLock.WaitAsync();
            try
            {
                var state = GlobalState.Instance;
                state.IsUpdateDownloading = true;
                state.UpdateDownloadProgress = 0;
                state.IsUpdateAvailable = true;

                await _updateService.DownloadUpdateAsync(progress =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        state.UpdateDownloadProgress = progress;
                        state.ForceUpdateProgress = progress;
                    });
                });

                var result = MessageBox.Show(
                    $"v{state.UpdateVersion} surumu indirildi.\n\nUygulama yeniden baslatilarak guncellensin mi?",
                    "Guncelleme Hazir",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _updateService.ApplyUpdateAndRestart();
                }
                else
                {
                    _updateService.ApplyUpdateOnExit();
                    ToastService.ShowInfo("Guncelleme uygulama kapatildiginda yuklenecek.");
                }

                ClearDeferredUpdateState();
                ClearPendingUpdateState();
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Guncelleme indirme hatasi: {ex.Message}");
            }
            finally
            {
                GlobalState.Instance.IsUpdateDownloading = false;
                _downloadLock.Release();
            }
        }

        private async Task HandleMandatoryUpdateIfNeededAsync()
        {
            var result = await EnsureStartupCheck();
            if (!result.HasUpdate || result.RemoteConfig is not { ForceUpdate: true })
                return;

            var currentVersion = _updateService.GetCurrentVersion() ?? "0.0.0";
            var minVersion = result.RemoteConfig.MinRequiredVersion;
            var isOutdated = !string.IsNullOrWhiteSpace(minVersion)
                && Version.TryParse(currentVersion, out var current)
                && Version.TryParse(minVersion, out var minimum)
                && current < minimum;

            if (!isOutdated)
                return;

            var state = GlobalState.Instance;
            DispatchToUi(() =>
            {
                state.ForceUpdateMessage = string.IsNullOrWhiteSpace(result.RemoteConfig.ForceUpdateMessage)
                    ? "Bu guncelleme zorunludur. Lutfen bekleyin..."
                    : result.RemoteConfig.ForceUpdateMessage;
                state.ForceUpdateProgress = 0;
                state.IsForceUpdateRequired = true;
            });
            ApplyPendingUpdateState();

            try
            {
                await _updateService.DownloadUpdateAsync(progress =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        state.ForceUpdateProgress = progress;
                        state.UpdateDownloadProgress = progress;
                    });
                });

                _updateService.ApplyUpdateAndRestart();
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Zorunlu guncelleme indirilemedi: {ex.Message}");
            }
        }

        private Task<UpdateCheckResult> EnsureStartupCheck()
        {
            lock (_checkLock)
            {
                _startupCheckTask ??= RunUpdateCheckAsync();
                return _startupCheckTask;
            }
        }

        private Task<UpdateCheckResult> RunFreshUpdateCheckAsync()
        {
            lock (_checkLock)
            {
                _startupCheckTask = RunUpdateCheckAsync();
                return _startupCheckTask;
            }
        }

        private async Task<UpdateCheckResult> RunUpdateCheckAsync()
        {
            var configTask = _updateService.FetchRemoteConfigAsync();
            var updateTask = _updateService.CheckForUpdateAsync();
            await Task.WhenAll(configTask, updateTask);

            if (updateTask.Result)
                ApplyPendingUpdateState();

            return new UpdateCheckResult(updateTask.Result, configTask.Result);
        }

        private void ApplyPendingUpdateState()
        {
            DispatchToUi(() =>
            {
                var state = GlobalState.Instance;
                state.HasPendingUpdate = true;
                state.IsUpdateAvailable = true;
                state.UpdateVersion = _updateService.GetPendingVersion() ?? "?";
            });
        }

        private static void ClearPendingUpdateState()
        {
            DispatchToUi(() =>
            {
                var state = GlobalState.Instance;
                state.HasPendingUpdate = false;
                state.IsUpdateAvailable = false;
                state.IsUpdateModalVisible = false;
            });
        }

        private static void ClearDeferredUpdateState()
        {
            LocalSettings.Current.DeferredUpdateVersion = null;
            LocalSettings.Current.DeferredUpdateCount = 0;
            LocalSettings.Save();
        }

        private void CloseUpdateModal()
        {
            GlobalState.Instance.IsUpdateModalVisible = false;
            _updateModalTcs?.TrySetResult(true);
            _updateModalTcs = null;
        }

        private static void SetLoading(bool isLoading, string text = "", double progress = 0)
        {
            DispatchToUi(() =>
            {
                var state = GlobalState.Instance;
                state.IsLoading = isLoading;
                state.LoadingProgressText = isLoading ? text : "%0";
                state.LoadingProgress = progress;
            });
        }

        private static void DispatchToUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.Invoke(action);
        }

        private sealed record UpdateCheckResult(bool HasUpdate, UpdateConfig? RemoteConfig);
    }
}
