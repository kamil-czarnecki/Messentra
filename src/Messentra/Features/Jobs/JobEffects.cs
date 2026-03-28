using Fluxor;
using Mediator;
using Messentra.Features.Jobs.DeleteJob;
using Messentra.Features.Jobs.GetJobs;
using Messentra.Features.Jobs.PauseJob;
using Messentra.Features.Jobs.ResumeJob;

namespace Messentra.Features.Jobs;

public sealed class JobEffects
{
    private readonly IMediator _mediator;
    private readonly ILogger<JobEffects> _logger;

    public JobEffects(IMediator mediator, ILogger<JobEffects> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleFetchJobs(FetchJobsAction _, IDispatcher dispatcher)
    {
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
}