using System.ComponentModel;
using Mediator;
using Messentra.Features.Mcp.GetResource;
using Messentra.Features.Mcp.ListResources;
using ModelContextProtocol.Server;

namespace Messentra.Features.Mcp.Tools;

[McpServerToolType]
public sealed class ResourcesMcpTool(IMediator mediator, McpHelpers mcpHelpers)
{
    [McpServerTool, Description(
        "Returns queues and subscriptions with message counts. " +
        "If folderName is omitted, returns all resources in the namespace without duplicates. " +
        "nameFilter is a case-insensitive substring match. hasDlq restricts to resources with dead-letter messages.")]
    public async Task<object> ListResources(
        [Description("Connection name (case-insensitive)")] string connectionName,
        [Description("Optional folder name (case-insensitive)")] string? folderName = null,
        [Description("Optional substring filter on resource name (case-insensitive)")] string? nameFilter = null,
        [Description("When true, only return resources with dead-letter messages")] bool hasDlq = false,
        CancellationToken ct = default)
    {
        var connection = await mcpHelpers.ResolveConnection(connectionName, ct);
        if (connection is null)
            return new McpError($"Connection '{connectionName}' not found.");

        IReadOnlySet<string>? urlFilter = null;
        
        if (folderName is null)
            return await mediator.Send(
                new ListResourcesQuery(connection.Id, connection.ConnectionConfig, urlFilter, nameFilter, hasDlq),
                ct);
        
        var urls = await mcpHelpers.ResolveFolderResourceUrls(connection.Id, folderName, ct);
            
        if (urls is null)
            return new McpError($"Folder '{folderName}' not found in connection '{connectionName}'.");
            
        urlFilter = urls;

        return await mediator.Send(
            new ListResourcesQuery(connection.Id, connection.ConnectionConfig, urlFilter, nameFilter, hasDlq), ct);
    }

    [McpServerTool, Description(
        "Fetches live data for a single queue or subscription, updates the cache, and returns its ResourceSummary. " +
        "For a subscription, provide both resourceName (subscription name) and topicName.")]
    public async Task<object> GetResource(
        [Description("Connection name (case-insensitive)")] string connectionName,
        [Description("Queue name, or subscription name when topicName is also provided")] string resourceName,
        [Description("Topic name — required when fetching a subscription")] string? topicName = null,
        CancellationToken ct = default)
    {
        var connection = await mcpHelpers.ResolveConnection(connectionName, ct);
        
        if (connection is null)
            return new McpError($"Connection '{connectionName}' not found.");

        var result = await mediator.Send(
            new GetResourceQuery(connection.Id, connection.ConnectionConfig, resourceName, topicName),
            ct);
        
        return result.Value;
    }
}
