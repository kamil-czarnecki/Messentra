using Messentra.Infrastructure.AzureServiceBus;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details.Tabs.Overview;

public partial class OverviewTab
{
    [Parameter]
    public required ResourceTreeNode Resource { get; init; }

    private ResourceOverview? OverviewData => Resource switch
    {
        QueueTreeNode q => q.Resource.Overview,
        TopicTreeNode t => t.Resource.Overview,
        SubscriptionTreeNode s => s.Resource.Overview,
        _ => null
    };

    private Color StatusColor => ResourceStatusColorMapper.GetStatusColor(OverviewData?.Status);

    private static string FormatBytes(long? bytes)
    {
        return bytes switch
        {
            null => "-",
            >= 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
            >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            >= 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes} B"
        };
    }
}
