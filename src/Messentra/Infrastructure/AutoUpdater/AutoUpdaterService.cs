using ElectronNET.API;
using ElectronNET.API.Entities;

namespace Messentra.Infrastructure.AutoUpdater;

public interface IAutoUpdaterService
{
    event Action<UpdateInfo>? UpdateAvailable;
    event Action? UpdateNotAvailable;
    event Action<UpdateInfo>? UpdateDownloaded;
    event Action<ProgressInfo>? DownloadProgress;
    event Action<string>? Error;
    
    Task<string> GetCurrentVersion();
    Task CheckForUpdates();
    Task DownloadUpdate();
    void QuitAndInstall(bool isSilent = true, bool isForceRunAfter = false);
}

public sealed class AutoUpdaterService : IAutoUpdaterService
{
    public event Action<UpdateInfo>? UpdateAvailable;
    public event Action? UpdateNotAvailable;
    public event Action<UpdateInfo>? UpdateDownloaded;
    public event Action<ProgressInfo>? DownloadProgress;
    public event Action<string>? Error;
    
    private readonly ILogger<AutoUpdaterService> _logger;

    public AutoUpdaterService(ILogger<AutoUpdaterService> logger)
    {
        _logger = logger;

        SetupAutoUpdater();
    }
    
    public async Task<string> GetCurrentVersion()
    {
        _logger.LogInformation("Getting current version...");
        var semVer = await Electron.AutoUpdater.CurrentVersionAsync;
        _logger.LogInformation("Current version: {SemVer}", semVer.Version);
        
        return $"v{semVer.Version}";
    }

    public async Task CheckForUpdates()
    {
        _logger.LogInformation("Checking for updates...");
        await Electron.AutoUpdater.CheckForUpdatesAsync();
        _logger.LogInformation("Update check complete");
    }

    public async Task DownloadUpdate()
    {
        _logger.LogInformation("Downloading update...");
        await Electron.AutoUpdater.DownloadUpdateAsync();
        _logger.LogInformation("Update download complete");
    }

    public void QuitAndInstall(bool isSilent = true, bool isForceRunAfter = false)
    {
        _logger.LogInformation("Quit and installing update...");
        Electron.AutoUpdater.QuitAndInstall(isSilent, isForceRunAfter);
        _logger.LogInformation("Update quit complete");
    }
    
    private void SetupAutoUpdater()
    {
        Electron.AutoUpdater.AutoDownload = false;
        Electron.AutoUpdater.AutoInstallOnAppQuit = false;
        Electron.AutoUpdater.FullChangelog = true;
        Electron.AutoUpdater.OnCheckingForUpdate += () =>
        {
            _logger.LogInformation("Checking for update...");
        };
        Electron.AutoUpdater.OnUpdateAvailable += info =>
        {
            try
            {
                _logger.LogInformation("Update available: {Version}", info.Version);
                
                UpdateAvailable?.Invoke(info);    
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle update available event");
            }
            
        };
        Electron.AutoUpdater.OnUpdateNotAvailable += _ =>
        {
            try
            {
                _logger.LogInformation("Update not available");
                
                UpdateNotAvailable?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle update not available event");
            }
        };
        Electron.AutoUpdater.OnUpdateDownloaded += info =>
        {
            try
            {
                _logger.LogInformation("Update downloaded: {Version}", info.Version);
                
                UpdateDownloaded?.Invoke(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle update downloaded event");
            }
            
        };
        Electron.AutoUpdater.OnDownloadProgress += info =>
        {
            try
            {
                _logger.LogInformation("Download progress: {Percent}", info.Percent);
                
                DownloadProgress?.Invoke(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle download progress event");
            }
            
        };
        Electron.AutoUpdater.OnError += message =>
        {
            try
            {
                _logger.LogInformation("Error: {Message}", message);
                
                Error?.Invoke(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle error event");
            }
            
        };
    }
}