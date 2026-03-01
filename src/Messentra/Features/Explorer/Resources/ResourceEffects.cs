using Fluxor;
using Mediator;
using Messentra.Features.Explorer.Resources.Queues.GetAllQueueResources;
using Messentra.Features.Explorer.Resources.Queues.GetQueueResource;
using Messentra.Features.Explorer.Resources.Subscriptions.GetSubscriptionResource;
using Messentra.Features.Explorer.Resources.Topics.GetAllTopicResources;
using Messentra.Features.Explorer.Resources.Topics.GetTopicResource;
using Messentra.Features.Layout.State;
using Messentra.Infrastructure.AzureServiceBus;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources;

public sealed class ResourceEffects
{
    private readonly IMediator _mediator;

    public ResourceEffects(IMediator mediator)
    {
        _mediator = mediator;
    }

    [EffectMethod]
    public async Task HandleFetchResources(FetchResourcesAction action, IDispatcher dispatcher)
    {
        try
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.ConnectionName,
                "Info",
                "Fetching resources...",
                DateTime.Now)));
            var getQueues = _mediator.Send(new GetAllQueueResourcesQuery(action.ConnectionConfig)).AsTask();
            var getTopics = _mediator.Send(new GetAllTopicResourcesQuery(action.ConnectionConfig)).AsTask();

            await Task.WhenAll(getQueues, getTopics);

            var treeItems = MapToTreeItems(action, getQueues.Result, getTopics.Result);
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.ConnectionName,
                "Info",
                "Resources fetched successfully.",
                DateTime.Now)));
            dispatcher.Dispatch(new FetchResourcesSuccessAction(treeItems));
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.ConnectionName,
                "Error",
                "Fetching resources failed: " + ex.Message,
                DateTime.Now)));
            dispatcher.Dispatch(new FetchResourcesFailureAction(ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleRefreshQueue(RefreshQueueAction action, IDispatcher dispatcher)
    {
        try
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName,
                "Info",
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
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName,
                "Error",
                $"Refreshing '{action.Node.Resource.Name}' queue failed: " + ex.Message,
                DateTime.Now)));
            dispatcher.Dispatch(new RefreshQueueFailureAction(action.Node, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleRefreshTopic(RefreshTopicAction action, IDispatcher dispatcher)
    {
        try
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName,
                "Info",
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
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName,
                "Error",
                $"Refreshing '{action.Node.Resource.Name}' topic failed: " + ex.Message,
                DateTime.Now)));
            dispatcher.Dispatch(new RefreshTopicFailureAction(action.Node, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleRefreshSubscription(RefreshSubscriptionAction action, IDispatcher dispatcher)
    {
        try
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName,
                "Info",
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
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName,
                "Error",
                $"Refreshing '{action.Node.Resource.TopicName}/{action.Node.Resource.Name}' subscription failed: " + ex.Message,
                DateTime.Now)));
            dispatcher.Dispatch(new RefreshSubscriptionFailureAction(action.Node, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleRefreshQueues(RefreshQueuesAction action, IDispatcher dispatcher)
    {
        try
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName, "Info", "Refreshing all queues...", DateTime.Now)));

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
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName, "Error", "Refreshing queues failed: " + ex.Message, DateTime.Now)));
            dispatcher.Dispatch(new RefreshQueuesFailureAction(action.Node, ex.Message));
        }
    }

    [EffectMethod]
    public async Task HandleRefreshTopics(RefreshTopicsAction action, IDispatcher dispatcher)
    {
        try
        {
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName, "Info", "Refreshing all topics...", DateTime.Now)));

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
            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.Node.ConnectionName, "Error", "Refreshing topics failed: " + ex.Message, DateTime.Now)));
            dispatcher.Dispatch(new RefreshTopicsFailureAction(action.Node, ex.Message));
        }
    }

    private static ResourceTreeItemData MapToTreeItems(
        FetchResourcesAction action,
        IReadOnlyCollection<Resource.Queue> queues,
        IReadOnlyCollection<Resource.Topic> topics)
    {
        var queueItems = new ResourceTreeItemData
        {
            Text = "Queues",
            IsReadonly = true,
            Value = new QueuesTreeNode(action.ConnectionName, action.ConnectionConfig),
            Icon = Icons.Material.Filled.ViewList,
            IconColor = Color.Secondary,
            Children = queues
                .Select(queue => new ResourceTreeItemData
                {
                    Text = queue.Name,
                    Value = new QueueTreeNode(action.ConnectionName, queue, action.ConnectionConfig),
                    Expandable = false
                })
                .ToList()
        };

        var topicItems = new ResourceTreeItemData
        {
            Text = "Topics",
            IsReadonly = true,
            Value = new TopicsTreeNode(action.ConnectionName, action.ConnectionConfig),
            Icon = Icons.Material.Filled.DynamicFeed,
            IconColor = Color.Secondary,
            Children = topics
                .Select(topic =>
                {
                    var subscriptionItems = topic.Subscriptions
                        .Select(sub => new ResourceTreeItemData
                        {
                            Text = sub.Name,
                            Value = new SubscriptionTreeNode(action.ConnectionName, sub, action.ConnectionConfig),
                            Expandable = false
                        })
                        .ToList();

                    return new ResourceTreeItemData
                    {
                        Text = topic.Name,
                        Value = new TopicTreeNode(action.ConnectionName, topic, action.ConnectionConfig),
                        Icon = Icons.Material.Filled.Topic,
                        IconColor = Color.Secondary,
                        Expandable = subscriptionItems.Count > 0,
                        Children = subscriptionItems.Count > 0
                            ? new HashSet<ResourceTreeItemData>(subscriptionItems)
                            : null
                    };
                })
                .ToList()
        };

        var rootNode = new ResourceTreeItemData
        {
            Text = action.ConnectionName,
            Value = new NamespaceTreeNode(action.ConnectionName, action.ConnectionConfig),
            Icon = Icons.Material.Filled.Cloud,
            IconColor = Color.Primary,
            Expanded = true,
            Expandable = true,
            IsReadonly = true,
            Children = [queueItems, topicItems]
        };

        return rootNode;
    }
}