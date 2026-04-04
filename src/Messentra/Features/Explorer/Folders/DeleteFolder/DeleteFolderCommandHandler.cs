using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Explorer.Folders.DeleteFolder;

public sealed class DeleteFolderCommandHandler : ICommandHandler<DeleteFolderCommand>
{
    private readonly MessentraDbContext _dbContext;

    public DeleteFolderCommandHandler(MessentraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<Unit> Handle(DeleteFolderCommand command, CancellationToken cancellationToken)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Set<FolderResource>()
            .Where(r => r.FolderId == command.FolderId)
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Set<Folder>()
            .Where(f => f.Id == command.FolderId)
            .ExecuteDeleteAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return Unit.Value;
    }
}
