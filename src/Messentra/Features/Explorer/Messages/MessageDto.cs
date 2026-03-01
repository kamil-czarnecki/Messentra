namespace Messentra.Features.Explorer.Messages;

public sealed record MessageDto(
    string Body,
    BrokerProperties BrokerProperties,
    IReadOnlyDictionary<string, object> ApplicationProperties);

public sealed record BrokerProperties(
    string? MessageId,
    long SequenceNumber,
    string? CorrelationId,
    string? SessionId,
    string? ReplyToSessionId,
    DateTime EnqueuedTimeUtc,
    DateTime ScheduledEnqueueTimeUtc,
    TimeSpan TimeToLive,
    DateTime LockedUntilUtc,
    DateTime ExpiresAtUtc,
    int DeliveryCount,
    string? Label,
    string? To,
    string? ReplyTo,
    string? PartitionKey,
    string? ContentType,
    string? DeadLetterReason,
    string? DeadLetterErrorDescription);