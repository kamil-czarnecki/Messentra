using Fluxor;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources;

public sealed class ResourceSelector
{
    public IStateSelection<ResourceState, List<ResourceTreeItemData>> TreeItems { get; }
    public IStateSelection<ResourceState, ResourceTreeNode?> SelectedResource { get; }
    public IStateSelection<ResourceState, string?> SearchPhrase { get; }
    public IStateSelection<ResourceState, HashSet<string>> ExpandedKeys { get; }

    // Per-instance cache — ResourceSelector must be registered as scoped, not singleton.
    private List<NamespaceEntry>? _lastNamespaces;
    private List<ResourceTreeItemData> _lastTree = [];

    public ResourceSelector(IFeature<ResourceState> feature)
    {
        TreeItems = new StateSelection<ResourceState, List<ResourceTreeItemData>>(feature);
        SelectedResource = new StateSelection<ResourceState, ResourceTreeNode?>(feature);
        SearchPhrase = new StateSelection<ResourceState, string?>(feature);
        ExpandedKeys = new StateSelection<ResourceState, HashSet<string>>(feature);

        TreeItems.Select(state =>
        {
            if (_lastNamespaces is not null && ReferenceEquals(_lastNamespaces, state.Namespaces))
                return _lastTree;
            _lastNamespaces = state.Namespaces;
            return _lastTree = BuildTreeItems(state.Namespaces);
        });
        SelectedResource.Select(
            state => state.SelectedResource,
            ReferenceEquals);
        SearchPhrase.Select(state => state.SearchPhrase);
        ExpandedKeys.Select(state => state.ExpandedKeys);
    }

    private static List<ResourceTreeItemData> BuildTreeItems(List<NamespaceEntry> namespaces) =>
        namespaces.Select(BuildNamespaceItem).ToList();

    private static ResourceTreeItemData BuildNamespaceItem(NamespaceEntry ns)
    {
        var folderItems = ns.Folders.Values
            .OrderBy(f => f.Node.Name, StringComparer.OrdinalIgnoreCase)
            .Select(f => BuildFolderItem(f, ns))
            .ToList<TreeItemData<ResourceTreeNode>>();

        var foldersGroupItem = new ResourceTreeItemData
        {
            Text = "Folders",
            IsReadonly = true,
            Value = new FoldersTreeNode(ns.ConnectionId, ns.ConnectionName, ns.ConnectionConfig),
            Icon = Icons.Material.Filled.Folder,
            IconColor = Color.Warning,
            Expandable = true,
            Children = folderItems.Count > 0 ? folderItems : null
        };

        var queueGroupItem = new ResourceTreeItemData
        {
            Text = "Queues",
            IsReadonly = true,
            Value = new QueuesTreeNode(ns.ConnectionName, ns.ConnectionConfig),
            Icon = Icons.Material.Filled.ViewList,
            IconColor = Color.Secondary,
            Expandable = true,
            Children = ns.Queues.Values
                .OrderBy(t => t.Node.Resource.Name, StringComparer.OrdinalIgnoreCase)
                .Select(BuildQueueItem)
                .ToList<TreeItemData<ResourceTreeNode>>()
        };

        var topicGroupItem = new ResourceTreeItemData
        {
            Text = "Topics",
            IsReadonly = true,
            Value = new TopicsTreeNode(ns.ConnectionName, ns.ConnectionConfig),
            Icon = Icons.Material.Filled.DynamicFeed,
            IconColor = Color.Secondary,
            Expandable = true,
            Children = ns.Topics.Values
                .OrderBy(t => t.Node.Resource.Name, StringComparer.OrdinalIgnoreCase)
                .Select(BuildTopicItem)
                .ToList<TreeItemData<ResourceTreeNode>>()
        };

        return new ResourceTreeItemData
        {
            Text = ns.ConnectionName,
            Value = new NamespaceTreeNode(ns.ConnectionName, ns.ConnectionConfig, ns.IsLoading),
            Icon = Icons.Material.Filled.Cloud,
            IconColor = Color.Primary,
            Expandable = true,
            IsReadonly = true,
            Children = ns.IsLoading
                ? null
                : [foldersGroupItem, queueGroupItem, topicGroupItem]
        };
    }

