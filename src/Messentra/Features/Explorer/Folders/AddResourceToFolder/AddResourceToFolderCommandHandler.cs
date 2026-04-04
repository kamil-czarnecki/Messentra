using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Explorer.Folders.AddResourceToFolder;

public sealed class AddResourceToFolderCommandHandler : ICommandHandler<AddResourceToFolderCommand>
{
    private readonly MessentraDbContext _dbContext;

    public AddResourceToFolderCommandHandler(MessentraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<Unit> Handle(AddResourceToFolderCommand command, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Set<FolderResource>()
            .AnyAsync(r => r.FolderId == command.FolderId && r.ResourceUrl == command.ResourceUrl, cancellationToken);

        if (!exists)
        {
            await _dbContext.Set<FolderResource>()
                .AddAsync(new FolderResource { FolderId = command.FolderId, ResourceUrl = command.ResourceUrl }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
