using System.Text.Json;
using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Explorer.Folders.ExportFolders;

public sealed class ExportFoldersCommandHandler : ICommandHandler<ExportFoldersCommand>
{
    private readonly IDbContextFactory<MessentraDbContext> _contextFactory;
    private readonly IFileSystem _fileSystem;

    public ExportFoldersCommandHandler(IDbContextFactory<MessentraDbContext> contextFactory, IFileSystem fileSystem)
    {
        _contextFactory = contextFactory;
        _fileSystem = fileSystem;
    }

    public async ValueTask<Unit> Handle(ExportFoldersCommand command, CancellationToken cancellationToken)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var namespacePrefix = FolderResourceUrl.GetNamespacePrefix(command.ConnectionConfig);

        var folders = await dbContext.Set<Folder>()
            .Where(f => f.ConnectionId == command.ConnectionId)
            .Select(f => new { f.Name, ResourceUrls = f.Resources.Select(r => r.ResourceUrl) })
            .ToListAsync(cancellationToken);

        var items = folders
            .Select(f => new FolderExportItem(
                f.Name,
                f.ResourceUrls
                    .Select(url => FolderResourceUrl.ToRelative(url, namespacePrefix))
                    .ToList()))
            .ToList();

        var json = JsonSerializer.Serialize(items, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        await using var stream = _fileSystem.OpenWrite(command.DestinationPath);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(json);

        return Unit.Value;
    }
}
