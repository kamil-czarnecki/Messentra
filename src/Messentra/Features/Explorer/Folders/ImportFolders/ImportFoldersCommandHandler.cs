using System.Text.Json;
using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Explorer.Folders.ImportFolders;

public sealed class ImportFoldersCommandHandler : ICommandHandler<ImportFoldersCommand>
{
    private readonly IDbContextFactory<MessentraDbContext> _contextFactory;

    public ImportFoldersCommandHandler(IDbContextFactory<MessentraDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async ValueTask<Unit> Handle(ImportFoldersCommand command, CancellationToken cancellationToken)
    {
        var items = JsonSerializer.Deserialize<List<FolderExportItem>>(command.JsonContent)
            ?? throw new InvalidOperationException("Invalid folder configuration file.");

        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var namespacePrefix = FolderResourceUrl.GetNamespacePrefix(command.ConnectionConfig);

        var existingFoldersByName = await dbContext.Set<Folder>()
            .Where(f => f.ConnectionId == command.ConnectionId)
            .ToDictionaryAsync(f => f.Name.ToLowerInvariant(), cancellationToken);

        foreach (var item in items)
        {
            var trimmedName = item.Name.Trim();
            var normalizedName = trimmedName.ToLowerInvariant();

            existingFoldersByName.TryGetValue(normalizedName, out var existing);

            if (existing is not null)
            {
                await dbContext.Set<FolderResource>()
                    .Where(r => r.FolderId == existing.Id)
                    .ExecuteDeleteAsync(cancellationToken);
                await dbContext.Set<Folder>()
                    .Where(f => f.Id == existing.Id)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            var folder = new Folder { ConnectionId = command.ConnectionId, Name = trimmedName };
            dbContext.Set<Folder>().Add(folder);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var resource in item.Resources)
            {
                dbContext.Set<FolderResource>().Add(
                    new FolderResource
                    {
                        FolderId = folder.Id,
                        ResourceUrl = FolderResourceUrl.ToAbsolute(resource, namespacePrefix)
                    });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
        return Unit.Value;
    }
}
