using Mediator;
using Messentra.Domain;

namespace Messentra.Features.Mcp.ListResources;

public sealed record ListResourcesQuery(
    long ConnectionId,
    ConnectionConfig Config,
    IReadOnlySet<string>? ResourceUrlFilter,
    string? NameFilter,
    bool HasDlq) : IQuery<IEnumerable<ResourceSummary>>;
