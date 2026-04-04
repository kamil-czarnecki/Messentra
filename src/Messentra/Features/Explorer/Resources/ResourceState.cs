using Fluxor;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources;

[FeatureState]
public sealed record ResourceState(List<NamespaceEntry> Namespaces, ResourceTreeNode? SelectedResource, HashSet<string> ExpandedKeys, string? SearchPhrase = null)
{
    private ResourceState() : this([], null, [], null) { }
}

public sealed record NamespaceEntry(
    long ConnectionId,
    string ConnectionName,
    ConnectionConfig ConnectionConfig,
    bool IsLoading,
    Dictionary<string, QueueEntry> Queues,
    Dictionary<string, TopicEntry> Topics,
    Dictionary<long, FolderEntry> Folders);

public sealed record FolderEntry(FolderTreeNode Node, IReadOnlySet<string> ResourceUrls);

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
    public FolderTreeNode? ParentFolderNode { get; init; }
}

public abstract record ResourceTreeNode(ConnectionConfig ConnectionConfig, bool IsLoading = false);

public sealed record NamespaceTreeNode(string ConnectionName, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);
public sealed record FoldersTreeNode(long ConnectionId, string ConnectionName, ConnectionConfig ConnectionConfig) : ResourceTreeNode(ConnectionConfig);
public sealed record FolderTreeNode(long FolderId, long ConnectionId, string Name, string ConnectionName, ConnectionConfig ConnectionConfig) : ResourceTreeNode(ConnectionConfig);
public sealed record QueuesTreeNode(string ConnectionName, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);
public sealed record QueueTreeNode(string ConnectionName, Resource.Queue Resource, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);
public sealed record TopicsTreeNode(string ConnectionName, ConnectionConfig ConnectionConfig) : ResourceTreeNode(ConnectionConfig);
public sealed record TopicTreeNode(string ConnectionName, Resource.Topic Resource, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);
public sealed record SubscriptionTreeNode(string ConnectionName, Resource.Subscription Resource, ConnectionConfig ConnectionConfig, bool IsLoading = false) : ResourceTreeNode(ConnectionConfig, IsLoading);
