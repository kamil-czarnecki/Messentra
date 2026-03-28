using Mediator;
using Messentra.Domain;
using Messentra.Features.Jobs.Stages.FetchMessages;
using Messentra.Infrastructure;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.DeleteJob;

public sealed class DeleteJobCommandHandler : ICommandHandler<DeleteJobCommand, bool>
{
    private readonly MessentraDbContext _dbContext;
    private readonly IFileSystem _fileSystem;

    public DeleteJobCommandHandler(MessentraDbContext dbContext, IFileSystem fileSystem)
    {
        _dbContext = dbContext;
        _fileSystem = fileSystem;
    }

    public async ValueTask<bool> Handle(DeleteJobCommand command, CancellationToken cancellationToken)
    {
        var job = await _dbContext.Set<Job>()
            .SingleOrDefaultAsync(x => x.Id == command.JobId, cancellationToken);

        if (job is null || job.Status == JobStatus.Running)
            return false;

        await _dbContext.Set<FetchedMessagesBatch>()
            .Where(x => x.JobId == command.JobId)
            .ExecuteDeleteAsync(cancellationToken);
        
        _dbContext.Set<Job>().Remove(job);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var jobFolderPath = Path.Combine(_fileSystem.GetRootPath(), "Jobs", command.JobId.ToString());
        if (_fileSystem.DirectoryExists(jobFolderPath))
        {
            _fileSystem.DeleteDirectory(jobFolderPath, recursive: true);
        }

        return true;
    }
}

