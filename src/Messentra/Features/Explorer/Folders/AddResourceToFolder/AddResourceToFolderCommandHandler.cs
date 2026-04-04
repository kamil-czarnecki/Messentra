using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Explorer.Folders.AddResourceToFolder;

public sealed class AddResourceToFolderCommandHandler : ICommandHandler<AddResourceToFolderCommand>
{
    private readonly IDbContextFactory<MessentraDbContext> _contextFactory;

    public AddResourceToFolderCommandHandler(IDbContextFactory<MessentraDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async ValueTask<Unit> Handle(AddResourceToFolderCommand command, CancellationToken cancellationToken)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var exists = await dbContext.Set<FolderResource>()
            .AnyAsync(r => r.FolderId == command.FolderId && r.ResourceUrl == command.ResourceUrl, cancellationToken);

        if (exists)
            return Unit.Value;

        await dbContext.Set<FolderResource>()
            .AddAsync(new FolderResource { FolderId = command.FolderId, ResourceUrl = command.ResourceUrl }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
