using Fluxor;

namespace Messentra.Features.Layout.State;

[FeatureState]
public sealed record AutoUpdateState(
    string? CurrentVersion,
    bool IsChecking,
    bool IsUpdateAvailable,
    bool IsDownloading,
    bool IsReadyToInstall,
    double DownloadProgress,
    string? AvailableVersion,
    string? ErrorMessage)
{
    private AutoUpdateState() : this(
        CurrentVersion: null,
        IsChecking: false,
        IsUpdateAvailable: false,
        IsDownloading: false,
        IsReadyToInstall: false,
        DownloadProgress: 0,
        AvailableVersion: null,
        ErrorMessage: null)
    {
    }
}

