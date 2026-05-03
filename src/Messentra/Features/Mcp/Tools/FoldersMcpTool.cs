using System.ComponentModel;
using Mediator;
using Messentra.Features.Mcp.ListFolders;
using ModelContextProtocol.Server;

namespace Messentra.Features.Mcp.Tools;

[McpServerToolType]
public sealed class FoldersMcpTool(IMediator mediator, McpHelpers mcpHelpers)
{
    [McpServerTool, Description("Returns all folders for a connection.")]
    public async Task<object> ListFolders(
        [Description("Connection name (case-insensitive)")] string connectionName,
        CancellationToken ct)
    {
        var connection = await mcpHelpers.ResolveConnection(connectionName, ct);
        
        if (connection is null)
            return new McpError($"Connection '{connectionName}' not found.");

        return await mediator.Send(new ListFoldersQuery(connection.Id), ct);
    }
}
