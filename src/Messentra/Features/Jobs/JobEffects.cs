using Fluxor;
using Mediator;
using Messentra.Features.Jobs.GetJobs;

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
    public async Task HandleFetchJobs(FetchJobsAction action, IDispatcher dispatcher)
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
}