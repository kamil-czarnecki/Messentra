using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Mcp;

public sealed class McpHelpers(IDbContextFactory<MessentraDbContext> contextFactory)
{
    public async Task<Connection?> ResolveConnection(string name, CancellationToken ct)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync(ct);
        var connections = await ctx.Set<Connection>().AsNoTracking().ToListAsync(ct);
        
        return connections.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlySet<string>?> ResolveFolderResourceUrls(
        long connectionId, string folderName, CancellationToken ct)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync(ct);
        var folders = await ctx.Set<Folder>()
            .AsNoTracking()
            .Include(f => f.Resources)
            .Where(f => f.ConnectionId == connectionId)
            .ToListAsync(ct);
        var folder = folders.FirstOrDefault(f => f.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase));
        
        return folder?.Resources.Select(r => r.ResourceUrl).ToHashSet();
    }
}
