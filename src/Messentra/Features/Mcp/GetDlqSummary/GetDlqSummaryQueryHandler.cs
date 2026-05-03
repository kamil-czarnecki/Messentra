using Azure.Messaging.ServiceBus;
using Mediator;
using Messentra.Features.Explorer.Messages;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using SubQueue = Messentra.Features.Explorer.Messages.SubQueue;

namespace Messentra.Features.Mcp.GetDlqSummary;

public sealed class GetDlqSummaryQueryHandler(
    IAzureServiceBusQueueMessagesProvider queueMessagesProvider,
    IAzureServiceBusSubscriptionMessagesProvider subscriptionMessagesProvider)
    : IQueryHandler<GetDlqSummaryQuery, GetDlqSummaryQueryResult>
{
    private static readonly IReadOnlyList<string> DefaultGroupBy =
        ["deadLetterReason", "deadLetterErrorDescription", "label"];

    public async ValueTask<GetDlqSummaryQueryResult> Handle(GetDlqSummaryQuery query, CancellationToken ct)
    {
        var connectionInfo = query.Config.ToConnectionInfo();
        var options = new FetchMessagesOptions(
            FetchMode.Peek,
            FetchReceiveMode.PeekLock,
            query.SampleSize,
            StartSequence: query.FromSequenceNumber,
            TimeSpan.Zero,
            SubQueue.DeadLetter);

        try
        {
            var messages = (query.TopicName is null
                ? await queueMessagesProvider.Get(connectionInfo, query.ResourceName, options, ct)
                : await subscriptionMessagesProvider.Get(connectionInfo, query.TopicName, query.ResourceName, options, ct))
                .ToList();

            var nextSeq = messages.Count == query.SampleSize
                ? messages.Last().Message.BrokerProperties.SequenceNumber + 1
                : (long?)null;

            var effectiveGroupBy = query.GroupBy is { Count: > 0 } gb ? gb : DefaultGroupBy;

            var groups = messages
                .GroupBy(
                    m => effectiveGroupBy.Select(f => ExtractField(m.Message, f)).ToArray(),
                    StringArrayComparer.Instance)
                .OrderByDescending(g => g.Count())
                .Select(g => new DlqReasonGroup(
                    effectiveGroupBy.Zip(g.Key).ToDictionary(t => t.First, t => t.Second),
                    g.Count(),
                    g.First().Message.Body))
                .ToList();

            return new DlqSummaryResult(messages.Count, nextSeq, groups);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            return new McpError($"Resource '{query.ResourceName}' not found.");
        }
    }

    private static string? ExtractField(MessageDto message, string field) =>
        field.ToLowerInvariant() switch
        {
            "label" or "subject" => message.BrokerProperties.Label,
            "deadletterreason" => message.BrokerProperties.DeadLetterReason,
            "deadlettererrordescription" => message.BrokerProperties.DeadLetterErrorDescription,
            "correlationid" => message.BrokerProperties.CorrelationId,
            "messageid" => message.BrokerProperties.MessageId,
            "contenttype" => message.BrokerProperties.ContentType,
            "sessionid" => message.BrokerProperties.SessionId,
            "to" => message.BrokerProperties.To,
            "replyto" => message.BrokerProperties.ReplyTo,
            _ => message.ApplicationProperties.TryGetValue(field, out var v) ? v?.ToString() : null
        };

    private sealed class StringArrayComparer : IEqualityComparer<string?[]>
    {
        internal static readonly StringArrayComparer Instance = new();

        public bool Equals(string?[]? x, string?[]? y) =>
            x is not null && y is not null && x.SequenceEqual(y);

        public int GetHashCode(string?[] obj) =>
            obj.Aggregate(0, (h, s) => HashCode.Combine(h, s));
    }
}
