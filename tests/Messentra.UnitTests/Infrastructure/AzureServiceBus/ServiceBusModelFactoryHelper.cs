using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus;

/// <summary>
/// Wraps <see cref="ServiceBusModelFactory"/> with valid required defaults so callers only need
/// to supply the values relevant to their test.
/// </summary>
internal static class ServiceBusModelFactoryHelper
{
    // Azure SDK validation rules:
    //  QueueProperties / SubscriptionProperties:
    //    lockDuration              > 00:00:00
    //    defaultMessageTimeToLive  > 00:00:00
    //    autoDeleteOnIdle         >= 00:05:00
    //    duplicateDetectionHistoryTimeWindow >= 00:00:20  (Queue only)
    //  TopicProperties:
    //    defaultMessageTimeToLive  > 00:00:00
    //    autoDeleteOnIdle         >= 00:05:00
    //    duplicateDetectionHistoryTimeWindow > 00:00:00

    private static readonly TimeSpan DefaultLockDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultMessageTimeToLive = TimeSpan.FromDays(1);
    private static readonly TimeSpan DefaultAutoDeleteOnIdle = TimeSpan.FromDays(1);
    private static readonly TimeSpan DefaultDuplicateDetectionWindow = TimeSpan.FromMinutes(1);

    public static QueueProperties QueueProperties(
        string name,
        TimeSpan? lockDuration = null,
        long maxSizeInMegabytes = 1024,
        bool requiresDuplicateDetection = false,
        bool requiresSession = false,
        TimeSpan? defaultMessageTimeToLive = null,
        TimeSpan? autoDeleteOnIdle = null,
        bool deadLetteringOnMessageExpiration = false,
        TimeSpan? duplicateDetectionHistoryTimeWindow = null,
        int maxDeliveryCount = 10,
        bool enableBatchedOperations = true,
        EntityStatus? status = null,
        string? forwardTo = null,
        string? forwardDeadLetteredMessagesTo = null,
        string? userMetadata = null,
        bool enablePartitioning = false,
        long maxMessageSizeInKilobytes = 256) =>
        ServiceBusModelFactory.QueueProperties(
            name: name,
            lockDuration: lockDuration ?? DefaultLockDuration,
            maxSizeInMegabytes: maxSizeInMegabytes,
            requiresDuplicateDetection: requiresDuplicateDetection,
            requiresSession: requiresSession,
            defaultMessageTimeToLive: defaultMessageTimeToLive ?? DefaultMessageTimeToLive,
            autoDeleteOnIdle: autoDeleteOnIdle ?? DefaultAutoDeleteOnIdle,
            deadLetteringOnMessageExpiration: deadLetteringOnMessageExpiration,
            duplicateDetectionHistoryTimeWindow: duplicateDetectionHistoryTimeWindow ?? DefaultDuplicateDetectionWindow,
            maxDeliveryCount: maxDeliveryCount,
            enableBatchedOperations: enableBatchedOperations,
            status: status ?? EntityStatus.Active,
            forwardTo: forwardTo,
            forwardDeadLetteredMessagesTo: forwardDeadLetteredMessagesTo,
            userMetadata: userMetadata ?? string.Empty,
            enablePartitioning: enablePartitioning,
            maxMessageSizeInKilobytes: maxMessageSizeInKilobytes);

    public static SubscriptionProperties SubscriptionProperties(
        string topicName,
        string subscriptionName,
        TimeSpan? lockDuration = null,
        bool requiresSession = false,
        TimeSpan? defaultMessageTimeToLive = null,
        TimeSpan? autoDeleteOnIdle = null,
        bool deadLetteringOnMessageExpiration = false,
        int maxDeliveryCount = 10,
        bool enableBatchedOperations = true,
        EntityStatus? status = null,
        string? forwardTo = null,
        string? forwardDeadLetteredMessagesTo = null,
        string? userMetadata = null) =>
        ServiceBusModelFactory.SubscriptionProperties(
            topicName: topicName,
            subscriptionName: subscriptionName,
            lockDuration: lockDuration ?? DefaultLockDuration,
            requiresSession: requiresSession,
            defaultMessageTimeToLive: defaultMessageTimeToLive ?? DefaultMessageTimeToLive,
            autoDeleteOnIdle: autoDeleteOnIdle ?? DefaultAutoDeleteOnIdle,
            deadLetteringOnMessageExpiration: deadLetteringOnMessageExpiration,
            maxDeliveryCount: maxDeliveryCount,
            enableBatchedOperations: enableBatchedOperations,
            status: status ?? EntityStatus.Active,
            forwardTo: forwardTo,
            forwardDeadLetteredMessagesTo: forwardDeadLetteredMessagesTo,
            userMetadata: userMetadata ?? string.Empty);

    public static TopicProperties TopicProperties(
        string name,
        long maxSizeInMegabytes = 1024,
        bool requiresDuplicateDetection = false,
        TimeSpan? defaultMessageTimeToLive = null,
        TimeSpan? autoDeleteOnIdle = null,
        TimeSpan? duplicateDetectionHistoryTimeWindow = null,
        bool enableBatchedOperations = true,
        EntityStatus? status = null,
        bool enablePartitioning = false,
        long maxMessageSizeInKilobytes = 256) =>
        ServiceBusModelFactory.TopicProperties(
            name: name,
            maxSizeInMegabytes: maxSizeInMegabytes,
            requiresDuplicateDetection: requiresDuplicateDetection,
            defaultMessageTimeToLive: defaultMessageTimeToLive ?? DefaultMessageTimeToLive,
            autoDeleteOnIdle: autoDeleteOnIdle ?? DefaultAutoDeleteOnIdle,
            duplicateDetectionHistoryTimeWindow: duplicateDetectionHistoryTimeWindow ?? DefaultDuplicateDetectionWindow,
            enableBatchedOperations: enableBatchedOperations,
            status: status ?? EntityStatus.Active,
            enablePartitioning: enablePartitioning,
            maxMessageSizeInKilobytes: maxMessageSizeInKilobytes);
}

