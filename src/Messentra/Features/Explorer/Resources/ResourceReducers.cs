using Fluxor;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources;

public static class ResourceReducers
{
    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, FetchResourcesAction action)
        => state with
        {
            Resources =
            [
                ..state.Resources,
                new ResourceTreeItemData
                {
                    Text = action.ConnectionName,
                    Value = new NamespaceTreeNode(action.ConnectionName, action.ConnectionConfig, IsLoading: true),
                    Icon = Icons.Material.Filled.Cloud,
                    IconColor = Color.Primary,
                    Expandable = true,
                    IsReadonly = true
                }
            ]
        };

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, FetchResourcesSuccessAction action)
    {
       var node = action.Resource.Value as NamespaceTreeNode;
       var updatedResources = state.Resources
           .Where(r => ((NamespaceTreeNode)r.Value!).ConnectionName != node!.ConnectionName)
           .ToList();

        updatedResources.Add(action.Resource);
        
        return state with { Resources = updatedResources };
    }

    [ReducerMethod]
    public static ResourceState OnFetchResourcesFailure(ResourceState state, FetchResourcesFailureAction _)
        => state;

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, SelectResourceAction action)
    {
        var updatedResources = state.Resources
            .Select(r => SetSelected(r, action.Resource.Value))
            .ToList();
        
        return new ResourceState(Resources: updatedResources, action.Resource);
    }
    
    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, DisconnectResourceAction action)
    {
        var node = action.Resource.Value as NamespaceTreeNode;
        var updatedResources = state.Resources
            .Where(r => r.Value != node)
            .ToList();
        
        return new ResourceState(Resources: updatedResources, SelectedResource: null);
    }
    
    private static ResourceTreeItemData SetSelected(ResourceTreeItemData node, ResourceTreeNode? selectedValue) =>
        new()
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text,
            Value = node.Value,
            Icon = node.Icon,
            IconColor = node.IconColor,
            EndIcon =  node.EndIcon,
            EndIconColor = node.EndIconColor,
            Expanded = node.Expanded,
            Expandable = node.Expandable,
            Selected = node.Value == selectedValue,
            Children = node.Children?
                .OfType<ResourceTreeItemData>()
                .Select(child => SetSelected(child, selectedValue))
                .ToList()
        };

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshQueueAction action)
    {
        var updatedResources = state.Resources
            .Select(r => SetQueueNodeLoading(r, action.Node))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value is QueueTreeNode q &&
                              q.Resource.Url == action.Node.Resource.Url
            ? SetLoadingOnItem(state.SelectedResource, true)
            : state.SelectedResource;

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshQueueSuccessAction action)
    {
        var updatedResources = state.Resources
            .Select(r => ReplaceQueueNode(r, action.UpdatedNode))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value is QueueTreeNode q &&
                              q.Resource.Url == action.UpdatedNode.Resource.Url
            ? ReplaceQueueNodeInItem(state.SelectedResource, action.UpdatedNode)
            : state.SelectedResource;

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshQueueFailureAction action)
    {
        var updatedResources = state.Resources
            .Select(r => ClearQueueNodeLoading(r, action.Node))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value is QueueTreeNode q &&
                              q.Resource.Url == action.Node.Resource.Url
            ? SetLoadingOnItem(state.SelectedResource, false)
            : state.SelectedResource;

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshTopicAction action)
    {
        var updatedResources = state.Resources
            .Select(r => SetTopicNodeLoading(r, action.Node))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value switch
        {
            TopicTreeNode t when t.Resource.Url == action.Node.Resource.Url
                => SetLoadingOnItem(state.SelectedResource, true),
            SubscriptionTreeNode s when s.ConnectionConfig == action.Node.ConnectionConfig &&
                                        action.Node.Resource.Subscriptions.Any(sub => sub.Url == s.Resource.Url)
                => SetLoadingOnItem(state.SelectedResource, true),
            _ => state.SelectedResource
        };

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshTopicSuccessAction action)
    {
        var updatedResources = state.Resources
            .Select(r => ReplaceTopicNode(r, action.UpdatedNode))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value is TopicTreeNode t &&
                              t.Resource.Url == action.UpdatedNode.Resource.Url
            ? ReplaceTopicNodeInItem(state.SelectedResource, action.UpdatedNode)
            : state.SelectedResource;

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshTopicFailureAction action)
    {
        var updatedResources = state.Resources
            .Select(r => ClearTopicNodeLoading(r, action.Node))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value switch
        {
            TopicTreeNode t when t.Resource.Url == action.Node.Resource.Url
                => SetLoadingOnItem(state.SelectedResource, false),
            SubscriptionTreeNode s when s.ConnectionConfig == action.Node.ConnectionConfig &&
                                        action.Node.Resource.Subscriptions.Any(sub => sub.Url == s.Resource.Url)
                => SetLoadingOnItem(state.SelectedResource, false),
            _ => state.SelectedResource
        };

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshSubscriptionAction action)
    {
        var updatedResources = state.Resources
            .Select(r => SetSubscriptionNodeLoading(r, action.Node))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value is SubscriptionTreeNode s &&
                              s.Resource.Url == action.Node.Resource.Url
            ? SetLoadingOnItem(state.SelectedResource, true)
            : state.SelectedResource;

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshSubscriptionSuccessAction action)
    {
        var updatedResources = state.Resources
            .Select(r => ReplaceSubscriptionNode(r, action.UpdatedNode))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value is SubscriptionTreeNode s &&
                              s.Resource.Url == action.UpdatedNode.Resource.Url
            ? ReplaceSubscriptionNodeInItem(state.SelectedResource, action.UpdatedNode)
            : state.SelectedResource;

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshSubscriptionFailureAction action)
    {
        var updatedResources = state.Resources
            .Select(r => ClearSubscriptionNodeLoading(r, action.Node))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value is SubscriptionTreeNode s &&
                              s.Resource.Url == action.Node.Resource.Url
            ? SetLoadingOnItem(state.SelectedResource, false)
            : state.SelectedResource;

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshQueuesAction action)
    {
        var updatedResources = state.Resources
            .Select(r => SetAllQueuesLoading(r, action.Node, true))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value is QueueTreeNode q &&
                              q.ConnectionConfig == action.Node.ConnectionConfig
            ? SetLoadingOnItem(state.SelectedResource, true)
            : state.SelectedResource;

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshQueuesSuccessAction action)
    {
        var updatedResources = state.Resources
            .Select(r => ReplaceAllQueueNodes(r, action.Node, action.UpdatedNodes))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value is QueueTreeNode q &&
                              q.ConnectionConfig == action.Node.ConnectionConfig
            ? ReplaceQueueNodeInItem(state.SelectedResource,
                action.UpdatedNodes.FirstOrDefault(n => n.Resource.Url == q.Resource.Url)
                ?? q with { IsLoading = false })
            : state.SelectedResource;

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshQueuesFailureAction action)
    {
        var updatedResources = state.Resources
            .Select(r => SetAllQueuesLoading(r, action.Node, false))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value is QueueTreeNode q &&
                              q.ConnectionConfig == action.Node.ConnectionConfig
            ? SetLoadingOnItem(state.SelectedResource, false)
            : state.SelectedResource;

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshTopicsAction action)
    {
        var updatedResources = state.Resources
            .Select(r => SetAllTopicsLoading(r, action.Node, true))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value switch
        {
            TopicTreeNode t when t.ConnectionConfig == action.Node.ConnectionConfig
                => SetLoadingOnItem(state.SelectedResource, true),
            SubscriptionTreeNode s when s.ConnectionConfig == action.Node.ConnectionConfig
                => SetLoadingOnItem(state.SelectedResource, true),
            _ => state.SelectedResource
        };

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshTopicsSuccessAction action)
    {
        var updatedResources = state.Resources
            .Select(r => ReplaceAllTopicNodes(r, action.Node, action.UpdatedNodes))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value switch
        {
            TopicTreeNode t when t.ConnectionConfig == action.Node.ConnectionConfig =>
                ReplaceTopicNodeInItem(state.SelectedResource,
                    action.UpdatedNodes.FirstOrDefault(n => n.Resource.Url == t.Resource.Url)
                    ?? t with { IsLoading = false }),
            SubscriptionTreeNode s when s.ConnectionConfig == action.Node.ConnectionConfig =>
                ReplaceSubscriptionNodeInItem(state.SelectedResource,
                    action.UpdatedNodes
                        .SelectMany(t => t.Resource.Subscriptions
                            .Select(sub => new SubscriptionTreeNode(t.ConnectionName, sub, t.ConnectionConfig)))
                        .FirstOrDefault(n => n.Resource.Url == s.Resource.Url)
                    ?? s with { IsLoading = false }),
            _ => state.SelectedResource
        };

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshTopicsFailureAction action)
    {
        var updatedResources = state.Resources
            .Select(r => SetAllTopicsLoading(r, action.Node, false))
            .ToList();

        var updatedSelected = state.SelectedResource?.Value switch
        {
            TopicTreeNode t when t.ConnectionConfig == action.Node.ConnectionConfig
                => SetLoadingOnItem(state.SelectedResource, false),
            SubscriptionTreeNode s when s.ConnectionConfig == action.Node.ConnectionConfig
                => SetLoadingOnItem(state.SelectedResource, false),
            _ => state.SelectedResource
        };

        return new ResourceState(Resources: updatedResources, SelectedResource: updatedSelected);
    }
    
    private static ResourceTreeItemData SetQueueNodeLoading(ResourceTreeItemData node, QueueTreeNode target)
    {
        if (node.Value is QueueTreeNode q && q.Resource.Url == target.Resource.Url)
            return SetLoadingOnItem(node, true);

        if (node.Children is null)
            return node;

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text, Value = node.Value, Icon = node.Icon, IconColor = node.IconColor,
            EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
            Expandable = node.Expandable, Selected = node.Selected,
            Children = node.Children.OfType<ResourceTreeItemData>()
                .Select(child => SetQueueNodeLoading(child, target))
                .ToList<TreeItemData<ResourceTreeNode>>()
        };
    }

    private static ResourceTreeItemData ClearQueueNodeLoading(ResourceTreeItemData node, QueueTreeNode target)
    {
        if (node.Value is QueueTreeNode q && q.Resource.Url == target.Resource.Url)
            return SetLoadingOnItem(node, false);

        if (node.Children is null)
            return node;

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text, Value = node.Value, Icon = node.Icon, IconColor = node.IconColor,
            EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
            Expandable = node.Expandable, Selected = node.Selected,
            Children = node.Children.OfType<ResourceTreeItemData>()
                .Select(child => ClearQueueNodeLoading(child, target))
                .ToList<TreeItemData<ResourceTreeNode>>()
        };
    }

    private static ResourceTreeItemData SetTopicNodeLoading(ResourceTreeItemData node, TopicTreeNode target)
    {
        if (node.Value is TopicTreeNode t && t.Resource.Url == target.Resource.Url)
            return SetLoadingOnTopicItem(node, true);

        if (node.Children is null)
            return node;

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text, Value = node.Value, Icon = node.Icon, IconColor = node.IconColor,
            EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
            Expandable = node.Expandable, Selected = node.Selected,
            Children = node.Children.OfType<ResourceTreeItemData>()
                .Select(child => SetTopicNodeLoading(child, target))
                .ToList<TreeItemData<ResourceTreeNode>>()
        };
    }

    private static ResourceTreeItemData ClearTopicNodeLoading(ResourceTreeItemData node, TopicTreeNode target)
    {
        if (node.Value is TopicTreeNode t && t.Resource.Url == target.Resource.Url)
            return SetLoadingOnTopicItem(node, false);

        if (node.Children is null)
            return node;

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text, Value = node.Value, Icon = node.Icon, IconColor = node.IconColor,
            EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
            Expandable = node.Expandable, Selected = node.Selected,
            Children = node.Children.OfType<ResourceTreeItemData>()
                .Select(child => ClearTopicNodeLoading(child, target))
                .ToList<TreeItemData<ResourceTreeNode>>()
        };
    }

    // Sets IsLoading on the topic node itself AND all its subscription children
    private static ResourceTreeItemData SetLoadingOnTopicItem(ResourceTreeItemData node, bool isLoading) =>
        new()
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text,
            Value = node.Value is TopicTreeNode t ? t with { IsLoading = isLoading } : node.Value,
            Icon = node.Icon,
            IconColor = node.IconColor,
            EndIcon = node.EndIcon,
            EndIconColor = node.EndIconColor,
            Expanded = node.Expanded,
            Expandable = node.Expandable,
            Selected = node.Selected,
            Children = node.Children?
                .OfType<ResourceTreeItemData>()
                .Select(child => child.Value is SubscriptionTreeNode
                    ? SetLoadingOnItem(child, isLoading)
                    : child)
                .ToList<TreeItemData<ResourceTreeNode>>()
        };
    
    private static ResourceTreeItemData SetAllQueuesLoading(ResourceTreeItemData node, QueuesTreeNode target, bool isLoading)
    {
        if (node.Value is QueuesTreeNode q && q.ConnectionConfig == target.ConnectionConfig)
            return new ResourceTreeItemData
            {
                Text = node.Text, Value = q with { IsLoading = isLoading }, Icon = node.Icon, IconColor = node.IconColor,
                EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
                Expandable = node.Expandable, Selected = node.Selected,
                IsReadonly =  node.IsReadonly,
                Children = node.Children?
                    .OfType<ResourceTreeItemData>()
                    .Select(child => child.Value is QueueTreeNode ? SetLoadingOnItem(child, isLoading) : child)
                    .ToList<TreeItemData<ResourceTreeNode>>()
            };

        if (node.Children is null)
            return node;

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text, Value = node.Value, Icon = node.Icon, IconColor = node.IconColor,
            EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
            Expandable = node.Expandable, Selected = node.Selected,
            Children = node.Children.OfType<ResourceTreeItemData>()
                .Select(child => SetAllQueuesLoading(child, target, isLoading))
                .ToList<TreeItemData<ResourceTreeNode>>()
        };
    }

    private static ResourceTreeItemData ReplaceAllQueueNodes(
        ResourceTreeItemData node, QueuesTreeNode target, IReadOnlyCollection<QueueTreeNode> updatedNodes)
    {
        if (node.Value is QueuesTreeNode q && q.ConnectionConfig == target.ConnectionConfig)
            return new ResourceTreeItemData
            {
                Text = node.Text, Value = q with { IsLoading = false }, Icon = node.Icon, IconColor = node.IconColor,
                EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
                Expandable = node.Expandable, Selected = node.Selected,
                IsReadonly = node.IsReadonly,
                Children = updatedNodes
                    .Select(qn =>
                    {
                        var existing = node.Children?.OfType<ResourceTreeItemData>()
                            .FirstOrDefault(c => c.Value is QueueTreeNode eq && eq.Resource.Url == qn.Resource.Url);
                        return ReplaceQueueNodeInItem(existing ?? new ResourceTreeItemData { Expandable = false }, qn);
                    })
                    .ToList<TreeItemData<ResourceTreeNode>>()
            };

        if (node.Children is null)
            return node;

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text, Value = node.Value, Icon = node.Icon, IconColor = node.IconColor,
            EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
            Expandable = node.Expandable, Selected = node.Selected,
            Children = node.Children.OfType<ResourceTreeItemData>()
                .Select(child => ReplaceAllQueueNodes(child, target, updatedNodes))
                .ToList<TreeItemData<ResourceTreeNode>>()
        };
    }

    private static ResourceTreeItemData SetAllTopicsLoading(ResourceTreeItemData node, TopicsTreeNode target, bool isLoading)
    {
        if (node.Value is TopicsTreeNode t && t.ConnectionConfig == target.ConnectionConfig)
            return new ResourceTreeItemData
            {
                Text = node.Text, Value = t with { IsLoading = isLoading }, Icon = node.Icon, IconColor = node.IconColor,
                EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
                Expandable = node.Expandable, Selected = node.Selected,
                IsReadonly =  node.IsReadonly,
                Children = node.Children?
                    .OfType<ResourceTreeItemData>()
                    .Select(child => child.Value is TopicTreeNode ? SetLoadingOnTopicItem(child, isLoading) : child)
                    .ToList<TreeItemData<ResourceTreeNode>>()
            };

        if (node.Children is null)
            return node;

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text, Value = node.Value, Icon = node.Icon, IconColor = node.IconColor,
            EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
            Expandable = node.Expandable, Selected = node.Selected,
            Children = node.Children.OfType<ResourceTreeItemData>()
                .Select(child => SetAllTopicsLoading(child, target, isLoading))
                .ToList<TreeItemData<ResourceTreeNode>>()
        };
    }

    private static ResourceTreeItemData ReplaceAllTopicNodes(
        ResourceTreeItemData node, TopicsTreeNode target, IReadOnlyCollection<TopicTreeNode> updatedNodes)
    {
        if (node.Value is TopicsTreeNode t && t.ConnectionConfig == target.ConnectionConfig)
            return new ResourceTreeItemData
            {
                Text = node.Text, Value = t with { IsLoading = false }, Icon = node.Icon, IconColor = node.IconColor,
                EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
                Expandable = node.Expandable, Selected = node.Selected,
                IsReadonly =  node.IsReadonly,
                Children = updatedNodes
                    .Select(tn =>
                    {
                        var existing = node.Children?.OfType<ResourceTreeItemData>()
                            .FirstOrDefault(c => c.Value is TopicTreeNode et && et.Resource.Url == tn.Resource.Url);
                        return ReplaceTopicNodeInItem(existing ?? new ResourceTreeItemData
                        {
                            Icon = Icons.Material.Filled.Topic,
                            IconColor = Color.Secondary,
                            Expandable = tn.Resource.Subscriptions.Count > 0
                        }, tn);
                    })
                    .ToList<TreeItemData<ResourceTreeNode>>()
            };

        if (node.Children is null)
            return node;

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text, Value = node.Value, Icon = node.Icon, IconColor = node.IconColor,
            EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
            Expandable = node.Expandable, Selected = node.Selected,
            Children = node.Children.OfType<ResourceTreeItemData>()
                .Select(child => ReplaceAllTopicNodes(child, target, updatedNodes))
                .ToList<TreeItemData<ResourceTreeNode>>()
        };
    }

    private static ResourceTreeItemData SetSubscriptionNodeLoading(ResourceTreeItemData node, SubscriptionTreeNode target)
    {
        if (node.Value is SubscriptionTreeNode s && s.Resource.Url == target.Resource.Url)
            return SetLoadingOnItem(node, true);

        if (node.Children is null)
            return node;

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text, Value = node.Value, Icon = node.Icon, IconColor = node.IconColor,
            EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
            Expandable = node.Expandable, Selected = node.Selected,
            Children = node.Children.OfType<ResourceTreeItemData>()
                .Select(child => SetSubscriptionNodeLoading(child, target))
                .ToList<TreeItemData<ResourceTreeNode>>()
        };
    }

    private static ResourceTreeItemData ClearSubscriptionNodeLoading(ResourceTreeItemData node, SubscriptionTreeNode target)
    {
        if (node.Value is SubscriptionTreeNode s && s.Resource.Url == target.Resource.Url)
            return SetLoadingOnItem(node, false);

        if (node.Children is null)
            return node;

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text, Value = node.Value, Icon = node.Icon, IconColor = node.IconColor,
            EndIcon = node.EndIcon, EndIconColor = node.EndIconColor, Expanded = node.Expanded,
            Expandable = node.Expandable, Selected = node.Selected,
            Children = node.Children.OfType<ResourceTreeItemData>()
                .Select(child => ClearSubscriptionNodeLoading(child, target))
                .ToList<TreeItemData<ResourceTreeNode>>()
        };
    }

    private static ResourceTreeItemData SetLoadingOnItem(ResourceTreeItemData node, bool isLoading) =>
        new()
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text,
            Value = node.Value switch
            {
                QueueTreeNode q => q with { IsLoading = isLoading },
                TopicTreeNode t => t with { IsLoading = isLoading },
                SubscriptionTreeNode s => s with { IsLoading = isLoading },
                var v => v
            },
            Icon = node.Icon,
            IconColor = node.IconColor,
            EndIcon = node.EndIcon,
            EndIconColor = node.EndIconColor,
            Expanded = node.Expanded,
            Expandable = node.Expandable,
            Selected = node.Selected,
            Children = node.Children
        };

    private static ResourceTreeItemData ReplaceQueueNode(ResourceTreeItemData node, QueueTreeNode updatedNode)
    {
        if (node.Value is QueueTreeNode q && q.Resource.Url == updatedNode.Resource.Url)
            return ReplaceQueueNodeInItem(node, updatedNode);

        if (node.Children is null)
            return node;

        var updatedChildren = node.Children
            .OfType<ResourceTreeItemData>()
            .Select(child => ReplaceQueueNode(child, updatedNode))
            .ToList();

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text,
            Value = node.Value,
            Icon = node.Icon,
            IconColor = node.IconColor,
            EndIcon = node.EndIcon,
            EndIconColor = node.EndIconColor,
            Expanded = node.Expanded,
            Expandable = node.Expandable,
            Selected = node.Selected,
            Children = updatedChildren
        };
    }

    private static ResourceTreeItemData ReplaceQueueNodeInItem(ResourceTreeItemData node, QueueTreeNode updatedNode) =>
        new()
        {
            IsReadonly = node.IsReadonly,
            Text = updatedNode.Resource.Name,
            Value = updatedNode,
            Icon = node.Icon,
            IconColor = node.IconColor,
            EndIcon = node.EndIcon,
            EndIconColor = node.EndIconColor,
            Expanded = node.Expanded,
            Expandable = node.Expandable,
            Selected = node.Selected,
            Children = node.Children
        };

    private static ResourceTreeItemData ReplaceTopicNode(ResourceTreeItemData node, TopicTreeNode updatedNode)
    {
        if (node.Value is TopicTreeNode t && t.Resource.Url == updatedNode.Resource.Url)
            return ReplaceTopicNodeInItem(node, updatedNode);

        if (node.Children is null)
            return node;

        var updatedChildren = node.Children
            .OfType<ResourceTreeItemData>()
            .Select(child => ReplaceTopicNode(child, updatedNode))
            .ToList();

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text,
            Value = node.Value,
            Icon = node.Icon,
            IconColor = node.IconColor,
            EndIcon = node.EndIcon,
            EndIconColor = node.EndIconColor,
            Expanded = node.Expanded,
            Expandable = node.Expandable,
            Selected = node.Selected,
            Children = updatedChildren
        };
    }

    private static ResourceTreeItemData ReplaceTopicNodeInItem(ResourceTreeItemData node, TopicTreeNode updatedNode)
    {
        var subscriptionItems = updatedNode.Resource.Subscriptions
            .Select(sub => new ResourceTreeItemData
            {
                Text = sub.Name,
                Value = new SubscriptionTreeNode(updatedNode.ConnectionName, sub, updatedNode.ConnectionConfig),
                Expandable = false
            })
            .ToList<TreeItemData<ResourceTreeNode>>();

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = updatedNode.Resource.Name,
            Value = updatedNode,
            Icon = node.Icon,
            IconColor = node.IconColor,
            EndIcon = node.EndIcon,
            EndIconColor = node.EndIconColor,
            Expanded = node.Expanded,
            Expandable = node.Expandable,
            Selected = node.Selected,
            Children = subscriptionItems.Count > 0 ? subscriptionItems : node.Children
        };
    }

    private static ResourceTreeItemData ReplaceSubscriptionNode(ResourceTreeItemData node, SubscriptionTreeNode updatedNode)
    {
        if (node.Value is SubscriptionTreeNode s && s.Resource.Url == updatedNode.Resource.Url)
            return ReplaceSubscriptionNodeInItem(node, updatedNode);

        if (node.Children is null)
            return node;

        var updatedChildren = node.Children
            .OfType<ResourceTreeItemData>()
            .Select(child => ReplaceSubscriptionNode(child, updatedNode))
            .ToList();

        return new ResourceTreeItemData
        {
            IsReadonly = node.IsReadonly,
            Text = node.Text,
            Value = node.Value,
            Icon = node.Icon,
            IconColor = node.IconColor,
            EndIcon = node.EndIcon,
            EndIconColor = node.EndIconColor,
            Expanded = node.Expanded,
            Expandable = node.Expandable,
            Selected = node.Selected,
            Children = updatedChildren
        };
    }

    private static ResourceTreeItemData ReplaceSubscriptionNodeInItem(ResourceTreeItemData node, SubscriptionTreeNode updatedNode) =>
        new()
        {
            IsReadonly = node.IsReadonly,
            Text = updatedNode.Resource.Name,
            Value = updatedNode,
            Icon = node.Icon,
            IconColor = node.IconColor,
            EndIcon = node.EndIcon,
            EndIconColor = node.EndIconColor,
            Expanded = node.Expanded,
            Expandable = node.Expandable,
            Selected = node.Selected,
            Children = node.Children
        };
}
