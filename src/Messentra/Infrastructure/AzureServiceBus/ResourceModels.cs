namespace Messentra.Infrastructure.AzureServiceBus;

public record ResourceOverview(
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    MessageInfo MessageInfo,
    SizeInfo SizeInfo);

public record MessageInfo(
    long Active,
    long DeadLetter,
    long Scheduled,
    long Transfer,
    long TransferDeadLetter,
    long Total);

public record SizeInfo(
    long CurrentSizeInBytes,
    long MaxSizeInMegabytes)
{
    public double FreeSpacePercentage => MaxSizeInMegabytes > 0
        ? (MaxSizeInMegabytes * 1024 * 1024 - CurrentSizeInBytes) / (double)(MaxSizeInMegabytes * 1024 * 1024) * 100
        : 0;
}

public record QueueProperties(
    TimeSpan DefaultMessageTimeToLive,
    TimeSpan LockDuration,
    TimeSpan AutoDeleteOnIdle,
    int MaxDeliveryCount,
    bool DeadLetteringOnMessageExpiration,
    string? ForwardDeadLetteredMessagesTo,
    string? ForwardTo,
    bool EnablePartitioning,
    bool RequiresDuplicateDetection,
    TimeSpan DuplicateDetectionWindow,
    bool RequiresSession,
    long? MaxMessageSizeInKilobytes,
    string UserMetadata);

public record TopicProperties(
    TimeSpan DefaultMessageTimeToLive,
    TimeSpan AutoDeleteOnIdle,
    bool EnablePartitioning,
    bool RequiresDuplicateDetection,
    TimeSpan DuplicateDetectionWindow,
    long? MaxMessageSizeInKilobytes,
    string UserMetadata);

public record SubscriptionProperties(
    TimeSpan DefaultMessageTimeToLive,
    TimeSpan LockDuration,
    TimeSpan AutoDeleteOnIdle,
    int MaxDeliveryCount,
    bool DeadLetteringOnMessageExpiration,
    string? ForwardDeadLetteredMessagesTo,
    string? ForwardTo,
    bool RequiresSession,
    string UserMetadata);

