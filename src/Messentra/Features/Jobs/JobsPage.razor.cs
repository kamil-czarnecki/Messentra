using ElectronNET.API;
using Fluxor;
using Messentra.Domain;
using Messentra.Infrastructure;

namespace Messentra.Features.Jobs;

public partial class JobsPage
{
    private readonly IState<JobState> _jobsState;
    private readonly IDispatcher _dispatcher;
    private readonly IFileSystem _fileSystem;

    public JobsPage(IState<JobState> jobsState, IDispatcher dispatcher, IFileSystem fileSystem)
    {
        _jobsState = jobsState;
        _dispatcher = dispatcher;
        _fileSystem = fileSystem;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender && !_jobsState.Value.IsLoading && !_jobsState.Value.IsLoaded)
        {
            _dispatcher.Dispatch(new FetchJobsAction());
        }
    }

    private void Pause(long jobId)
    {
        _dispatcher.Dispatch(new PauseJobAction(jobId));
    }

    private void Resume(long jobId)
    {
        _dispatcher.Dispatch(new ResumeJobAction(jobId));
    }

    private void Delete(long jobId)
    {
        _dispatcher.Dispatch(new DeleteJobAction(jobId));
    }

    private async Task DownloadAsync(string sourcePath)
    {
        if (!_fileSystem.FileExists(sourcePath))
            return;

        var window = Electron.WindowManager.BrowserWindows.FirstOrDefault();
        if (window is null)
            return;

        var destinationPath = await Electron.Dialog.ShowSaveDialogAsync(window, new SaveDialogOptions
        {
            Title = "Save exported file",
            DefaultPath = Path.GetFileName(sourcePath)
        });

        if (string.IsNullOrWhiteSpace(destinationPath))
            return;

        await using var source = _fileSystem.OpenRead(sourcePath);
        await using var destination = _fileSystem.OpenWrite(destinationPath, useAsync: true);
        await source.CopyToAsync(destination);
        await destination.FlushAsync();
    }

    private static MudBlazor.Color GetStatusColor(JobStatus status) =>
        status switch
        {
            JobStatus.Queued => MudBlazor.Color.Info,
            JobStatus.Running => MudBlazor.Color.Primary,
            JobStatus.Completed => MudBlazor.Color.Success,
            JobStatus.Failed => MudBlazor.Color.Error,
            JobStatus.Paused => MudBlazor.Color.Warning,
            _ => MudBlazor.Color.Default
        };
}