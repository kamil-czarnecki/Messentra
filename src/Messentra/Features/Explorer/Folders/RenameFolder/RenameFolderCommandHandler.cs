using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Explorer.Folders.RenameFolder;

public sealed class RenameFolderCommandHandler : ICommandHandler<RenameFolderCommand>
{
    private readonly MessentraDbContext _dbContext;

    public RenameFolderCommandHandler(MessentraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<Unit> Handle(RenameFolderCommand command, CancellationToken cancellationToken)
    {
        await _dbContext.Set<Folder>()
            .Where(f => f.Id == command.FolderId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.Name, command.NewName), cancellationToken);
        return Unit.Value;
    }
}