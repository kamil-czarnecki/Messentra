using System.ComponentModel;
using Mediator;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Mcp.GetDlqSummary;
using Messentra.Features.Mcp.GetResource;
using Messentra.Features.Mcp.ListResources;
using Messentra.Features.Mcp.PeekMessages;
using ModelContextProtocol.Server;

namespace Messentra.Features.Mcp.Tools;

[McpServerToolType]
public sealed class ResourcesMcpTool(IMediator mediator, IMcpHelpers mcpHelpers)
{
    [McpServerTool, Description(
        "Returns queues and subscriptions with message counts, status, and DLQ settings (MaxDeliveryCount, DeadLetteringOnMessageExpiration, ForwardDeadLetteredMessagesTo). " +
        "If folderName is omitted, returns all resources in the namespace without duplicates. " +
        "nameFilter is a case-insensitive substring match. hasDlq restricts to resources with dead-letter messages. " +
        "Results are served from a 5-minute cache — call GetResource to force a live fetch for a specific resource.")]
    public async Task<McpToolResult<List<ResourceSummary>>> ListResources(
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
        {
            var resources = await mediator.Send(
                new ListResourcesQuery(connection.Id, connection.ConnectionConfig, urlFilter, nameFilter, hasDlq),
                ct);
            return resources.ToList();
        }

        var urls = await mcpHelpers.ResolveFolderResourceUrls(connection.Id, folderName, ct);

        if (urls is null)
            return new McpError($"Folder '{folderName}' not found in connection '{connectionName}'.");

        urlFilter = urls;

        var filtered = await mediator.Send(
            new ListResourcesQuery(connection.Id, connection.ConnectionConfig, urlFilter, nameFilter, hasDlq), ct);
        return filtered.ToList();
    }

    [McpServerTool, Description(
        "Fetches live data for a single queue or subscription, bypasses the cache, and returns its full ResourceSummary including status and DLQ settings. " +
        "Use this when ListResources may be stale or when you need accurate current message counts. " +
        "For a subscription, provide both resourceName (subscription name) and topicName.")]
    public async Task<McpToolResult<ResourceSummary>> GetResource(
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

        return result.Match<McpToolResult<ResourceSummary>>(
            summary => summary,
            error => error);
    }

    [McpServerTool, Description(
        "Peeks messages from a queue or subscription without consuming them. " +
        "Supports active and dead-letter subqueues — each message includes DeadLetterReason and DeadLetterErrorDescription when peeking DLQ. " +
        "Returns messages and a nextSequenceNumber for pagination — pass it as fromSequenceNumber to fetch the next batch. " +
        "To understand why messages are failing at scale, prefer GetDlqSummary over repeated PeekMessages calls.")]
    public async Task<McpToolResult<PeekMessagesResult>> PeekMessages(
        [Description("Connection name (case-insensitive)")] string connectionName,
        [Description("Queue name, or subscription name when topicName is also provided")] string resourceName,
        [Description("Topic name — required when peeking a subscription")] string? topicName = null,
        [Description("Subqueue to peek from: 'active' or 'dlq'")] string subQueue = "dlq",
        [Description("Maximum number of messages to return (1–100)")] int maxMessages = 20,
        [Description("Sequence number to start from; omit to start from the beginning")] long? fromSequenceNumber = null,
        CancellationToken ct = default)
    {
        var connection = await mcpHelpers.ResolveConnection(connectionName, ct);

        if (connection is null)
            return new McpError($"Connection '{connectionName}' not found.");

        if (!subQueue.Equals("active", StringComparison.OrdinalIgnoreCase) &&
            !subQueue.Equals("dlq", StringComparison.OrdinalIgnoreCase))
            return new McpError($"Invalid subQueue '{subQueue}'. Accepted values are 'active' or 'dlq'.");

        var sq = subQueue.Equals("dlq", StringComparison.OrdinalIgnoreCase)
            ? SubQueue.DeadLetter
            : SubQueue.Active;
        var count = Math.Clamp(maxMessages, 1, 100);

        var result = await mediator.Send(
            new PeekMessagesQuery(connection.Id, connection.ConnectionConfig, resourceName, topicName, sq, count, fromSequenceNumber),
            ct);

        return result.Match<McpToolResult<PeekMessagesResult>>(
            r => r,
            error => error);
    }

    [McpServerTool, Description(
        "Peeks up to sampleSize messages from the DLQ of a queue or subscription and returns a grouped breakdown " +
        "ordered by frequency. Each group's key is returned as a dictionary of field name → value. " +
        "Use groupBy to control which fields form the group key — supported broker properties: " +
        "label/subject, deadLetterReason, deadLetterErrorDescription, correlationId, messageId, contentType, sessionId, to, replyTo. " +
        "Any other name is matched against application properties (e.g. PayloadTypeId, EventType). " +
        "Defaults to [deadLetterReason, deadLetterErrorDescription, label] when omitted. " +
        "SampledCount shows how many messages were inspected — the actual DLQ may contain more. " +
        "NextSequenceNumber is set when a full batch was returned; pass it as fromSequenceNumber to continue sampling. " +
        "For the total DLQ count, use GetResource or ListResources. For raw message content, use PeekMessages with subQueue='dlq'.")]
    public async Task<McpToolResult<DlqSummaryResult>> GetDlqSummary(
        [Description("Connection name (case-insensitive)")] string connectionName,
        [Description("Queue name, or subscription name when topicName is also provided")] string resourceName,
        [Description("Topic name — required when targeting a subscription")] string? topicName = null,
        [Description("Number of DLQ messages to sample (1–2000)")] int sampleSize = 500,
        [Description("Sequence number to continue from; omit to start from the beginning")] long? fromSequenceNumber = null,
        [Description("Fields to group by (broker property names or application property keys). Defaults to [deadLetterReason, deadLetterErrorDescription, label].")] string[]? groupBy = null,
        CancellationToken ct = default)
    {
        var connection = await mcpHelpers.ResolveConnection(connectionName, ct);

        if (connection is null)
            return new McpError($"Connection '{connectionName}' not found.");

        var count = Math.Clamp(sampleSize, 1, 2000);
        var result = await mediator.Send(
            new GetDlqSummaryQuery(connection.ConnectionConfig, resourceName, topicName, count, fromSequenceNumber, groupBy), ct);

        return result.Match<McpToolResult<DlqSummaryResult>>(
            r => r,
            error => error);
    }
}
