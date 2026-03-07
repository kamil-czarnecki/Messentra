using Messentra.Infrastructure.AzureServiceBus;
using Microsoft.AspNetCore.Components;

namespace Messentra.Features.Explorer.Resources.Components.Details.Tabs.Properties;

public partial class PropertiesTab
{
    [Parameter]
    public required ResourceTreeNode Resource { get; init; }

    private QueueProperties? QueueProps => (Resource as QueueTreeNode)?.Resource.Properties;
    private TopicProperties? TopicProps => (Resource as TopicTreeNode)?.Resource.Properties;
    private SubscriptionProperties? SubProps => (Resource as SubscriptionTreeNode)?.Resource.Properties;

    private TimeSpan? DefaultMessageTimeToLive =>
        QueueProps?.DefaultMessageTimeToLive ?? SubProps?.DefaultMessageTimeToLive ?? TopicProps?.DefaultMessageTimeToLive;

    private TimeSpan? AutoDeleteOnIdle =>
        QueueProps?.AutoDeleteOnIdle ?? SubProps?.AutoDeleteOnIdle ?? TopicProps?.AutoDeleteOnIdle;

    private TimeSpan? LockDuration => QueueProps?.LockDuration ?? SubProps?.LockDuration;

    private int? MaxDeliveryCount => QueueProps?.MaxDeliveryCount ?? SubProps?.MaxDeliveryCount;

    private bool? DeadLetteringOnExpiration =>
        QueueProps?.DeadLetteringOnMessageExpiration ?? SubProps?.DeadLetteringOnMessageExpiration;

    private bool? EnablePartitioning =>
        QueueProps?.EnablePartitioning ?? TopicProps?.EnablePartitioning;

    private TimeSpan? DuplicateDetectionWindow =>
        QueueProps?.DuplicateDetectionWindow ?? TopicProps?.DuplicateDetectionWindow;

    private bool? RequiresDuplicateDetection =>
        QueueProps?.RequiresDuplicateDetection ?? TopicProps?.RequiresDuplicateDetection;

    private bool? RequiresSession => QueueProps?.RequiresSession ?? SubProps?.RequiresSession;

    private string? ForwardTo => QueueProps?.ForwardTo ?? SubProps?.ForwardTo;

    private string? ForwardDeadLetteredMessagesTo =>
        QueueProps?.ForwardDeadLetteredMessagesTo ?? SubProps?.ForwardDeadLetteredMessagesTo;

    private string? UserMetadata => QueueProps?.UserMetadata ?? SubProps?.UserMetadata ?? TopicProps?.UserMetadata;

    private long? MaxMessageSizeInKilobytes => QueueProps?.MaxMessageSizeInKilobytes ?? TopicProps?.MaxMessageSizeInKilobytes;

    private long? MaxSizeInMegabytes => Resource switch
    {
        QueueTreeNode q when q.Resource.Overview.SizeInfo.MaxSizeInMegabytes > 0 => q.Resource.Overview.SizeInfo.MaxSizeInMegabytes,
        TopicTreeNode t when t.Resource.Overview.SizeInfo.MaxSizeInMegabytes > 0 => t.Resource.Overview.SizeInfo.MaxSizeInMegabytes,
        _ => null
    };
}

