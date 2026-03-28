using Fluxor;
using Mediator;
using Messentra.Features.Jobs.DeleteJob;
using Messentra.Features.Jobs.GetJobs;
using Messentra.Features.Jobs.PauseJob;
using Messentra.Features.Jobs.ResumeJob;

namespace Messentra.Features.Jobs;

public sealed class JobEffects : IDisposable
{
    private readonly IMediator _mediator;
    private readonly IJobProgressNotifier _jobProgressNotifier;
    private readonly ILogger<JobEffects> _logger;
    private readonly Lock _lock = new();
    private IDisposable? _subscription;

    public JobEffects(IMediator mediator, IJobProgressNotifier jobProgressNotifier, ILogger<JobEffects> logger)
    {
        _mediator = mediator;
        _jobProgressNotifier = jobProgressNotifier;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleFetchJobs(FetchJobsAction action, IDispatcher dispatcher)
    {
        EnsureProgressSubscription(dispatcher);

        try
        {
            var jobs = await _mediator.Send(new GetJobsQuery(), CancellationToken.None);
            dispatcher.Dispatch(new FetchJobsSuccessAction(jobs));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch jobs.");
            dispatcher.Dispatch(new FetchJobsFailureAction());
        }
    }

    private void EnsureProgressSubscription(IDispatcher dispatcher)
    {
        lock (_lock)
        {
            if (_subscription is not null)
                return;

            _subscription = _jobProgressNotifier.Subscribe(update =>
            {
                dispatcher.Dispatch(new JobProgressReceivedAction(update));
            });
        }
    }

    [EffectMethod]
    public async Task HandlePauseJob(PauseJobAction action, IDispatcher dispatcher)
    {
        try
        {
            var paused = await _mediator.Send(new PauseJobCommand(action.JobId), CancellationToken.None);
            dispatcher.Dispatch(paused
                ? new PauseJobSuccessAction(action.JobId)
                : new PauseJobFailureAction(action.JobId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause job {JobId}.", action.JobId);
            dispatcher.Dispatch(new PauseJobFailureAction(action.JobId));
        }
    }

    [EffectMethod]
    public async Task HandleResumeJob(ResumeJobAction action, IDispatcher dispatcher)
    {
        try
        {
            var resumed = await _mediator.Send(new ResumeJobCommand(action.JobId), CancellationToken.None);
            dispatcher.Dispatch(resumed
                ? new ResumeJobSuccessAction(action.JobId)
                : new ResumeJobFailureAction(action.JobId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume job {JobId}.", action.JobId);
            dispatcher.Dispatch(new ResumeJobFailureAction(action.JobId));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteJob(DeleteJobAction action, IDispatcher dispatcher)
    {
        try
        {
            var deleted = await _mediator.Send(new DeleteJobCommand(action.JobId), CancellationToken.None);
            dispatcher.Dispatch(deleted
                ? new DeleteJobSuccessAction(action.JobId)
                : new DeleteJobFailureAction(action.JobId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete job {JobId}.", action.JobId);
            dispatcher.Dispatch(new DeleteJobFailureAction(action.JobId));
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}