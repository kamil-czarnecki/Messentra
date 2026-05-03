using Messentra.Features.Explorer.Messages;

namespace Messentra.Features.Mcp.PeekMessages;

public sealed record PeekedMessage(
    string? MessageId,
    long SequenceNumber,
    string? Subject,
    string? ContentType,
    string? CorrelationId,
    DateTime EnqueuedAtUtc,
    DateTime ExpiresAtUtc,
    int DeliveryCount,
    string? DeadLetterReason,
    string? DeadLetterErrorDescription,
    string Body,
    IReadOnlyDictionary<string, string> ApplicationProperties)
{
    internal static PeekedMessage From(ServiceBusMessage message)
    {
        var broker = message.Message.BrokerProperties;
        var body = message.Message.Body;

        return new PeekedMessage(
            broker.MessageId,
            broker.SequenceNumber,
            broker.Label,
            broker.ContentType,
            broker.CorrelationId,
            broker.EnqueuedTimeUtc,
            broker.ExpiresAtUtc,
            broker.DeliveryCount,
            broker.DeadLetterReason,
            broker.DeadLetterErrorDescription,
            body,
            message.Message.ApplicationProperties
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString() ?? string.Empty));
    }
}
