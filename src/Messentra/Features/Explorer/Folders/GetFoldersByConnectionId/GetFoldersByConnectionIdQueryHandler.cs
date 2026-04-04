using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Explorer.Folders.GetFoldersByConnectionId;

public sealed class GetFoldersByConnectionIdQueryHandler
    : IQueryHandler<GetFoldersByConnectionIdQuery, IReadOnlyCollection<FolderDto>>
{
    private readonly IDbContextFactory<MessentraDbContext> _contextFactory;

    public GetFoldersByConnectionIdQueryHandler(IDbContextFactory<MessentraDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async ValueTask<IReadOnlyCollection<FolderDto>> Handle(
        GetFoldersByConnectionIdQuery query, CancellationToken cancellationToken)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await dbContext.Set<Folder>()
            .Where(f => f.ConnectionId == query.ConnectionId)
            .Select(f => new { f.Id, f.Name, ResourceUrls = f.Resources.Select(r => r.ResourceUrl) })
            .ToListAsync(cancellationToken);

        return rows
            .Select(f => new FolderDto(f.Id, f.Name, f.ResourceUrls.ToHashSet()))
            .ToList();
    }
}