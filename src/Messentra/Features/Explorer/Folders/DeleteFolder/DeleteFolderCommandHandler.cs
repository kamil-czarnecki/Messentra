using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Explorer.Folders.DeleteFolder;

public sealed class DeleteFolderCommandHandler : ICommandHandler<DeleteFolderCommand>
{
    private readonly IDbContextFactory<MessentraDbContext> _contextFactory;

    public DeleteFolderCommandHandler(IDbContextFactory<MessentraDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async ValueTask<Unit> Handle(DeleteFolderCommand command, CancellationToken cancellationToken)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Set<FolderResource>()
            .Where(r => r.FolderId == command.FolderId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<Folder>()
            .Where(f => f.Id == command.FolderId)
            .ExecuteDeleteAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return Unit.Value;
    }
}
