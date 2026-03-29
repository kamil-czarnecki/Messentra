using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.PauseJob;

public sealed class PauseJobCommandHandler : ICommandHandler<PauseJobCommand, bool>
{
    private readonly IDbContextFactory<MessentraDbContext> _dbContextFactory;
    private readonly IJobCancellationRegistry _jobCancellationRegistry;

    public PauseJobCommandHandler(IDbContextFactory<MessentraDbContext> dbContextFactory, IJobCancellationRegistry jobCancellationRegistry)
    {
        _dbContextFactory = dbContextFactory;
        _jobCancellationRegistry = jobCancellationRegistry;
    }

    public async ValueTask<bool> Handle(PauseJobCommand command, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var status = await dbContext.Set<Job>()
            .Where(x => x.Id == command.JobId)
            .Select(x => x.Status)
            .SingleOrDefaultAsync(cancellationToken);

        if (status != JobStatus.Running)
            return false;

        _jobCancellationRegistry.RequestPause(command.JobId);
        return true;
    }
}

