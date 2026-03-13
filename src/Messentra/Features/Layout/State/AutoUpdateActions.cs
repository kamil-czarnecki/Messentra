namespace Messentra.Features.Layout.State;

public sealed record RunAutoUpdaterAction;
public sealed record UpdateCurrentVersionAction(string Version);
public sealed record CheckForUpdatesAction;
public sealed record UpdateCheckingAction;
public sealed record UpdateAvailableAction(string Version);
public sealed record UpdateNotAvailableAction;
public sealed record DownloadUpdateAction;
public sealed record UpdateDownloadProgressAction(double Percent);
public sealed record UpdateReadyToInstallAction;
public sealed record InstallUpdateAction;
public sealed record AutoUpdateErrorAction(string Message);
public sealed record DismissUpdateErrorAction;

