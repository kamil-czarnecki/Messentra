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

        var folderName = command.Name.Trim();
        var normalizedFolderName = folderName.ToLowerInvariant();
        var exists = await dbContext.Set<Folder>()
            .AnyAsync(f => f.ConnectionId == command.ConnectionId && f.Name.ToLower() == normalizedFolderName, cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Folder '{folderName}' already exists.");

        var folder = new Folder { ConnectionId = command.ConnectionId, Name = folderName };
        await dbContext.Set<Folder>().AddAsync(folder, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return folder.Id;
    }
}