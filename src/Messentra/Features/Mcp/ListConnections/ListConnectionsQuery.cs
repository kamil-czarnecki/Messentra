using Mediator;

namespace Messentra.Features.Mcp.ListConnections;

public sealed record ListConnectionsQuery : IQuery<IEnumerable<ConnectionSummary>>;
