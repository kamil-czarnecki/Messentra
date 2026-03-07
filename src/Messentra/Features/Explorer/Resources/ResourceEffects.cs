using Fluxor;
using Mediator;
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

            dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                action.ConnectionName,
                "Info",
                "Resources fetched successfully.",
                DateTime.Now)));
            dispatcher.Dispatch(new FetchResourcesSuccessAction(action.ConnectionName, action.ConnectionConfig, getQueues.Result, getTopics.Result));
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

}

