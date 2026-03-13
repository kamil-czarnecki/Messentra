using Fluxor;

namespace Messentra.Features.Layout.State;

public static class AutoUpdateReducers
{
    [ReducerMethod]
    public static AutoUpdateState Reduce(AutoUpdateState state, UpdateCurrentVersionAction action) =>
        state with { CurrentVersion = action.Version };
    
    [ReducerMethod]
    public static AutoUpdateState Reduce(AutoUpdateState state, UpdateCheckingAction _) =>
        state with { IsChecking = true, ErrorMessage = null };

    [ReducerMethod]
    public static AutoUpdateState Reduce(AutoUpdateState state, UpdateAvailableAction action) =>
        state with { IsChecking = false, IsUpdateAvailable = true, AvailableVersion = action.Version };

    [ReducerMethod]
    public static AutoUpdateState Reduce(AutoUpdateState state, UpdateNotAvailableAction _) =>
        state with { IsChecking = false, IsUpdateAvailable = false };

    [ReducerMethod]
    public static AutoUpdateState Reduce(AutoUpdateState state, DownloadUpdateAction _) =>
        state with { IsDownloading = true, DownloadProgress = 0 };

    [ReducerMethod]
    public static AutoUpdateState Reduce(AutoUpdateState state, UpdateDownloadProgressAction action) =>
        state with { DownloadProgress = action.Percent };

    [ReducerMethod]
    public static AutoUpdateState Reduce(AutoUpdateState state, UpdateReadyToInstallAction _) =>
        state with { IsDownloading = false, IsReadyToInstall = true, DownloadProgress = 100 };

    [ReducerMethod]
    public static AutoUpdateState Reduce(AutoUpdateState state, AutoUpdateErrorAction action) =>
        state with { IsChecking = false, IsDownloading = false, ErrorMessage = action.Message };

    [ReducerMethod(typeof(DismissUpdateErrorAction))]
    public static AutoUpdateState Reduce(AutoUpdateState state) =>
        state with { ErrorMessage = null };
}

