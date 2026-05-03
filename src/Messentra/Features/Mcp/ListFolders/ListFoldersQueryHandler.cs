using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Mcp.ListFolders;

public sealed class ListFoldersQueryHandler(IDbContextFactory<MessentraDbContext> contextFactory)
    : IQueryHandler<ListFoldersQuery, IEnumerable<FolderSummary>>
{
    public async ValueTask<IEnumerable<FolderSummary>> Handle(ListFoldersQuery query, CancellationToken ct)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync(ct);
        var folders = await ctx.Set<Folder>()
            .AsNoTracking()
            .Where(f => f.ConnectionId == query.ConnectionId)
            .Select(f => new FolderSummary(f.Name))
            .ToListAsync(ct);
        return folders;
    }
}
