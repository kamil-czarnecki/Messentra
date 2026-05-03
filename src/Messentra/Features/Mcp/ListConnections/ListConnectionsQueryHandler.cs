using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Mcp.ListConnections;

public sealed class ListConnectionsQueryHandler(IDbContextFactory<MessentraDbContext> contextFactory)
    : IQueryHandler<ListConnectionsQuery, IEnumerable<ConnectionSummary>>
{
    public async ValueTask<IEnumerable<ConnectionSummary>> Handle(ListConnectionsQuery query, CancellationToken ct)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync(ct);
        
        var connections = await ctx.Set<Connection>().AsNoTracking().ToListAsync(ct);
        
        return connections.Select(c => new ConnectionSummary(c.Name, GetNamespace(c.ConnectionConfig)));
    }

    private static string GetNamespace(ConnectionConfig config) => config.ConnectionType switch
    {
        ConnectionType.ConnectionString => ParseNamespaceFromConnectionString(config.ConnectionStringConfig!.ConnectionString),
        ConnectionType.EntraId => config.EntraIdConfig!.Namespace,
        ConnectionType.Corrupted => "(corrupted)",
        _ => throw new ArgumentOutOfRangeException(nameof(config.ConnectionType), config.ConnectionType, "Unsupported ConnectionType")
    };

    private static string ParseNamespaceFromConnectionString(string connectionString)
    {
        const string prefix = "Endpoint=sb://";
        var start = connectionString.IndexOf(prefix, StringComparison.Ordinal);
        if (start < 0) return "(corrupted)";
        start += prefix.Length;
        var slashEnd = connectionString.IndexOf('/', start);
        var semicolonEnd = connectionString.IndexOf(';', start);
        var end = (slashEnd, semicolonEnd) switch
        {
            (< 0, < 0) => -1,
            (< 0, _) => semicolonEnd,
            (_, < 0) => slashEnd,
            _ => Math.Min(slashEnd, semicolonEnd)
        };
        return end < 0
            ? connectionString[start..]
            : connectionString[start..end];
    }
}
