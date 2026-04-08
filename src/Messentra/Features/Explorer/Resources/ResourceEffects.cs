using Fluxor;
using Mediator;
using Messentra.Features.Explorer.Folders.AddResourceToFolder;
using Messentra.Features.Explorer.Folders.CreateFolder;
using Messentra.Features.Explorer.Folders.DeleteFolder;
using Messentra.Features.Explorer.Folders.GetFoldersByConnectionId;
using Messentra.Features.Explorer.Folders.ExportFolders;
using Messentra.Features.Explorer.Folders.ImportFolders;
using Messentra.Features.Explorer.Folders.RemoveResourceFromFolder;
using Messentra.Features.Explorer.Folders.RenameFolder;
using Messentra.Features.Explorer.Resources.Queues.GetAllQueueResources;
using Messentra.Features.Explorer.Resources.Queues.GetQueueResource;
using Messentra.Features.Explorer.Resources.Subscriptions.GetSubscriptionResource;
using Messentra.Features.Explorer.Resources.Topics.GetAllTopicResources;
using Messentra.Features.Explorer.Resources.Topics.GetTopicResource;
using Messentra.Features.Layout.State;

namespace Messentra.Features.Explorer.Resources;

public sealed class ResourceEffects
{
    private readonly IMediator _mediator;
    private readonly ILogger<ResourceEffects> _logger;
    private readonly Dictionary<string, CancellationTokenSource> _fetchResourcesCts = new();

