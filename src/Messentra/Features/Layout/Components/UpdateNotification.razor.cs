using ElectronNET.API.Entities;
using Fluxor;
using Messentra.Features.Layout.State;
using Messentra.Infrastructure.AutoUpdater;
using MudBlazor;

namespace Messentra.Features.Layout.Components;

public partial class UpdateNotification : IDisposable
{
    private readonly IState<AutoUpdateState> _state;
    private readonly IDispatcher _dispatcher;
    private readonly IAutoUpdaterService _autoUpdater;

    public UpdateNotification(IState<AutoUpdateState> state, IDispatcher dispatcher, IAutoUpdaterService autoUpdater)
    {
        _state = state;
        _dispatcher = dispatcher;
        _autoUpdater = autoUpdater;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (!firstRender)
            return;

        RegisterAutoUpdater();
        _dispatcher.Dispatch(new RunAutoUpdaterAction());
    }

    private void RegisterAutoUpdater()
    {
        _autoUpdater.UpdateAvailable += OnUpdateAvailable;
        _autoUpdater.UpdateNotAvailable += OnUpdateNotAvailable;
        _autoUpdater.UpdateDownloaded += OnUpdateDownloaded;
        _autoUpdater.DownloadProgress += OnDownloadProgress;
        _autoUpdater.Error += OnAutoUpdaterError;
    }

    private void OnUpdateAvailable(UpdateInfo updateInfo)
    {
        _dispatcher.Dispatch(new UpdateAvailableAction(updateInfo.Version));
    }

    private void OnUpdateNotAvailable()
    {
        _dispatcher.Dispatch(new UpdateNotAvailableAction());
    }

    private void OnUpdateDownloaded(UpdateInfo updateInfo)
    {
        _dispatcher.Dispatch(new UpdateReadyToInstallAction());
    }

    private void OnDownloadProgress(ProgressInfo progressInfo)
    {
        _dispatcher.Dispatch(new UpdateDownloadProgressAction(progressInfo.Percent));
    }

    private void OnAutoUpdaterError(string errorMessage)
    {
        _dispatcher.Dispatch(new AutoUpdateErrorAction(errorMessage));
    }

    private void OnDownloadClick()
    {
        _dispatcher.Dispatch(new DownloadUpdateAction());
    }

    private void OnInstallClick()
    {
        _dispatcher.Dispatch(new InstallUpdateAction());
    }

    private void OnCloseErrorClick()
    {
        _dispatcher.Dispatch(new DismissUpdateErrorAction());
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;
        
        _autoUpdater.UpdateAvailable -= OnUpdateAvailable;
        _autoUpdater.UpdateNotAvailable -= OnUpdateNotAvailable;
        _autoUpdater.UpdateDownloaded -= OnUpdateDownloaded;
        _autoUpdater.DownloadProgress -= OnDownloadProgress;
        _autoUpdater.Error -= OnAutoUpdaterError;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