    private static ResourceTreeItemData BuildQueueItem(QueueEntry entry)
    {
        var node = entry.Node with { IsLoading = entry.IsLoading };
        return new ResourceTreeItemData
        {
            Text = node.Resource.Name,
            Value = node,
            Expandable = false
        };
    }

    private static ResourceTreeItemData BuildTopicItem(TopicEntry entry)
    {
        var node = entry.Node with { IsLoading = entry.IsLoading };
        var subscriptionItems = entry.Subscriptions.Values
            .OrderBy(s => s.Node.Resource.Name, StringComparer.OrdinalIgnoreCase)
            .Select(BuildSubscriptionItem)
            .ToList<TreeItemData<ResourceTreeNode>>();

        return new ResourceTreeItemData
        {
            Text = node.Resource.Name,
            Value = node,
            Icon = Icons.Material.Filled.Topic,
            IconColor = Color.Secondary,
            Expandable = subscriptionItems.Count > 0,
            Children = subscriptionItems.Count > 0 ? subscriptionItems : null
        };
    }

    private static ResourceTreeItemData BuildSubscriptionItem(SubscriptionEntry entry)
    {
        var node = entry.Node with { IsLoading = entry.IsLoading };
        return new ResourceTreeItemData
        {
            Text = node.Resource.Name,
            Value = node,
            Expandable = false
        };
    }

    private static ResourceTreeItemData BuildFolderItem(FolderEntry entry, NamespaceEntry ns)
    {
        var queueItems = new List<ResourceTreeItemData>();
        var subsByTopicUrl = new Dictionary<string, (TopicEntry Topic, List<SubscriptionEntry> Subs)>();

        foreach (var url in entry.ResourceUrls)
        {
            if (ns.Queues.TryGetValue(url, out var queue))
            {
                queueItems.Add(WithParentFolder(BuildQueueItem(queue), entry.Node));
                continue;
            }
            foreach (var topicEntry in ns.Topics.Values)
            {
                if (!topicEntry.Subscriptions.TryGetValue(url, out var sub)) continue;
                var topicUrl = topicEntry.Node.Resource.Url;
                if (!subsByTopicUrl.TryGetValue(topicUrl, out _))
                    subsByTopicUrl[topicUrl] = (topicEntry, []);
                subsByTopicUrl[topicUrl].Subs.Add(sub);
                break;
            }
        }

        var allItems = queueItems
            .Concat(subsByTopicUrl.Values.Select(TreeItemData<ResourceTreeNode> (g) =>
                BuildDerivedTopicHeader(g.Topic, g.Subs, entry.Node)))
            .OrderBy(i => i.Text, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ResourceTreeItemData
        {
            Text = entry.Node.Name,
            Value = entry.Node,
            Icon = Icons.Material.Filled.FolderOpen,
            IconColor = Color.Warning,
            Expandable = allItems.Count > 0,
            Children = allItems.Count > 0 ? allItems : null,
            IsReadonly = true
        };
    }

    private static ResourceTreeItemData BuildDerivedTopicHeader(
        TopicEntry topicEntry,
        List<SubscriptionEntry> subsInFolder,
        FolderTreeNode parentFolder)
    {
        var topicNode = topicEntry.Node with { IsLoading = topicEntry.IsLoading };
        var subItems = subsInFolder
            .OrderBy(s => s.Node.Resource.Name, StringComparer.OrdinalIgnoreCase)
            .Select(TreeItemData<ResourceTreeNode> (s) => WithParentFolder(BuildSubscriptionItem(s), parentFolder))
            .ToList();

        return new ResourceTreeItemData
        {
            Text = topicNode.Resource.Name,
            Value = topicNode,
            Icon = Icons.Material.Filled.Topic,
            IconColor = Color.Secondary,
            Expandable = subItems.Count > 0,
            Children = subItems.Count > 0 ? subItems : null,
            ParentFolderNode = parentFolder
        };
    }

    private static ResourceTreeItemData WithParentFolder(ResourceTreeItemData item, FolderTreeNode folder) =>
        new()
        {
            Text = item.Text,
            Value = item.Value,
            Icon = item.Icon,
            IconColor = item.IconColor,
            IsReadonly = item.IsReadonly,
            Expandable = item.Expandable,
            Children = item.Children,
            ParentFolderNode = folder
        };
}