    public ResourceEffects(IMediator mediator, ILogger<ResourceEffects> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleFetchResources(FetchResourcesAction action, IDispatcher dispatcher)
    {
        CancellationTokenSource? previousCts;
        CancellationTokenSource cts;

        lock (_fetchResourcesCts)
        {
            _fetchResourcesCts.TryGetValue(action.ConnectionName, out previousCts);
            cts = new CancellationTokenSource();
            _fetchResourcesCts[action.ConnectionName] = cts;
        }

        if (previousCts is not null)
        {
            await previousCts.CancelAsync();
            previousCts.Dispose();
        }

        try
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.ConnectionName,
                "Debug",
                "Fetching resources...",
                DateTime.Now)));
            var getQueues = _mediator.Send(new GetAllQueueResourcesQuery(action.ConnectionConfig), cts.Token).AsTask();
            var getTopics = _mediator.Send(new GetAllTopicResourcesQuery(action.ConnectionConfig), cts.Token).AsTask();
            var getFolders = _mediator.Send(new GetFoldersByConnectionIdQuery(action.ConnectionId), cts.Token).AsTask();

            await Task.WhenAll(getQueues, getTopics, getFolders);

            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.ConnectionName,
                "Info",
                "Resources fetched successfully.",
                DateTime.Now)));
            dispatcher.Dispatch(new FetchResourcesSuccessAction(action.ConnectionId, action.ConnectionName, action.ConnectionConfig, getQueues.Result, getTopics.Result, getFolders.Result));
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.ConnectionName,
                "Info",
                "Fetching resources canceled.",
                DateTime.Now)));
            dispatcher.Dispatch(new FetchResourcesCanceledAction(action.ConnectionName));
        }
        catch (Exception ex)
        {
            var errorSummary = BuildExceptionSummary(ex);
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.ConnectionName,
                "Error",
                $"Fetching resources failed: {errorSummary}",
                DateTime.Now)));
            dispatcher.Dispatch(new FetchResourcesFailureAction(action.ConnectionName, errorSummary));
            _logger.LogError(ex, "Error fetching resources for connection '{ConnectionName}'", action.ConnectionName);
        }
        finally
        {
            lock (_fetchResourcesCts)
            {
                if (_fetchResourcesCts.TryGetValue(action.ConnectionName, out var current) && ReferenceEquals(current, cts))
                    _fetchResourcesCts.Remove(action.ConnectionName);
            }

            cts.Dispose();
        }
    }

    [EffectMethod]
    public Task HandleCancelFetchResources(CancelFetchResourcesAction action, IDispatcher dispatcher)
    {
        CancellationTokenSource? cts;

        lock (_fetchResourcesCts)
            _fetchResourcesCts.TryGetValue(action.ConnectionName, out cts);

        if (cts is null)
            return Task.CompletedTask;

        dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
            action.ConnectionName,
            "Debug",
            "Canceling resource fetch...",
            DateTime.Now)));

        cts.Cancel();
        return Task.CompletedTask;
    }

    [EffectMethod]
    public async Task HandleRefreshQueue(RefreshQueueAction action, IDispatcher dispatcher)
    {
        try
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName,
                "Debug",
                $"Refreshing '{action.Node.Resource.Name}' queue...",
                DateTime.Now)));

            var result = await _mediator.Send(new GetQueueResourceQuery(action.Node.Resource.Name, action.Node.ConnectionConfig));

            result.Switch(
                queue =>
                {
                    dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                        action.Node.ConnectionName,
                        "Info",
                        $"'{action.Node.Resource.Name}' queue refreshed successfully.",
                        DateTime.Now)));
                    dispatcher.Dispatch(new RefreshQueueSuccessAction(new QueueTreeNode(
                        action.Node.ConnectionName,
                        queue,
                        action.Node.ConnectionConfig)));
                },
                _ =>
                {
                    dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                        action.Node.ConnectionName,
                        "Error",
                        $"'{action.Node.Resource.Name}' queue not found.",
                        DateTime.Now)));
                    dispatcher.Dispatch(new RefreshQueueFailureAction(action.Node, "Queue not found."));
                });
        }
        catch (Exception ex)
        {
            var errorSummary = BuildExceptionSummary(ex);
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName,
                "Error",
                $"Refreshing '{action.Node.Resource.Name}' queue failed: {errorSummary}",
                DateTime.Now)));
            dispatcher.Dispatch(new RefreshQueueFailureAction(action.Node, errorSummary));
            _logger.LogError(ex, "Error refreshing queue '{QueueName}' for connection '{ConnectionName}'", action.Node.Resource.Name, action.Node.ConnectionName);
        }
    }

    [EffectMethod]
    public async Task HandleRefreshTopic(RefreshTopicAction action, IDispatcher dispatcher)
    {
        try
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName,
                "Debug",
                $"Refreshing '{action.Node.Resource.Name}' topic...",
                DateTime.Now)));

            var result = await _mediator.Send(new GetTopicResourceQuery(action.Node.Resource.Name, action.Node.ConnectionConfig));

            result.Switch(
                topic =>
                {
                    dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                        action.Node.ConnectionName,
                        "Info",
                        $"'{action.Node.Resource.Name}' topic refreshed successfully.",
                        DateTime.Now)));
                    dispatcher.Dispatch(new RefreshTopicSuccessAction(new TopicTreeNode(
                        action.Node.ConnectionName,
                        topic,
                        action.Node.ConnectionConfig)));
                },
                _ =>
                {
                    dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                        action.Node.ConnectionName,
                        "Error",
                        $"'{action.Node.Resource.Name}' topic not found.",
                        DateTime.Now)));
                    dispatcher.Dispatch(new RefreshTopicFailureAction(action.Node, "Topic not found."));
                });
        }
        catch (Exception ex)
        {
            var errorSummary = BuildExceptionSummary(ex);
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName,
                "Error",
                $"Refreshing '{action.Node.Resource.Name}' topic failed: {errorSummary}",
                DateTime.Now)));
            dispatcher.Dispatch(new RefreshTopicFailureAction(action.Node, errorSummary));
            _logger.LogError(ex, "Error refreshing topic '{TopicName}' for connection '{ConnectionName}'", action.Node.Resource.Name, action.Node.ConnectionName);
        }
    }

    [EffectMethod]
    public async Task HandleRefreshSubscription(RefreshSubscriptionAction action, IDispatcher dispatcher)
    {
        try
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName,
                "Debug",
                $"Refreshing '{action.Node.Resource.TopicName}/{action.Node.Resource.Name}' subscription...",
                DateTime.Now)));

            var result = await _mediator.Send(new GetSubscriptionResourceQuery(
                action.Node.Resource.TopicName,
                action.Node.Resource.Name,
                action.Node.ConnectionConfig));

            result.Switch(
                subscription =>
                {
                    dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                        action.Node.ConnectionName,
                        "Info",
                        $"'{action.Node.Resource.TopicName}/{action.Node.Resource.Name}' subscription refreshed successfully.",
                        DateTime.Now)));
                    dispatcher.Dispatch(new RefreshSubscriptionSuccessAction(new SubscriptionTreeNode(
                        action.Node.ConnectionName,
                        subscription,
                        action.Node.ConnectionConfig)));
                },
                _ =>
                {
                    dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                        action.Node.ConnectionName,
                        "Error",
                        $"'{action.Node.Resource.TopicName}/{action.Node.Resource.Name}' subscription not found.",
                        DateTime.Now)));
                    dispatcher.Dispatch(new RefreshSubscriptionFailureAction(action.Node, "Subscription not found."));
                });
        }
        catch (Exception ex)
        {
            var errorSummary = BuildExceptionSummary(ex);
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName,
                "Error",
                $"Refreshing '{action.Node.Resource.TopicName}/{action.Node.Resource.Name}' subscription failed: {errorSummary}",
                DateTime.Now)));
            dispatcher.Dispatch(new RefreshSubscriptionFailureAction(action.Node, errorSummary));
            _logger.LogError(ex, "Error refreshing subscription '{TopicName}/{SubscriptionName}' for connection '{ConnectionName}'", action.Node.Resource.TopicName, action.Node.Resource.Name, action.Node.ConnectionName);
        }
    }

    [EffectMethod]
    public async Task HandleRefreshQueues(RefreshQueuesAction action, IDispatcher dispatcher)
    {
        try
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName, "Debug", "Refreshing all queues...", DateTime.Now)));

            var queues = await _mediator.Send(new GetAllQueueResourcesQuery(action.Node.ConnectionConfig));

            var updatedNodes = queues
                .Select(q => new QueueTreeNode(action.Node.ConnectionName, q, action.Node.ConnectionConfig))
                .ToList();

            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName, "Info", $"All queues refreshed successfully ({updatedNodes.Count}).", DateTime.Now)));
            dispatcher.Dispatch(new RefreshQueuesSuccessAction(action.Node, updatedNodes));
        }
        catch (Exception ex)
        {
            var errorSummary = BuildExceptionSummary(ex);
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName, "Error", $"Refreshing queues failed: {errorSummary}", DateTime.Now)));
            dispatcher.Dispatch(new RefreshQueuesFailureAction(action.Node, errorSummary));
            _logger.LogError(ex, "Error refreshing queues for connection '{ConnectionName}'", action.Node.ConnectionName);
        }
    }

    [EffectMethod]
    public async Task HandleRefreshTopics(RefreshTopicsAction action, IDispatcher dispatcher)
    {
        try
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName, "Debug", "Refreshing all topics...", DateTime.Now)));

            var topics = await _mediator.Send(new GetAllTopicResourcesQuery(action.Node.ConnectionConfig));

            var updatedNodes = topics
                .Select(t => new TopicTreeNode(action.Node.ConnectionName, t, action.Node.ConnectionConfig))
                .ToList();

            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName, "Info", $"All topics refreshed successfully ({updatedNodes.Count}).", DateTime.Now)));
            dispatcher.Dispatch(new RefreshTopicsSuccessAction(action.Node, updatedNodes));
        }
        catch (Exception ex)
        {
            var errorSummary = BuildExceptionSummary(ex);
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName, "Error", $"Refreshing topics failed: {errorSummary}", DateTime.Now)));
            dispatcher.Dispatch(new RefreshTopicsFailureAction(action.Node, errorSummary));
            _logger.LogError(ex, "Error refreshing topics for connection '{ConnectionName}'", action.Node.ConnectionName);
        }
    }

    [EffectMethod]
    public async Task HandleCreateFolder(CreateFolderAction action, IDispatcher dispatcher)
    {
        try
        {
            var folderId = await _mediator.Send(new CreateFolderCommand(action.ConnectionId, action.Name));
            var node = new FolderTreeNode(folderId, action.ConnectionId, action.Name, action.ConnectionName, action.ConnectionConfig);
            var entry = new FolderEntry(node, new HashSet<string>());
            dispatcher.Dispatch(new CreateFolderSuccessAction(action.ConnectionId, action.ConnectionName, entry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create folder");
            dispatcher.Dispatch(new CreateFolderFailureAction(action.ConnectionName, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleCreateFolderAndAddResource(CreateFolderAndAddResourceAction action, IDispatcher dispatcher)
    {
        try
        {
            var folderId = await _mediator.Send(new CreateFolderCommand(action.ConnectionId, action.FolderName));
            var node = new FolderTreeNode(folderId, action.ConnectionId, action.FolderName, action.ConnectionName, action.ConnectionConfig);
            var entry = new FolderEntry(node, new HashSet<string>());
            dispatcher.Dispatch(new CreateFolderSuccessAction(action.ConnectionId, action.ConnectionName, entry));
            await _mediator.Send(new AddResourceToFolderCommand(folderId, action.ResourceUrl));
            dispatcher.Dispatch(new AddResourceToFolderSuccessAction(folderId, action.ConnectionId, action.ConnectionName, action.ResourceUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create folder and add resource");
            dispatcher.Dispatch(new CreateFolderFailureAction(action.ConnectionName, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleCreateFolderAndAddResources(CreateFolderAndAddResourcesAction action, IDispatcher dispatcher)
    {
        try
        {
            var folderId = await _mediator.Send(new CreateFolderCommand(action.ConnectionId, action.FolderName));
            var node = new FolderTreeNode(folderId, action.ConnectionId, action.FolderName, action.ConnectionName, action.ConnectionConfig);
            var entry = new FolderEntry(node, new HashSet<string>());
            dispatcher.Dispatch(new CreateFolderSuccessAction(action.ConnectionId, action.ConnectionName, entry));
            foreach (var url in action.ResourceUrls)
            {
                await _mediator.Send(new AddResourceToFolderCommand(folderId, url));
                dispatcher.Dispatch(new AddResourceToFolderSuccessAction(folderId, action.ConnectionId, action.ConnectionName, url));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create folder and add resources");
            dispatcher.Dispatch(new CreateFolderFailureAction(action.ConnectionName, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleRenameFolder(RenameFolderAction action, IDispatcher dispatcher)
    {
        try
        {
            await _mediator.Send(new RenameFolderCommand(action.FolderId, action.NewName));
            dispatcher.Dispatch(new RenameFolderSuccessAction(action.FolderId, action.ConnectionId, action.ConnectionName, action.NewName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rename folder");
            dispatcher.Dispatch(new RenameFolderFailureAction(action.ConnectionName, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleDeleteFolder(DeleteFolderAction action, IDispatcher dispatcher)
    {
        try
        {
            await _mediator.Send(new DeleteFolderCommand(action.FolderId));
            dispatcher.Dispatch(new DeleteFolderSuccessAction(action.FolderId, action.ConnectionId, action.ConnectionName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete folder");
            dispatcher.Dispatch(new DeleteFolderFailureAction(action.ConnectionName, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleAddResourceToFolder(AddResourceToFolderAction action, IDispatcher dispatcher)
    {
        try
        {
            await _mediator.Send(new AddResourceToFolderCommand(action.FolderId, action.ResourceUrl));
            dispatcher.Dispatch(new AddResourceToFolderSuccessAction(action.FolderId, action.ConnectionId, action.ConnectionName, action.ResourceUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add resource to folder");
            dispatcher.Dispatch(new AddResourceToFolderFailureAction(action.ConnectionName, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleRemoveResourceFromFolder(RemoveResourceFromFolderAction action, IDispatcher dispatcher)
    {
        try
        {
            await _mediator.Send(new RemoveResourceFromFolderCommand(action.FolderId, action.ResourceUrl));
            dispatcher.Dispatch(new RemoveResourceFromFolderSuccessAction(action.FolderId, action.ConnectionId, action.ConnectionName, action.ResourceUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove resource from folder");
            dispatcher.Dispatch(new RemoveResourceFromFolderFailureAction(action.ConnectionName, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleExportFolders(ExportFoldersAction action, IDispatcher dispatcher)
    {
        try
        {
            await _mediator.Send(new ExportFoldersCommand(action.ConnectionId, action.ConnectionConfig, action.DestinationPath));
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.ConnectionName, "Info", "Folders exported successfully.", DateTime.Now)));
            dispatcher.Dispatch(new ExportFoldersSuccessAction(action.ConnectionName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export folders for connection '{ConnectionName}'", action.ConnectionName);
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.ConnectionName, "Error", $"Exporting folders failed: {ex.Message}", DateTime.Now)));
            dispatcher.Dispatch(new ExportFoldersFailureAction(action.ConnectionName, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleImportFolders(ImportFoldersAction action, IDispatcher dispatcher)
    {
        try
        {
            await _mediator.Send(new ImportFoldersCommand(action.ConnectionId, action.ConnectionConfig, action.JsonContent));
            var folders = await _mediator.Send(new GetFoldersByConnectionIdQuery(action.ConnectionId));
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.ConnectionName, "Info", "Folders imported successfully.", DateTime.Now)));
            dispatcher.Dispatch(new ImportFoldersSuccessAction(
                action.ConnectionId, action.ConnectionName, action.ConnectionConfig, folders.ToList()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import folders for connection '{ConnectionName}'", action.ConnectionName);
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.ConnectionName, "Error", $"Importing folders failed: {ex.Message}", DateTime.Now)));
            dispatcher.Dispatch(new ImportFoldersFailureAction(action.ConnectionName, ex.Message));
        }
    }

    private static string BuildExceptionSummary(Exception ex)
    {
        var parts = new List<string>();
        
        AppendExceptionMessages(ex, parts);

        return string.Join(Environment.NewLine, parts).Trim();
    }

    private static void AppendExceptionMessages(Exception ex, List<string> parts)
    {
        if (ex is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                AppendExceptionMessages(innerException, parts);

            return;
        }

        parts.Add($"{ex.GetType().Name}: {ex.Message}");

        if (ex.InnerException is not null)
            AppendExceptionMessages(ex.InnerException, parts);
    }

}
