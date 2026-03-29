using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.ResumeJob;

public sealed class ResumeJobCommandHandler : ICommandHandler<ResumeJobCommand, bool>
{
    private static JobStatus[] AllowedStatuses { get; } = [JobStatus.Paused, JobStatus.Failed];
    private readonly IDbContextFactory<MessentraDbContext> _dbContextFactory;
    private readonly IBackgroundJobQueue _backgroundJobQueue;

    public ResumeJobCommandHandler(IDbContextFactory<MessentraDbContext> dbContextFactory, IBackgroundJobQueue backgroundJobQueue)
    {
        _dbContextFactory = dbContextFactory;
        _backgroundJobQueue = backgroundJobQueue;
    }

    public async ValueTask<bool> Handle(ResumeJobCommand command, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await dbContext.Set<Job>()
            .SingleOrDefaultAsync(x => x.Id == command.JobId, cancellationToken);

        if (job is null || !AllowedStatuses.Contains(job.Status))
            return false;

        job.UpdateStatus(JobStatus.Queued);
        await dbContext.SaveChangesAsync(cancellationToken);

        await _backgroundJobQueue.Enqueue(job.Id, cancellationToken);

        return true;
    }
}

