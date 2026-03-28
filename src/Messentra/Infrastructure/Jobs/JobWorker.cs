using Messentra.Features.Jobs;

namespace Messentra.Infrastructure.Jobs;

public sealed class JobWorker : BackgroundService
{
    private readonly IJobRunner _jobRunner;
    private readonly IBackgroundJobQueue _queue;
    private readonly ILogger<JobWorker> _logger;

    public JobWorker(
        IJobRunner jobRunner,
        IBackgroundJobQueue queue,
        ILogger<JobWorker> logger)
    {
        _jobRunner = jobRunner;
        _queue = queue;
        _logger = logger;
        _jobRunner = jobRunner;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var jobId = await _queue.Dequeue(stoppingToken);

            try
            {
                await _jobRunner.Run(jobId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected job worker failure for job {JobId}", jobId);
            }
        }
    }
}