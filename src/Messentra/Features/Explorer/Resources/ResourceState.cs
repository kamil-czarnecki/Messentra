using Fluxor;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources;

[FeatureState]
public sealed record ResourceState(List<ResourceTreeItemData> Resources, ResourceTreeItemData? SelectedResource)
{
    private ResourceState() : this([], null)
    {
    }
    
    
}

public class ResourceTreeItemData : TreeItemData<ResourceTreeNode>
{
    public bool IsReadonly { get; init; }
    public Color IconColor { get; init; } = Color.Default;
    public string? EndIcon { get; init; }
    public Color EndIconColor { get; init; } = Color.Default;
}

public abstract record ResourceTreeNode(ConnectionConfig ConnectionConfig, bool IsLoading = false);

public record NamespaceTreeNode(string ConnectionName, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);
public record QueuesTreeNode(string ConnectionName, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);

public record QueueTreeNode(string ConnectionName, Resource.Queue Resource, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);

public record TopicsTreeNode(string ConnectionName, ConnectionConfig ConnectionConfig) : ResourceTreeNode(ConnectionConfig);
public record TopicTreeNode(string ConnectionName, Resource.Topic Resource, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);

public record SubscriptionTreeNode(string ConnectionName, Resource.Subscription Resource, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);
