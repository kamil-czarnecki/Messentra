using System.ComponentModel;
using Mediator;
using Messentra.Features.Mcp.ListFolders;
using ModelContextProtocol.Server;

namespace Messentra.Features.Mcp.Tools;

[McpServerToolType]
public sealed class FoldersMcpTool(IMediator mediator, IMcpHelpers mcpHelpers)
{
    [McpServerTool, Description(
        "Returns all user-defined folders for a connection. " +
        "Folders are organisational groups of resources — use them to scope ListResources to a subset of the namespace.")]
    public async Task<McpToolResult<List<FolderSummary>>> ListFolders(
        [Description("Connection name (case-insensitive)")] string connectionName,
        CancellationToken ct)
    {
        var connection = await mcpHelpers.ResolveConnection(connectionName, ct);

        if (connection is null)
            return new McpError($"Connection '{connectionName}' not found.");

        return (await mediator.Send(new ListFoldersQuery(connection.Id), ct)).ToList();
    }
}
