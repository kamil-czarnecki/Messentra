using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Mcp;

public sealed class McpHelpers(IDbContextFactory<MessentraDbContext> contextFactory) : IMcpHelpers
{
    public async Task<Connection?> ResolveConnection(string name, CancellationToken ct)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync(ct);
        return await ctx
            .Set<Connection>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name, ct);
    }

    public async Task<IReadOnlySet<string>?> ResolveFolderResourceUrls(
        long connectionId, string folderName, CancellationToken ct)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync(ct);
        var folder = await ctx
            .Set<Folder>()
            .AsNoTracking()
            .Include(f => f.Resources)
            .FirstOrDefaultAsync(
                f => f.ConnectionId == connectionId && EF.Functions.Collate(f.Name, "NOCASE") == folderName,
                ct);

        return folder?.Resources.Select(r => r.ResourceUrl).ToHashSet();
    }
}
