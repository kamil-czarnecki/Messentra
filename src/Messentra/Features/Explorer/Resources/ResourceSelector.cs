using Fluxor;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources;

public sealed class ResourceSelector
{
    public IStateSelection<ResourceState, List<ResourceTreeItemData>> TreeItems { get; }
    public IStateSelection<ResourceState, ResourceTreeNode?> SelectedResource { get; }
    public IStateSelection<ResourceState, string?> SearchPhrase { get; }

    public ResourceSelector(IFeature<ResourceState> feature)
    {
        TreeItems = new StateSelection<ResourceState, List<ResourceTreeItemData>>(feature);
        SelectedResource = new StateSelection<ResourceState, ResourceTreeNode?>(feature);
        SearchPhrase = new StateSelection<ResourceState, string?>(feature);

        TreeItems.Select(
            state => BuildTreeItems(state.Namespaces, state.SelectedResource, state.ExpandedKeys));
        SelectedResource.Select(
            state => state.SelectedResource,
            ReferenceEquals);
        SearchPhrase.Select(state => state.SearchPhrase);
    }

    private static List<ResourceTreeItemData> BuildTreeItems(
        List<NamespaceEntry> namespaces,
        ResourceTreeNode? selected,
        HashSet<string> expandedKeys) =>
        namespaces.Select(ns => BuildNamespaceItem(ns, selected, expandedKeys)).ToList();

    private static ResourceTreeItemData BuildNamespaceItem(NamespaceEntry ns, ResourceTreeNode? selected, HashSet<string> expandedKeys)
    {
        var folderItems = ns.Folders.Values
            .OrderBy(f => f.Node.Name, StringComparer.OrdinalIgnoreCase)
            .Select(f => BuildFolderItem(f, ns, selected, expandedKeys))
            .ToList<TreeItemData<ResourceTreeNode>>();

        var foldersGroupItem = new ResourceTreeItemData
        {
            Text = "Folders",
            IsReadonly = true,
            Value = new FoldersTreeNode(ns.ConnectionId, ns.ConnectionName, ns.ConnectionConfig),
            Icon = Icons.Material.Filled.Folder,
            IconColor = Color.Warning,
            Expandable = true,
            Expanded = expandedKeys.Contains($"folders:{ns.ConnectionName}"),
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
            Expanded = expandedKeys.Contains($"queues:{ns.ConnectionName}"),
            Children = ns.Queues.Values
                .OrderBy(t => t.Node.Resource.Name, StringComparer.OrdinalIgnoreCase)
                .Select(q => BuildQueueItem(q, selected))
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
            Expanded = expandedKeys.Contains($"topics:{ns.ConnectionName}"),
            Children = ns.Topics.Values
                .OrderBy(t => t.Node.Resource.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => BuildTopicItem(t, selected, expandedKeys))
                .ToList<TreeItemData<ResourceTreeNode>>()
        };

        return new ResourceTreeItemData
        {
            Text = ns.ConnectionName,
            Value = new NamespaceTreeNode(ns.ConnectionName, ns.ConnectionConfig, ns.IsLoading),
            Icon = Icons.Material.Filled.Cloud,
            IconColor = Color.Primary,
            Expandable = true,
            Expanded = expandedKeys.Contains($"ns:{ns.ConnectionName}"),
            IsReadonly = true,
            Children = ns.IsLoading
                ? null
                : [foldersGroupItem, queueGroupItem, topicGroupItem]
        };
    }

    private static ResourceTreeItemData BuildQueueItem(QueueEntry entry, ResourceTreeNode? selected)
    {
        var node = entry.Node with { IsLoading = entry.IsLoading };
        return new ResourceTreeItemData
        {
            Text = node.Resource.Name,
            Value = node,
            Expandable = false,
            Selected = selected is QueueTreeNode s && s.Resource.Url == node.Resource.Url
        };
    }

    private static ResourceTreeItemData BuildTopicItem(TopicEntry entry, ResourceTreeNode? selected, HashSet<string> expandedKeys)
    {
        var node = entry.Node with { IsLoading = entry.IsLoading };
        var subscriptionItems = entry.Subscriptions.Values
            .OrderBy(s => s.Node.Resource.Name, StringComparer.OrdinalIgnoreCase)
            .Select(s => BuildSubscriptionItem(s, selected))
            .ToList<TreeItemData<ResourceTreeNode>>();

        return new ResourceTreeItemData
        {
            Text = node.Resource.Name,
            Value = node,
            Icon = Icons.Material.Filled.Topic,
            IconColor = Color.Secondary,
            Expandable = subscriptionItems.Count > 0,
            Expanded = expandedKeys.Contains($"topic:{node.Resource.Url}"),
            Children = subscriptionItems.Count > 0 ? subscriptionItems : null,
            Selected = selected is TopicTreeNode t && t.Resource.Url == node.Resource.Url
        };
    }

    private static ResourceTreeItemData BuildSubscriptionItem(SubscriptionEntry entry, ResourceTreeNode? selected)
    {
        var node = entry.Node with { IsLoading = entry.IsLoading };
        return new ResourceTreeItemData
        {
            Text = node.Resource.Name,
            Value = node,
            Expandable = false,
            Selected = selected is SubscriptionTreeNode s && s.Resource.Url == node.Resource.Url
        };
    }

    private static ResourceTreeItemData BuildFolderItem(FolderEntry entry, NamespaceEntry ns, ResourceTreeNode? selected, HashSet<string> expandedKeys)
    {
        var resourceItems = entry.ResourceUrls
            .Select(url => ResolveResourceItem(url, ns, selected, entry.Node))
            .OfType<ResourceTreeItemData>()
            .ToList<TreeItemData<ResourceTreeNode>>();

        return new ResourceTreeItemData
        {
            Text = entry.Node.Name,
            Value = entry.Node,
            Icon = Icons.Material.Filled.FolderOpen,
            IconColor = Color.Warning,
            Expandable = resourceItems.Count > 0,
            Expanded = expandedKeys.Contains($"folder:{entry.Node.FolderId}"),
            Children = resourceItems.Count > 0 ? resourceItems : null
        };
    }

    private static ResourceTreeItemData? ResolveResourceItem(string resourceUrl, NamespaceEntry ns, ResourceTreeNode? selected, FolderTreeNode? parentFolder = null)
    {
        ResourceTreeItemData? item = null;

        if (ns.Queues.TryGetValue(resourceUrl, out var queue))
            item = BuildQueueItem(queue, selected);
        else if (ns.Topics.TryGetValue(resourceUrl, out var topic))
            item = BuildTopicItem(topic, selected, []);
        else
        {
            foreach (var topicEntry in ns.Topics.Values)
            {
                if (!topicEntry.Subscriptions.TryGetValue(resourceUrl, out var sub)) continue;
                item = BuildSubscriptionItem(sub, selected);
                break;
            }
        }

        if (item is null || parentFolder is null) return item;

        return new ResourceTreeItemData
        {
            Text = item.Text,
            Value = item.Value,
            Icon = item.Icon,
            IconColor = item.IconColor,
            IsReadonly = item.IsReadonly,
            Expandable = item.Expandable,
            Expanded = item.Expanded,
            Selected = item.Selected,
            Children = item.Children,
            ParentFolderNode = parentFolder
        };
    }
}
