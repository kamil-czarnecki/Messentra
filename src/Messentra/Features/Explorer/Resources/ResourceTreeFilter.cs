using MudBlazor;

namespace Messentra.Features.Explorer.Resources;

public static class ResourceTreeFilter
{
    public static List<ResourceTreeItemData> Filter(List<ResourceTreeItemData> resources, SearchQuery query)
    {
        if (query.IsEmpty)
            return resources;

        return resources
            .Select(ns => FilterNamespace(ns, query))
            .OfType<ResourceTreeItemData>()
            .ToList();
    }

    private static ResourceTreeItemData? FilterNamespace(ResourceTreeItemData ns, SearchQuery query)
    {
        if (query.NamespaceFilter != null &&
            ns.Text?.Contains(query.NamespaceFilter, StringComparison.OrdinalIgnoreCase) != true)
            return null;

        if (query.NamePhrase == null && !query.HasDlq)
            return CloneExpanded(ns, ns.Children);

        if (ns.Children is null)
            return null;

        var filteredGroups = ns.Children
            .OfType<ResourceTreeItemData>()
            .Select(group => FilterGroup(group, query))
            .OfType<ResourceTreeItemData>()
            .ToList<ITreeItemData<ResourceTreeNode>>();

        return filteredGroups.Count == 0 ? null : CloneExpanded(ns, filteredGroups);
    }

    private static ResourceTreeItemData? FilterGroup(ResourceTreeItemData group, SearchQuery query)
    {
        var filteredItems = group.Children?
            .OfType<ResourceTreeItemData>()
            .Select(item => FilterItem(item, query))
            .OfType<ResourceTreeItemData>()
            .ToList<ITreeItemData<ResourceTreeNode>>();

        return filteredItems is { Count: > 0 } ? CloneExpanded(group, filteredItems) : null;
    }

    private static ResourceTreeItemData? FilterItem(ResourceTreeItemData item, SearchQuery query)
    {
        var nameMatches = query.NamePhrase == null ||
                          item.Text?.Contains(query.NamePhrase, StringComparison.OrdinalIgnoreCase) == true;

        if (item.Children is not { Count: > 0 } children)
        {
            if (!nameMatches)
                return null;

            if (query.HasDlq && !HasDlqMessages(item.Value))
                return null;

            return item;
        }

        // When children themselves have children (e.g. folder containing derived topic headers),
        // recurse rather than treating those children as leaf subscriptions.
        var childrenAreNested = children.OfType<ResourceTreeItemData>().Any(c => c.Children is { Count: > 0 });
        if (childrenAreNested)
        {
            if (nameMatches && !query.HasDlq)
                return CloneExpanded(item, children.ToList(), item.Selected);

            var filteredChildren = children
                .OfType<ResourceTreeItemData>()
                .Select(c => FilterItem(c, query))
                .OfType<ResourceTreeItemData>()
                .ToList<ITreeItemData<ResourceTreeNode>>();

            if (filteredChildren.Count == 0)
                return null;

            return CloneExpanded(item, filteredChildren, item.Selected);
        }

        var matchingSubs = children
            .OfType<ResourceTreeItemData>()
            .Where(s => SubscriptionMatchesQuery(s, query))
            .ToList<ITreeItemData<ResourceTreeNode>>();

        if (!nameMatches && matchingSubs.Count == 0)
            return null;

        List<ITreeItemData<ResourceTreeNode>> childrenToShow;
        if (nameMatches)
        {
            childrenToShow = query.HasDlq
                ? children
                    .OfType<ResourceTreeItemData>()
                    .Where(s => HasDlqMessages(s.Value))
                    .ToList<ITreeItemData<ResourceTreeNode>>()
                : children.ToList();
        }
        else
        {
            childrenToShow = matchingSubs;
        }

        if (query.HasDlq && childrenToShow.Count == 0)
            return null;

        return CloneExpanded(item, childrenToShow, item.Selected);
    }

    private static bool SubscriptionMatchesQuery(ResourceTreeItemData sub, SearchQuery query)
    {
        var nameMatches = query.NamePhrase == null ||
                          sub.Text?.Contains(query.NamePhrase, StringComparison.OrdinalIgnoreCase) == true;
        var dlqMatches = !query.HasDlq || HasDlqMessages(sub.Value);
        
        return nameMatches && dlqMatches;
    }

    public static bool HasDlqMessages(ResourceTreeNode? node) => node switch
    {
        QueueTreeNode q => q.Resource.Overview.MessageInfo.DeadLetter > 0,
        SubscriptionTreeNode s => s.Resource.Overview.MessageInfo.DeadLetter > 0,
        _ => false
    };

    private static ResourceTreeItemData CloneExpanded(
        ResourceTreeItemData source,
        IReadOnlyCollection<ITreeItemData<ResourceTreeNode>>? children,
        bool selected = false) =>
        new()
        {
            Text = source.Text, Value = source.Value, Icon = source.Icon, IconColor = source.IconColor,
            IsReadonly = source.IsReadonly, Expandable = true, Expanded = true, Selected = selected,
            Children = children
        };
}
