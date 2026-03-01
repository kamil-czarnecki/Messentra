using Fluxor;
using Messentra.Domain;
using Messentra.Features.Layout.State;
using Messentra.Features.Settings.Connections.GetConnections;
using Microsoft.AspNetCore.Components;

namespace Messentra.Features.Explorer.Resources.Components;

public partial class NamespaceTree
{
    private bool _isFocused;

    [Parameter, EditorRequired]
    public List<ResourceTreeItemData> Resources { get; init; } = [];
    [Parameter, EditorRequired]
    public List<ConnectionDto> Connections { get; init; } = [];
    [Parameter]
    public ResourceTreeItemData? SelectedResource { get; init; }

    private readonly NavigationManager _navigationManager;
    private readonly IDispatcher _dispatcher;

    public NamespaceTree(NavigationManager navigationManager, IDispatcher dispatcher)
    {
        _navigationManager = navigationManager;
        _dispatcher = dispatcher;
    }

    private void OpenConnections()
    {
        _navigationManager.NavigateTo("/options");
    }

    private void Disconnect(ResourceTreeItemData node)
    {
        _dispatcher.Dispatch(new DisconnectResourceAction(node));
        _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
            node.Text!,
            "Info",
            "Namespace disconnected.",
            DateTime.Now)));
    }

    private void ItemSelected(ResourceTreeItemData presenter, bool selected)
    {
        if (!selected)
            return;

        _dispatcher.Dispatch(new SelectResourceAction(presenter));
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

        switch (SelectedResource?.Value)
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
}
