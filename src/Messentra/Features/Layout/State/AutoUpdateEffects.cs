using Fluxor;
using Messentra.Infrastructure.AutoUpdater;

namespace Messentra.Features.Layout.State;

public sealed class AutoUpdateEffects
{
    private readonly IAutoUpdaterService _autoUpdater;

    public AutoUpdateEffects(IAutoUpdaterService autoUpdater)
    {
        _autoUpdater = autoUpdater;
    }

    [EffectMethod]
    public async Task HandleRunAutoUpdater(RunAutoUpdaterAction _, IDispatcher dispatcher)
    {
        var version = await _autoUpdater.GetCurrentVersion();
        
        dispatcher.Dispatch(new UpdateCurrentVersionAction(version));
        dispatcher.Dispatch(new CheckForUpdatesAction());
    }
    
    [EffectMethod]
    public async Task HandleCheckForUpdates(CheckForUpdatesAction _, IDispatcher dispatcher)
    {
        try
        {
            dispatcher.Dispatch(new UpdateCheckingAction());

            await _autoUpdater.CheckForUpdates();
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new AutoUpdateErrorAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleDownloadUpdate(DownloadUpdateAction _, IDispatcher dispatcher)
    {
        try
        {
            await _autoUpdater.DownloadUpdate();
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new AutoUpdateErrorAction(ex.Message));
        }
    }

    [EffectMethod]
    public Task HandleInstallUpdate(InstallUpdateAction _, IDispatcher dispatcher)
    {
        try
        {
            _autoUpdater.QuitAndInstall(false);
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new AutoUpdateErrorAction(ex.Message));
        }
        
        return Task.CompletedTask;
    }
}

