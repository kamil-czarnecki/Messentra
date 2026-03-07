using Fluxor;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources;

[FeatureState]
public sealed record ResourceState(List<NamespaceEntry> Namespaces, ResourceTreeNode? SelectedResource, HashSet<string> ExpandedKeys)
{
    private ResourceState() : this([], null, []) { }
}

public sealed record NamespaceEntry(
    string ConnectionName,
    ConnectionConfig ConnectionConfig,
    bool IsLoading,
    Dictionary<string, QueueEntry> Queues,
    Dictionary<string, TopicEntry> Topics);

public sealed record QueueEntry(QueueTreeNode Node, bool IsLoading);

public sealed record TopicEntry(
    TopicTreeNode Node,
    bool IsLoading,
    Dictionary<string, SubscriptionEntry> Subscriptions);

public sealed record SubscriptionEntry(SubscriptionTreeNode Node, bool IsLoading);

public class ResourceTreeItemData : TreeItemData<ResourceTreeNode>
{
    public bool IsReadonly { get; init; }
    public Color IconColor { get; init; } = Color.Default;
}

public abstract record ResourceTreeNode(ConnectionConfig ConnectionConfig, bool IsLoading = false);

public record NamespaceTreeNode(string ConnectionName, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);
public record QueuesTreeNode(string ConnectionName, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);
public record QueueTreeNode(string ConnectionName, Resource.Queue Resource, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);
public record TopicsTreeNode(string ConnectionName, ConnectionConfig ConnectionConfig) : ResourceTreeNode(ConnectionConfig);
public record TopicTreeNode(string ConnectionName, Resource.Topic Resource, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);
public record SubscriptionTreeNode(string ConnectionName, Resource.Subscription Resource, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);
