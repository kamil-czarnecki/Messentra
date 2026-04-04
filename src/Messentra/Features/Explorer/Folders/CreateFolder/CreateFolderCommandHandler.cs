using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Explorer.Folders.CreateFolder;

public sealed class CreateFolderCommandHandler : ICommandHandler<CreateFolderCommand, long>
{
    private readonly IDbContextFactory<MessentraDbContext> _contextFactory;

    public CreateFolderCommandHandler(IDbContextFactory<MessentraDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async ValueTask<long> Handle(CreateFolderCommand command, CancellationToken cancellationToken)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var folder = new Folder { ConnectionId = command.ConnectionId, Name = command.Name };
        await dbContext.Set<Folder>().AddAsync(folder, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return folder.Id;
    }
}