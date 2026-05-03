using System.ComponentModel;
using Mediator;
using Messentra.Features.Mcp.ListConnections;
using ModelContextProtocol.Server;

namespace Messentra.Features.Mcp.Tools;

[McpServerToolType]
public sealed class ConnectionsMcpTool(IMediator mediator)
{
    [McpServerTool, Description(
        "Returns all saved connections with their namespace. " +
        "Call this first — all other tools require a connection name.")]
    public async Task<IEnumerable<ConnectionSummary>> ListConnections(CancellationToken ct)
        => await mediator.Send(new ListConnectionsQuery(), ct);
}
