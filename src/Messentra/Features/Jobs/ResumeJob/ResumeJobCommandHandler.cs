using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.ResumeJob;

public sealed class ResumeJobCommandHandler : ICommandHandler<ResumeJobCommand, bool>
{
    private readonly MessentraDbContext _dbContext;
    private readonly IBackgroundJobQueue _backgroundJobQueue;

    public ResumeJobCommandHandler(MessentraDbContext dbContext, IBackgroundJobQueue backgroundJobQueue)
    {
        _dbContext = dbContext;
        _backgroundJobQueue = backgroundJobQueue;
    }

    public async ValueTask<bool> Handle(ResumeJobCommand command, CancellationToken cancellationToken)
    {
        var job = await _dbContext.Set<Job>()
            .SingleOrDefaultAsync(x => x.Id == command.JobId, cancellationToken);

        if (job is null || job.Status != JobStatus.Paused)
            return false;

        job.UpdateStatus(JobStatus.Queued);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _backgroundJobQueue.Enqueue(job.Id, cancellationToken);

        return true;
    }
}

