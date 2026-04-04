using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Explorer.Folders.RemoveResourceFromFolder;

public sealed class RemoveResourceFromFolderCommandHandler : ICommandHandler<RemoveResourceFromFolderCommand>
{
    private readonly MessentraDbContext _dbContext;

    public RemoveResourceFromFolderCommandHandler(MessentraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<Unit> Handle(RemoveResourceFromFolderCommand command, CancellationToken cancellationToken)
    {
        await _dbContext.Set<FolderResource>()
            .Where(r => r.FolderId == command.FolderId && r.ResourceUrl == command.ResourceUrl)
            .ExecuteDeleteAsync(cancellationToken);
        return Unit.Value;
    }
}
