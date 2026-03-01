using Messentra.Infrastructure.AzureServiceBus;
using Microsoft.AspNetCore.Components;

namespace Messentra.Features.Explorer.Resources.Components.Details.Tabs.Properties;

public partial class PropertiesTab
{
    [Parameter]
    public required ResourceTreeItemData Resource { get; init; }

    private QueueProperties? QueueProps => (Resource.Value as QueueTreeNode)?.Resource.Properties;
    private TopicProperties? TopicProps => (Resource.Value as TopicTreeNode)?.Resource.Properties;
    private SubscriptionProperties? SubProps => (Resource.Value as SubscriptionTreeNode)?.Resource.Properties;

    // Shared across Queue / Subscription / Topic
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

    private long? MaxSizeInMegabytes => Resource.Value switch
    {
        QueueTreeNode q when q.Resource.Overview.SizeInfo.MaxSizeInMegabytes > 0 => q.Resource.Overview.SizeInfo.MaxSizeInMegabytes,
        TopicTreeNode t when t.Resource.Overview.SizeInfo.MaxSizeInMegabytes > 0 => t.Resource.Overview.SizeInfo.MaxSizeInMegabytes,
        _ => null
    };

    private static (int days, int hours, int minutes, int seconds) SplitFull(TimeSpan? ts)
    {
        if (ts is null || ts == TimeSpan.MaxValue) return (0, 0, 0, 0);
        return ((int)ts.Value.TotalDays, ts.Value.Hours, ts.Value.Minutes, ts.Value.Seconds);
    }

    private static (int minutes, int seconds) SplitMinSec(TimeSpan? ts)
    {
        if (ts is null || ts == TimeSpan.MaxValue) return (0, 0);
        return ((int)ts.Value.TotalMinutes, ts.Value.Seconds);
    }
}

