using Fluxor;
using Messentra.Domain;
using Messentra.Features.Layout.State;
using Messentra.Features.Settings.Connections.GetConnections;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
    private readonly IDialogService _dialogService;

    public NamespaceTree(NavigationManager navigationManager, IDispatcher dispatcher, IDialogService dialogService)
    {
        _navigationManager = navigationManager;
        _dispatcher = dispatcher;
        _dialogService = dialogService;
    }

    private void OnExpandedChanged(ResourceTreeItemData item, bool expanded) =>
        _dispatcher.Dispatch(new ToggleExpandedAction(GetNodeKey(item.Value), expanded));

    private static string GetNodeKey(ResourceTreeNode? node) => node switch
    {
        NamespaceTreeNode n => $"ns:{n.ConnectionName}",
        FoldersTreeNode n => $"folders:{n.ConnectionName}",
        FolderTreeNode n => $"folder:{n.FolderId}",
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

    private void OnCancelFetchResourcesClicked(string connectionName)
    {
        _dispatcher.Dispatch(new CancelFetchResourcesAction(connectionName));
    }

    private async Task OnAddFolderClicked(FoldersTreeNode node)
    {
        var dialog = await _dialogService.ShowAsync<NewFolderDialog>(
            "New folder",
            new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, FullWidth = true });

        var result = await dialog.Result;
        if (result is { Canceled: false, Data: string folderName } && !string.IsNullOrWhiteSpace(folderName))
        {
            _dispatcher.Dispatch(new CreateFolderAction(
                node.ConnectionId,
                node.ConnectionName,
                node.ConnectionConfig,
                folderName));
        }
    }

    private async Task OnContextRenameFolder(FolderTreeNode folder)
    {
        var parameters = new DialogParameters<NewFolderDialog> { { x => x.InitialName, folder.Name } };
        var dialog = await _dialogService.ShowAsync<NewFolderDialog>("Rename folder", parameters,
            new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, FullWidth = true });
        var result = await dialog.Result;
        if (result is { Canceled: false, Data: string newName } && !string.IsNullOrWhiteSpace(newName))
            _dispatcher.Dispatch(new RenameFolderAction(folder.FolderId, folder.ConnectionId, folder.ConnectionName, newName));
    }

    private async Task OnContextNewFolder(FolderTreeNode folder) =>
        await OnAddFolderClicked(new FoldersTreeNode(folder.ConnectionId, folder.ConnectionName, folder.ConnectionConfig));

    private void OnContextDeleteFolder(FolderTreeNode folder) =>
        _dispatcher.Dispatch(new DeleteFolderAction(folder.FolderId, folder.ConnectionId, folder.ConnectionName));

    private void OnContextAddToFolder(ResourceTreeNode resource, FolderTreeNode targetFolder)
    {
        var resourceUrl = GetResourceUrl(resource);
        if (resourceUrl is null) return;
        _dispatcher.Dispatch(new AddResourceToFolderAction(
            targetFolder.FolderId, targetFolder.ConnectionId, targetFolder.ConnectionName, resourceUrl));
    }

    private async Task OnContextAddToNewFolder(ResourceTreeNode resource)
    {
        var resourceUrl = GetResourceUrl(resource);
        if (resourceUrl is null) return;

        var connectionName = GetConnectionName(resource);
        if (connectionName is null) return;

        var nsItem = Resources.FirstOrDefault(r => r.Value is NamespaceTreeNode n && n.ConnectionName == connectionName);
        var foldersNode = nsItem?.Children?.OfType<ResourceTreeItemData>()
            .Select(c => c.Value)
            .OfType<FoldersTreeNode>()
            .FirstOrDefault();
        if (foldersNode is null) return;

        var dialog = await _dialogService.ShowAsync<NewFolderDialog>(
            "New folder",
            new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, FullWidth = true });
        var result = await dialog.Result;
        if (result is not { Canceled: false, Data: string folderName } || string.IsNullOrWhiteSpace(folderName))
            return;

        _dispatcher.Dispatch(new CreateFolderAndAddResourceAction(
            foldersNode.ConnectionId, foldersNode.ConnectionName, foldersNode.ConnectionConfig,
            folderName, resourceUrl));
    }

    private void OnContextRemoveFromFolder(ResourceTreeNode resource, FolderTreeNode folder)
    {
        var resourceUrl = GetResourceUrl(resource);
        if (resourceUrl is null) return;
        _dispatcher.Dispatch(new RemoveResourceFromFolderAction(
            folder.FolderId, folder.ConnectionId, folder.ConnectionName, resourceUrl));
    }

    private void OnContextRefreshResource(ResourceTreeNode node)
    {
        switch (node)
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
        }
    }

    private static string? GetResourceUrl(ResourceTreeNode? node) => node switch
    {
        QueueTreeNode q => q.Resource.Url,
        TopicTreeNode t => t.Resource.Url,
        SubscriptionTreeNode s => s.Resource.Url,
        _ => null
    };

    private IEnumerable<FolderTreeNode> GetAllFolderNodes(string connectionName) =>
        GetAllNodes<FolderTreeNode>(connectionName);

    private void ItemSelected(ResourceTreeItemData presenter, bool selected)
    {
        if (!selected)
            return;

        _dispatcher.Dispatch(new SelectResourceAction(presenter.Value!));
    }

    private async Task OnTreeItemRightClick(ResourceTreeItemData presenter, MenuContext menuContext, MouseEventArgs args)
    {
        if (presenter is { IsReadonly: false, Value: not null })
            _dispatcher.Dispatch(new SelectResourceAction(presenter.Value));

        await menuContext.ToggleAsync(args);
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

        _dispatcher.Dispatch(new FetchResourcesAction(connection.Id, connection.Name, connectionConfig));
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
                var filteredQueueNodes = GetFilteredNodes<QueueTreeNode>(q.ConnectionName).ToList();
                var allQueueNodes = GetAllNodes<QueueTreeNode>(q.ConnectionName).ToList();

                if (ContainsSameNodes(filteredQueueNodes, allQueueNodes))
                {
                    _dispatcher.Dispatch(new RefreshQueuesAction(q));
                    break;
                }

                foreach (var queueNode in filteredQueueNodes)
                    _dispatcher.Dispatch(new RefreshQueueAction(queueNode));
                break;

            case TopicsTreeNode t:
                var filteredTopicNodes = GetFilteredNodes<TopicTreeNode>(t.ConnectionName).ToList();
                var allTopicNodes = GetAllNodes<TopicTreeNode>(t.ConnectionName).ToList();

                if (ContainsSameNodes(filteredTopicNodes, allTopicNodes))
                {
                    _dispatcher.Dispatch(new RefreshTopicsAction(t));
                    break;
                }

                foreach (var topicNode in filteredTopicNodes)
                    _dispatcher.Dispatch(new RefreshTopicAction(topicNode));
                break;
        }
    }

    private IEnumerable<TNode> GetAllNodes<TNode>(string connectionName)
        where TNode : ResourceTreeNode =>
        FlattenNodes(Resources)
            .OfType<TNode>()
            .Where(n => GetConnectionName(n) == connectionName)
            .Distinct();

    private IEnumerable<TNode> GetFilteredNodes<TNode>(string connectionName)
        where TNode : ResourceTreeNode =>
        FlattenNodes(FilteredResources)
            .OfType<TNode>()
            .Where(n => GetConnectionName(n) == connectionName)
            .Distinct();

    private static bool ContainsSameNodes<TNode>(IEnumerable<TNode> filteredNodes, IEnumerable<TNode> allNodes)
        where TNode : ResourceTreeNode
    {
        var filteredNodeKeys = filteredNodes.Select(GetNodeKey).ToHashSet(StringComparer.Ordinal);
        var allNodeKeys = allNodes.Select(GetNodeKey).ToHashSet(StringComparer.Ordinal);
        return filteredNodeKeys.SetEquals(allNodeKeys);
    }

    private static string? GetConnectionName(ResourceTreeNode node) => node switch
    {
        NamespaceTreeNode n => n.ConnectionName,
        FoldersTreeNode n => n.ConnectionName,
        FolderTreeNode n => n.ConnectionName,
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
