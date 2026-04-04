using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Explorer.Folders.RemoveResourceFromFolder;

public sealed class RemoveResourceFromFolderCommandHandler : ICommandHandler<RemoveResourceFromFolderCommand>
{
    private readonly IDbContextFactory<MessentraDbContext> _contextFactory;

    public RemoveResourceFromFolderCommandHandler(IDbContextFactory<MessentraDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async ValueTask<Unit> Handle(RemoveResourceFromFolderCommand command, CancellationToken cancellationToken)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Set<FolderResource>()
            .Where(r => r.FolderId == command.FolderId && r.ResourceUrl == command.ResourceUrl)
            .ExecuteDeleteAsync(cancellationToken);
        return Unit.Value;
    }
}
