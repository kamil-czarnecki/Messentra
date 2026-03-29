using Mediator;
using Messentra.Domain;
using Messentra.Features.Jobs.Stages.ImportMessages;
using Messentra.Features.Jobs.Stages.FetchMessages;
using Messentra.Infrastructure;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.DeleteJob;

public sealed class DeleteJobCommandHandler : ICommandHandler<DeleteJobCommand, bool>
{
    private readonly IDbContextFactory<MessentraDbContext> _dbContextFactory;
    private readonly IFileSystem _fileSystem;

    public DeleteJobCommandHandler(IDbContextFactory<MessentraDbContext> dbContextFactory, IFileSystem fileSystem)
    {
        _dbContextFactory = dbContextFactory;
        _fileSystem = fileSystem;
    }

    public async ValueTask<bool> Handle(DeleteJobCommand command, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var job = await dbContext.Set<Job>()
            .SingleOrDefaultAsync(x => x.Id == command.JobId, cancellationToken);

        if (job is null || job.Status == JobStatus.Running)
            return false;

        await dbContext.Set<FetchedMessagesBatch>()
            .Where(x => x.JobId == command.JobId)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Set<ImportedMessage>()
            .Where(x => x.JobId == command.JobId)
            .ExecuteDeleteAsync(cancellationToken);
        
        dbContext.Set<Job>().Remove(job);

        await dbContext.SaveChangesAsync(cancellationToken);

        var jobFolderPath = Path.Combine(_fileSystem.GetRootPath(), "Jobs", command.JobId.ToString());
        if (_fileSystem.DirectoryExists(jobFolderPath))
        {
            _fileSystem.DeleteDirectory(jobFolderPath, recursive: true);
        }

        return true;
    }
}

