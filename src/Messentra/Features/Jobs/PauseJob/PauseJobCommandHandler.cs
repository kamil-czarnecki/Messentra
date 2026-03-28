using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.PauseJob;

public sealed class PauseJobCommandHandler : ICommandHandler<PauseJobCommand, bool>
{
    private readonly MessentraDbContext _dbContext;
    private readonly IJobCancellationRegistry _jobCancellationRegistry;

    public PauseJobCommandHandler(MessentraDbContext dbContext, IJobCancellationRegistry jobCancellationRegistry)
    {
        _dbContext = dbContext;
        _jobCancellationRegistry = jobCancellationRegistry;
    }

    public async ValueTask<bool> Handle(PauseJobCommand command, CancellationToken cancellationToken)
    {
        var status = await _dbContext.Set<Job>()
            .Where(x => x.Id == command.JobId)
            .Select(x => x.Status)
            .SingleOrDefaultAsync(cancellationToken);

        if (status != JobStatus.Running)
            return false;

        _jobCancellationRegistry.RequestPause(command.JobId);
        return true;
    }
}

