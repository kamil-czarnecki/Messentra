using Fluxor;
using Messentra.Domain;
using Messentra.Features.Layout.State;
using Messentra.Features.Settings.Connections.GetConnections;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components;

public partial class NamespaceTree
{
    private bool _isFocused;

    [Parameter, EditorRequired]
    public List<ResourceTreeItemData> Resources { get; init; } = [];
    [Parameter, EditorRequired]
    public List<ConnectionDto> Connections { get; init; } = [];
    [Parameter]
    public ResourceTreeNode? SelectedResource { get; init; }
    [Parameter]
    public string? SearchPhrase { get; set; }

    private string? _localSearchPhrase;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _localSearchPhrase = SearchPhrase;
    }


    private readonly NavigationManager _navigationManager;
    private readonly IDispatcher _dispatcher;

    public NamespaceTree(NavigationManager navigationManager, IDispatcher dispatcher)
    {
        _navigationManager = navigationManager;
        _dispatcher = dispatcher;
    }

    private void OnExpandedChanged(ResourceTreeItemData item, bool expanded) =>
        _dispatcher.Dispatch(new ToggleExpandedAction(GetNodeKey(item.Value), expanded));

    private static string GetNodeKey(ResourceTreeNode? node) => node switch
    {
        NamespaceTreeNode n => $"ns:{n.ConnectionName}",
        QueuesTreeNode n => $"queues:{n.ConnectionName}",
        TopicsTreeNode n => $"topics:{n.ConnectionName}",
        QueueTreeNode n => $"queue:{n.Resource.Url}",
        TopicTreeNode n => $"topic:{n.Resource.Url}",
        SubscriptionTreeNode n => $"sub:{n.Resource.Url}",
        _ => string.Empty
    };

    private void OpenConnections()
    {
        _navigationManager.NavigateTo("/options");
    }

    private void Disconnect(ResourceTreeItemData node)
    {
        var connectionName = (node.Value as NamespaceTreeNode)!.ConnectionName;
        _dispatcher.Dispatch(new DisconnectResourceAction(connectionName));
        _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
            connectionName,
            "Info",
            "Namespace disconnected.",
            DateTime.Now)));
    }

    private void ItemSelected(ResourceTreeItemData presenter, bool selected)
    {
        if (!selected)
            return;

        _dispatcher.Dispatch(new SelectResourceAction(presenter.Value!));
    }

    private void SelectConnection(ConnectionDto connection)
    {
        var connectionConfig = connection.ConnectionConfig.ConnectionType switch
        {
            ConnectionType.ConnectionString =>
                ConnectionConfig.CreateConnectionString(connection.ConnectionConfig.ConnectionString!),
            ConnectionType.EntraId => ConnectionConfig.CreateEntraId(
                connection.ConnectionConfig.Namespace!,
                connection.ConnectionConfig.TenantId!,
                connection.ConnectionConfig.ClientId!),
            _ => throw new NotSupportedException(
                $"Unsupported connection type: {connection.ConnectionConfig.ConnectionType}")
        };

        _dispatcher.Dispatch(new FetchResourcesAction(connection.Name, connectionConfig));
    }

    private static string? GetMessageCountText(ResourceTreeNode? node) => node switch
    {
        NamespaceTreeNode => null,
        QueueTreeNode q => $"{q.Resource.Overview.MessageInfo.Active}/{q.Resource.Overview.MessageInfo.DeadLetter}",
        TopicTreeNode => null,
        SubscriptionTreeNode s => $"{s.Resource.Overview.MessageInfo.Active}/{s.Resource.Overview.MessageInfo.DeadLetter}",
        _ => null
    };

    private void OnRefreshClicked(ResourceTreeNode node)
    {
        switch (node)
        {
            case QueuesTreeNode q:
                _dispatcher.Dispatch(new RefreshQueuesAction(q));
                break;
            case TopicsTreeNode t:
                _dispatcher.Dispatch(new RefreshTopicsAction(t));
                break;
        }
    }

    private void RefreshHotkeyClicked()
    {
        if (!_isFocused)
            return;

        switch (SelectedResource)
        {
            case QueueTreeNode q:
                _dispatcher.Dispatch(new RefreshQueueAction(q));
                break;
            case TopicTreeNode t:
                _dispatcher.Dispatch(new RefreshTopicAction(t));
                break;
            case SubscriptionTreeNode s:
                _dispatcher.Dispatch(new RefreshSubscriptionAction(s));
                break;
            case QueuesTreeNode q:
                _dispatcher.Dispatch(new RefreshQueuesAction(q));
                break;
            case TopicsTreeNode t:
                _dispatcher.Dispatch(new RefreshTopicsAction(t));
                break;
        }
    }
    
    private void OnTextChanged(string? searchPhrase)
    {
        _localSearchPhrase = searchPhrase;
        _dispatcher.Dispatch(new SetSearchPhraseAction(searchPhrase));
        StateHasChanged();
    }

    private List<ResourceTreeItemData> FilteredResources =>
        string.IsNullOrEmpty(_localSearchPhrase)
            ? Resources
            : Resources
                .Select(FilterNamespace)
                .OfType<ResourceTreeItemData>()
                .ToList();

    private ResourceTreeItemData? FilterNamespace(ResourceTreeItemData ns)
    {
        if (ns.Children is null) return null;

        var filteredGroups = ns.Children
            .OfType<ResourceTreeItemData>()
            .Select(FilterGroup)
            .OfType<ResourceTreeItemData>()
            .ToList<TreeItemData<ResourceTreeNode>>();

        if (filteredGroups.Count == 0) return null;

        return new ResourceTreeItemData
        {
            Text = ns.Text, Value = ns.Value, Icon = ns.Icon, IconColor = ns.IconColor,
            IsReadonly = ns.IsReadonly, Expandable = true, Expanded = true,
            Children = filteredGroups
        };
    }

    private ResourceTreeItemData? FilterGroup(ResourceTreeItemData group)
    {
        var filteredItems = group.Children?
            .OfType<ResourceTreeItemData>()
            .Select(FilterItem)
            .OfType<ResourceTreeItemData>()
            .ToList<TreeItemData<ResourceTreeNode>>();

        if (filteredItems is null || filteredItems.Count == 0)
            return null;

        return new ResourceTreeItemData
        {
            Text = group.Text, Value = group.Value, Icon = group.Icon, IconColor = group.IconColor,
            IsReadonly = group.IsReadonly, Expandable = true, Expanded = true,
            Children = filteredItems
        };
    }

    private ResourceTreeItemData? FilterItem(ResourceTreeItemData item)
    {
        var nameMatches = item.Text?.Contains(_localSearchPhrase!, StringComparison.OrdinalIgnoreCase) == true;

        if (item.Children is not { Count: > 0 } children)
            return nameMatches ? item : null;
        
        var filteredSubs = children
            .OfType<ResourceTreeItemData>()
            .Where(s => s.Text?.Contains(_localSearchPhrase!, StringComparison.OrdinalIgnoreCase) == true)
            .ToList<TreeItemData<ResourceTreeNode>>();

        if (!nameMatches && filteredSubs.Count == 0)
            return null;

        return new ResourceTreeItemData
        {
            Text = item.Text, Value = item.Value, Icon = item.Icon, IconColor = item.IconColor,
            IsReadonly = item.IsReadonly, Expandable = true, Expanded = true, Selected = item.Selected,
            Children = nameMatches ? item.Children.ToList() : filteredSubs
        };

    }
}
