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

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (SearchPhrase != _localSearchPhrase)
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
        if (SearchQueryParser.Parse(_localSearchPhrase).IsEmpty)
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

            return;
        }

        switch (node)
        {
            case QueuesTreeNode q:
                foreach (var queueNode in GetFilteredNodes<QueueTreeNode>(q.ConnectionName))
                    _dispatcher.Dispatch(new RefreshQueueAction(queueNode));
                break;
            case TopicsTreeNode t:
                foreach (var subscriptionNode in GetFilteredNodes<SubscriptionTreeNode>(t.ConnectionName))
                    _dispatcher.Dispatch(new RefreshSubscriptionAction(subscriptionNode));
                break;
        }
    }

    private IEnumerable<TNode> GetFilteredNodes<TNode>(string connectionName)
        where TNode : ResourceTreeNode =>
        FlattenNodes(FilteredResources)
            .OfType<TNode>()
            .Where(n => GetConnectionName(n) == connectionName)
            .Distinct();

    private static string? GetConnectionName(ResourceTreeNode node) => node switch
    {
        NamespaceTreeNode n => n.ConnectionName,
        QueuesTreeNode n => n.ConnectionName,
        TopicsTreeNode n => n.ConnectionName,
        QueueTreeNode n => n.ConnectionName,
        TopicTreeNode n => n.ConnectionName,
        SubscriptionTreeNode n => n.ConnectionName,
        _ => null
    };

    private static IEnumerable<ResourceTreeNode> FlattenNodes(IEnumerable<ResourceTreeItemData> items)
    {
        foreach (var item in items)
        {
            if (item.Value is not null)
                yield return item.Value;

            if (item.Children is null)
                continue;

            foreach (var nested in FlattenNodes(item.Children.OfType<ResourceTreeItemData>()))
                yield return nested;
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
        if (searchPhrase == _localSearchPhrase) return;
        _localSearchPhrase = searchPhrase;
        _dispatcher.Dispatch(new SetSearchPhraseAction(searchPhrase));
        StateHasChanged();
    }

    private void OnSuggestionSelected(string? value) => OnTextChanged(value);

    private Task<IEnumerable<string>> SuggestSearchPhrases(string? value, CancellationToken ct)
    {
        var endsWithSpace = value?.EndsWith(' ') == true;
        var parts = (value ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var lastToken = !endsWithSpace && parts.Length > 0 ? parts[^1] : "";
        var completedParts = !endsWithSpace && parts.Length > 0 ? parts[..^1] : parts;
        var prefix = completedParts.Length > 0 ? string.Join(" ", completedParts) + " " : "";

        IEnumerable<string> suggestions;

        if (lastToken.StartsWith("namespace:", StringComparison.OrdinalIgnoreCase))
        {
            var partial = lastToken["namespace:".Length..];
            suggestions = Resources
                .Where(r => !string.IsNullOrEmpty(r.Text) &&
                            r.Text.Contains(partial, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals("namespace:" + r.Text, lastToken, StringComparison.OrdinalIgnoreCase))
                .Select(r => prefix + "namespace:" + r.Text);
        }
        else
        {
            suggestions = SourceArray.Where(p => p.StartsWith(lastToken, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(p, lastToken, StringComparison.OrdinalIgnoreCase))
                .Select(p => prefix + p);
        }

        return Task.FromResult(suggestions.Distinct().Take(10));
    }

    private static string GetSuggestionIcon(string token) =>
        token.StartsWith("namespace:", StringComparison.OrdinalIgnoreCase)
            ? Icons.Material.Filled.Cloud
            : Icons.Material.Filled.AllInbox;

    private List<ResourceTreeItemData> FilteredResources =>
        ResourceTreeFilter.Filter(Resources, SearchQueryParser.Parse(_localSearchPhrase));

    private static readonly string[] SourceArray = ["namespace:", "has:dlq"];
}
