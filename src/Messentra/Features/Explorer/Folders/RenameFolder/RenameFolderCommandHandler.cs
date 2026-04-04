using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Explorer.Folders.RenameFolder;

public sealed class RenameFolderCommandHandler : ICommandHandler<RenameFolderCommand>
{
    private readonly IDbContextFactory<MessentraDbContext> _contextFactory;

    public RenameFolderCommandHandler(IDbContextFactory<MessentraDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async ValueTask<Unit> Handle(RenameFolderCommand command, CancellationToken cancellationToken)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Set<Folder>()
            .Where(f => f.Id == command.FolderId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.Name, command.NewName), cancellationToken);
        return Unit.Value;
    }
}