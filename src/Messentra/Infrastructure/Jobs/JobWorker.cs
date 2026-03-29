using Messentra.Features.Jobs;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Infrastructure.Jobs;

public sealed class JobWorker : BackgroundService
{
    private readonly IJobRunner _jobRunner;
    private readonly IBackgroundJobQueue _queue;
    private readonly ILogger<JobWorker> _logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory;

    public JobWorker(
        IJobRunner jobRunner,
        IBackgroundJobQueue queue,
        ILogger<JobWorker> logger,
        IServiceScopeFactory? serviceScopeFactory = null)
    {
        _jobRunner = jobRunner;
        _queue = queue;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnqueueRunningAndQueuedJobs(stoppingToken);

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

    private async Task EnqueueRunningAndQueuedJobs(CancellationToken cancellationToken)
    {
        if (_serviceScopeFactory is null)
            return;

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<MessentraDbContext>();

        if (dbContext is null)
            return;

        var queuedJobs = await dbContext.Set<Domain.Job>()
            .Where(x => x.Status == Domain.JobStatus.Running || x.Status == Domain.JobStatus.Queued)
            .ToListAsync(cancellationToken);

        if (queuedJobs.Count == 0)
            return;

        foreach (var job in queuedJobs)
        {
            await _queue.Enqueue(job.Id, cancellationToken);
        }
    }
}