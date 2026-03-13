using ElectronNET.API.Entities;
using Messentra.Infrastructure.AutoUpdater;

namespace Messentra.ComponentTests;

public sealed class FakeAutoUpdaterService : IAutoUpdaterService
{
    public event Action<UpdateInfo>? UpdateAvailable;
    public event Action? UpdateNotAvailable;
    public event Action<UpdateInfo>? UpdateDownloaded;
    public event Action<ProgressInfo>? DownloadProgress;
    public event Action<string>? Error;

    public Task<string> GetCurrentVersion() => Task.FromResult("v1.0.0");
    public Task CheckForUpdates() => Task.CompletedTask;
    public Task DownloadUpdate() => Task.CompletedTask;
    public void QuitAndInstall(bool isSilent = true, bool isForceRunAfter = false) { }

    public void RaiseUpdateAvailable(UpdateInfo info) => UpdateAvailable?.Invoke(info);
    public void RaiseUpdateNotAvailable() => UpdateNotAvailable?.Invoke();
    public void RaiseUpdateDownloaded(UpdateInfo info) => UpdateDownloaded?.Invoke(info);
    public void RaiseDownloadProgress(ProgressInfo info) => DownloadProgress?.Invoke(info);
    public void RaiseError(string message) => Error?.Invoke(message);
}