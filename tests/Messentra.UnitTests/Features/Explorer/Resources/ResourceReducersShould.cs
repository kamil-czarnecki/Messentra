using Messentra.Domain;
using Messentra.Features.Explorer.Resources;
using Messentra.Infrastructure.AzureServiceBus;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Resources;

public sealed class ResourceReducersShould
{
    private const string ConnectionName = "test-connection";

    private static ResourceState BuildStateWithQueue(
        ConnectionConfig config, Resource.Queue queue, bool queueSelected = false)
    {
        var queueNode = new QueueTreeNode(ConnectionName, queue, config);
        var entry = new NamespaceEntry(
            ConnectionName, config, false,
            Queues: new Dictionary<string, QueueEntry> { [queue.Url] = new(queueNode, false) },
            Topics: []);
        return new ResourceState(
            Namespaces: [entry],
            SelectedResource: queueSelected ? queueNode : null,
            ExpandedKeys: []);
    }

    private static ResourceState BuildStateWithTopic(
        ConnectionConfig config,
        Resource.Topic topic,
        bool topicSelected = false,
        bool subscriptionSelected = false)
    {
        var topicNode = new TopicTreeNode(ConnectionName, topic, config);
        var subs = topic.Subscriptions.ToDictionary(
            s => s.Url,
            s => new SubscriptionEntry(new SubscriptionTreeNode(ConnectionName, s, config), false));
        var entry = new NamespaceEntry(
            ConnectionName, config, false,
            Queues: [],
            Topics: new Dictionary<string, TopicEntry> { [topic.Url] = new(topicNode, false, subs) });

        ResourceTreeNode? selected = null;
        if (topicSelected) selected = topicNode;
        else if (subscriptionSelected && topic.Subscriptions.Count > 0)
            selected = new SubscriptionTreeNode(ConnectionName, topic.Subscriptions.First(), config);

        return new ResourceState(Namespaces: [entry], SelectedResource: selected, ExpandedKeys: []);
    }

    [Fact]
    public void FetchResourcesAction_AppendsNamespaceNodeInLoadingState()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var state = new ResourceState(Namespaces: [], SelectedResource: null, ExpandedKeys: []);
        var action = new FetchResourcesAction(ConnectionName, config);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces.Count.ShouldBe(1);
        newState.Namespaces[0].ConnectionName.ShouldBe(ConnectionName);
        newState.Namespaces[0].IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void FetchResourcesSuccessAction_ReplacesLoadingNamespaceWithPopulatedEntry()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var loadingEntry = new NamespaceEntry(ConnectionName, config, IsLoading: true, Queues: [], Topics: []);
        var state = new ResourceState(Namespaces: [loadingEntry], SelectedResource: null, ExpandedKeys: []);
        var queue = ResourceTestData.CreateQueue("queue-1");
        var action = new FetchResourcesSuccessAction(ConnectionName, config, [queue], []);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces.Count.ShouldBe(1);
        newState.Namespaces[0].IsLoading.ShouldBeFalse();
        newState.Namespaces[0].Queues.ShouldContainKey(queue.Url);
    }

    [Fact]
    public void FetchResourcesFailureAction_StateUnchanged()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var entry = new NamespaceEntry(ConnectionName, config, false, [], []);
        var state = new ResourceState(Namespaces: [entry], SelectedResource: null, ExpandedKeys: []);
        var action = new FetchResourcesFailureAction("some error");

        // Act
        var newState = ResourceReducers.OnFetchResourcesFailure(state, action);

        // Assert
        newState.ShouldBe(state);
    }

    [Fact]
    public void SelectResourceAction_SetsSelectedResource()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var state = BuildStateWithQueue(config, queue);
        var queueNode = new QueueTreeNode(ConnectionName, queue, config);
        var action = new SelectResourceAction(queueNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.SelectedResource.ShouldNotBeNull();
        newState.SelectedResource.ShouldBe(queueNode);
    }

    [Fact]
    public void ToggleExpandedAction_AddsKeyWhenExpanded()
    {
        // Arrange
        var state = new ResourceState(Namespaces: [], SelectedResource: null, ExpandedKeys: []);
        var action = new ToggleExpandedAction("queues:test-connection", Expanded: true);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.ExpandedKeys.ShouldContain("queues:test-connection");
    }

    [Fact]
    public void ToggleExpandedAction_RemovesKeyWhenCollapsed()
    {
        // Arrange
        var state = new ResourceState(Namespaces: [], SelectedResource: null,
            ExpandedKeys: ["queues:test-connection", "topics:test-connection"]);
        var action = new ToggleExpandedAction("queues:test-connection", Expanded: false);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.ExpandedKeys.ShouldNotContain("queues:test-connection");
        newState.ExpandedKeys.ShouldContain("topics:test-connection");
    }

    [Fact]
    public void DisconnectResourceAction_RemovesNamespaceAndClearsSelection()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var entry = new NamespaceEntry(ConnectionName, config, false, [], []);
        var state = new ResourceState(
            Namespaces: [entry],
            SelectedResource: new NamespaceTreeNode(ConnectionName, config),
            ExpandedKeys: [$"ns:{ConnectionName}"]);
        var action = new DisconnectResourceAction(ConnectionName);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces.ShouldBeEmpty();
        newState.SelectedResource.ShouldBeNull();
    }

    [Fact]
    public void RefreshQueueAction_SetsQueueNodeLoading()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var state = BuildStateWithQueue(config, queue);
        var queueNode = new QueueTreeNode(ConnectionName, queue, config);
        var action = new RefreshQueueAction(queueNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Queues[queue.Url].IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void RefreshQueueAction_SetsSelectedResourceLoadingWhenQueueSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var state = BuildStateWithQueue(config, queue, queueSelected: true);
        var queueNode = new QueueTreeNode(ConnectionName, queue, config);
        var action = new RefreshQueueAction(queueNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as QueueTreeNode;
        selectedNode!.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void RefreshQueueSuccessAction_ReplacesQueueNode()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var state = BuildStateWithQueue(config, queue);
        var updatedQueue = ResourceTestData.CreateQueue("queue-1");
        var updatedNode = new QueueTreeNode(ConnectionName, updatedQueue, config);
        var action = new RefreshQueueSuccessAction(updatedNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Queues[queue.Url].Node.Resource.Name.ShouldBe("queue-1");
        newState.Namespaces[0].Queues[queue.Url].IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshQueueSuccessAction_ReplacesSelectedResourceWhenQueueSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var state = BuildStateWithQueue(config, queue, queueSelected: true);
        var updatedQueue = ResourceTestData.CreateQueue("queue-1");
        var updatedNode = new QueueTreeNode(ConnectionName, updatedQueue, config);
        var action = new RefreshQueueSuccessAction(updatedNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as QueueTreeNode;
        selectedNode!.Resource.Name.ShouldBe("queue-1");
        selectedNode.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshQueueFailureAction_ClearsQueueNodeLoading()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var loadingNode = new QueueTreeNode(ConnectionName, queue, config, IsLoading: true);
        var entry = new NamespaceEntry(
            ConnectionName, config, false,
            Queues: new Dictionary<string, QueueEntry> { [queue.Url] = new(loadingNode, true) },
            Topics: []);
        var state = new ResourceState(Namespaces: [entry], SelectedResource: null, ExpandedKeys: []);
        var action = new RefreshQueueFailureAction(loadingNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Queues[queue.Url].IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshQueueFailureAction_ClearsSelectedResourceLoadingWhenQueueSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var loadingNode = new QueueTreeNode(ConnectionName, queue, config, IsLoading: true);
        var entry = new NamespaceEntry(
            ConnectionName, config, false,
            Queues: new Dictionary<string, QueueEntry> { [queue.Url] = new(loadingNode, true) },
            Topics: []);
        var state = new ResourceState(Namespaces: [entry], SelectedResource: loadingNode, ExpandedKeys: []);
        var action = new RefreshQueueFailureAction(loadingNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as QueueTreeNode;
        selectedNode!.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshTopicAction_SetsTopicNodeLoading()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var state = BuildStateWithTopic(config, topic);
        var topicNode = new TopicTreeNode(ConnectionName, topic, config);
        var action = new RefreshTopicAction(topicNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Topics[topic.Url].IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void RefreshTopicAction_SetsSelectedResourceLoadingWhenTopicSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var state = BuildStateWithTopic(config, topic, topicSelected: true);
        var topicNode = new TopicTreeNode(ConnectionName, topic, config);
        var action = new RefreshTopicAction(topicNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as TopicTreeNode;
        selectedNode!.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void RefreshTopicSuccessAction_ReplacesTopicNode()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var state = BuildStateWithTopic(config, topic);
        var updatedTopic = ResourceTestData.CreateTopic("topic-1");
        var updatedNode = new TopicTreeNode(ConnectionName, updatedTopic, config);
        var action = new RefreshTopicSuccessAction(updatedNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Topics[topic.Url].Node.Resource.Name.ShouldBe("topic-1");
        newState.Namespaces[0].Topics[topic.Url].IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshTopicSuccessAction_ReplacesSelectedResourceWhenTopicSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var state = BuildStateWithTopic(config, topic, topicSelected: true);
        var updatedTopic = ResourceTestData.CreateTopic("topic-1");
        var updatedNode = new TopicTreeNode(ConnectionName, updatedTopic, config);
        var action = new RefreshTopicSuccessAction(updatedNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as TopicTreeNode;
        selectedNode!.Resource.Name.ShouldBe("topic-1");
        selectedNode.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshTopicFailureAction_ClearsTopicNodeLoading()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var loadingNode = new TopicTreeNode(ConnectionName, topic, config, IsLoading: true);
        var entry = new NamespaceEntry(
            ConnectionName, config, false, Queues: [],
            Topics: new Dictionary<string, TopicEntry> { [topic.Url] = new(loadingNode, true, []) });
        var state = new ResourceState(Namespaces: [entry], SelectedResource: null, ExpandedKeys: []);
        var action = new RefreshTopicFailureAction(loadingNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Topics[topic.Url].IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshTopicFailureAction_ClearsSelectedResourceLoadingWhenTopicSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var loadingNode = new TopicTreeNode(ConnectionName, topic, config, IsLoading: true);
        var entry = new NamespaceEntry(
            ConnectionName, config, false, Queues: [],
            Topics: new Dictionary<string, TopicEntry> { [topic.Url] = new(loadingNode, true, []) });
        var state = new ResourceState(Namespaces: [entry], SelectedResource: loadingNode, ExpandedKeys: []);
        var action = new RefreshTopicFailureAction(loadingNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as TopicTreeNode;
        selectedNode!.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshSubscriptionAction_SetsSubscriptionNodeLoading()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var sub = ResourceTestData.CreateSubscription("sub-1", "topic-1");
        var topic = ResourceTestData.CreateTopic("topic-1", [sub]);
        var state = BuildStateWithTopic(config, topic);
        var subNode = new SubscriptionTreeNode(ConnectionName, sub, config);
        var action = new RefreshSubscriptionAction(subNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Topics[topic.Url].Subscriptions[sub.Url].IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void RefreshSubscriptionAction_SetsSelectedResourceLoadingWhenSubscriptionSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var sub = ResourceTestData.CreateSubscription("sub-1", "topic-1");
        var topic = ResourceTestData.CreateTopic("topic-1", [sub]);
        var state = BuildStateWithTopic(config, topic, subscriptionSelected: true);
        var subNode = new SubscriptionTreeNode(ConnectionName, sub, config);
        var action = new RefreshSubscriptionAction(subNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as SubscriptionTreeNode;
        selectedNode!.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void RefreshSubscriptionSuccessAction_ReplacesSubscriptionNode()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var sub = ResourceTestData.CreateSubscription("sub-1", "topic-1");
        var topic = ResourceTestData.CreateTopic("topic-1", [sub]);
        var state = BuildStateWithTopic(config, topic);
        var updatedSub = ResourceTestData.CreateSubscription("sub-1", "topic-1");
        var updatedNode = new SubscriptionTreeNode(ConnectionName, updatedSub, config);
        var action = new RefreshSubscriptionSuccessAction(updatedNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Topics[topic.Url].Subscriptions[sub.Url].Node.Resource.Name.ShouldBe("sub-1");
        newState.Namespaces[0].Topics[topic.Url].Subscriptions[sub.Url].IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshSubscriptionSuccessAction_ReplacesSelectedResourceWhenSubscriptionSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var sub = ResourceTestData.CreateSubscription("sub-1", "topic-1");
        var topic = ResourceTestData.CreateTopic("topic-1", [sub]);
        var state = BuildStateWithTopic(config, topic, subscriptionSelected: true);
        var updatedSub = ResourceTestData.CreateSubscription("sub-1", "topic-1");
        var updatedNode = new SubscriptionTreeNode(ConnectionName, updatedSub, config);
        var action = new RefreshSubscriptionSuccessAction(updatedNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as SubscriptionTreeNode;
        selectedNode!.Resource.Name.ShouldBe("sub-1");
        selectedNode.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshSubscriptionFailureAction_ClearsSubscriptionNodeLoading()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var sub = ResourceTestData.CreateSubscription("sub-1", "topic-1");
        var topic = ResourceTestData.CreateTopic("topic-1", [sub]);
        var loadingSubNode = new SubscriptionTreeNode(ConnectionName, sub, config, IsLoading: true);
        var topicNode = new TopicTreeNode(ConnectionName, topic, config);
        var entry = new NamespaceEntry(
            ConnectionName, config, false, Queues: [],
            Topics: new Dictionary<string, TopicEntry>
            {
                [topic.Url] = new(topicNode, false,
                    new Dictionary<string, SubscriptionEntry> { [sub.Url] = new(loadingSubNode, true) })
            });
        var state = new ResourceState(Namespaces: [entry], SelectedResource: null, ExpandedKeys: []);
        var action = new RefreshSubscriptionFailureAction(loadingSubNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Topics[topic.Url].Subscriptions[sub.Url].IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshSubscriptionFailureAction_ClearsSelectedResourceLoadingWhenSubscriptionSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var sub = ResourceTestData.CreateSubscription("sub-1", "topic-1");
        var topic = ResourceTestData.CreateTopic("topic-1", [sub]);
        var loadingSubNode = new SubscriptionTreeNode(ConnectionName, sub, config, IsLoading: true);
        var topicNode = new TopicTreeNode(ConnectionName, topic, config);
        var entry = new NamespaceEntry(
            ConnectionName, config, false, Queues: [],
            Topics: new Dictionary<string, TopicEntry>
            {
                [topic.Url] = new(topicNode, false,
                    new Dictionary<string, SubscriptionEntry> { [sub.Url] = new(loadingSubNode, true) })
            });
        var state = new ResourceState(Namespaces: [entry], SelectedResource: loadingSubNode, ExpandedKeys: []);
        var action = new RefreshSubscriptionFailureAction(loadingSubNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as SubscriptionTreeNode;
        selectedNode!.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshQueuesAction_SetsAllQueuesLoading()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var state = BuildStateWithQueue(config, queue);
        var queuesNode = new QueuesTreeNode(ConnectionName, config);
        var action = new RefreshQueuesAction(queuesNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Queues[queue.Url].IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void RefreshQueuesAction_SetsSelectedResourceLoadingWhenQueueSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var state = BuildStateWithQueue(config, queue, queueSelected: true);
        var queuesNode = new QueuesTreeNode(ConnectionName, config);
        var action = new RefreshQueuesAction(queuesNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as QueueTreeNode;
        selectedNode!.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void RefreshQueuesSuccessAction_ReplacesAllQueueNodes()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var state = BuildStateWithQueue(config, queue);
        var updatedQueue = ResourceTestData.CreateQueue("queue-1");
        var updatedQueueNode = new QueueTreeNode(ConnectionName, updatedQueue, config);
        var queuesNode = new QueuesTreeNode(ConnectionName, config);
        var action = new RefreshQueuesSuccessAction(queuesNode, [updatedQueueNode]);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Queues[queue.Url].Node.Resource.Name.ShouldBe("queue-1");
        newState.Namespaces[0].Queues[queue.Url].IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshQueuesSuccessAction_ReplacesSelectedResourceWhenQueueSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var state = BuildStateWithQueue(config, queue, queueSelected: true);
        var updatedQueue = ResourceTestData.CreateQueue("queue-1");
        var updatedQueueNode = new QueueTreeNode(ConnectionName, updatedQueue, config);
        var queuesNode = new QueuesTreeNode(ConnectionName, config);
        var action = new RefreshQueuesSuccessAction(queuesNode, [updatedQueueNode]);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as QueueTreeNode;
        selectedNode!.Resource.Name.ShouldBe("queue-1");
        selectedNode.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshQueuesFailureAction_ClearsAllQueuesLoading()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var loadingQueueNode = new QueueTreeNode(ConnectionName, queue, config, IsLoading: true);
        var entry = new NamespaceEntry(
            ConnectionName, config, false,
            Queues: new Dictionary<string, QueueEntry> { [queue.Url] = new(loadingQueueNode, true) },
            Topics: []);
        var state = new ResourceState(Namespaces: [entry], SelectedResource: null, ExpandedKeys: []);
        var queuesNode = new QueuesTreeNode(ConnectionName, config);
        var action = new RefreshQueuesFailureAction(queuesNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Queues[queue.Url].IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshQueuesFailureAction_ClearsSelectedResourceLoadingWhenQueueSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var loadingQueueNode = new QueueTreeNode(ConnectionName, queue, config, IsLoading: true);
        var entry = new NamespaceEntry(
            ConnectionName, config, false,
            Queues: new Dictionary<string, QueueEntry> { [queue.Url] = new(loadingQueueNode, true) },
            Topics: []);
        var state = new ResourceState(Namespaces: [entry], SelectedResource: loadingQueueNode, ExpandedKeys: []);
        var queuesNode = new QueuesTreeNode(ConnectionName, config);
        var action = new RefreshQueuesFailureAction(queuesNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as QueueTreeNode;
        selectedNode!.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshTopicsAction_SetsAllTopicsLoading()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var state = BuildStateWithTopic(config, topic);
        var topicsNode = new TopicsTreeNode(ConnectionName, config);
        var action = new RefreshTopicsAction(topicsNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Topics[topic.Url].IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void RefreshTopicsAction_SetsSelectedResourceLoadingWhenTopicSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var state = BuildStateWithTopic(config, topic, topicSelected: true);
        var topicsNode = new TopicsTreeNode(ConnectionName, config);
        var action = new RefreshTopicsAction(topicsNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as TopicTreeNode;
        selectedNode!.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void RefreshTopicsSuccessAction_ReplacesAllTopicNodes()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var state = BuildStateWithTopic(config, topic);
        var updatedTopic = ResourceTestData.CreateTopic("topic-1");
        var updatedTopicNode = new TopicTreeNode(ConnectionName, updatedTopic, config);
        var topicsNode = new TopicsTreeNode(ConnectionName, config);
        var action = new RefreshTopicsSuccessAction(topicsNode, [updatedTopicNode]);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Topics[topic.Url].Node.Resource.Name.ShouldBe("topic-1");
        newState.Namespaces[0].Topics[topic.Url].IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshTopicsSuccessAction_ReplacesSelectedResourceWhenTopicSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var state = BuildStateWithTopic(config, topic, topicSelected: true);
        var updatedTopic = ResourceTestData.CreateTopic("topic-1");
        var updatedTopicNode = new TopicTreeNode(ConnectionName, updatedTopic, config);
        var topicsNode = new TopicsTreeNode(ConnectionName, config);
        var action = new RefreshTopicsSuccessAction(topicsNode, [updatedTopicNode]);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as TopicTreeNode;
        selectedNode!.Resource.Name.ShouldBe("topic-1");
        selectedNode.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshTopicsFailureAction_ClearsAllTopicsLoading()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var loadingTopicNode = new TopicTreeNode(ConnectionName, topic, config, IsLoading: true);
        var entry = new NamespaceEntry(
            ConnectionName, config, false, Queues: [],
            Topics: new Dictionary<string, TopicEntry> { [topic.Url] = new(loadingTopicNode, true, []) });
        var state = new ResourceState(Namespaces: [entry], SelectedResource: null, ExpandedKeys: []);
        var topicsNode = new TopicsTreeNode(ConnectionName, config);
        var action = new RefreshTopicsFailureAction(topicsNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces[0].Topics[topic.Url].IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshTopicsFailureAction_ClearsSelectedResourceLoadingWhenTopicSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var loadingTopicNode = new TopicTreeNode(ConnectionName, topic, config, IsLoading: true);
        var entry = new NamespaceEntry(
            ConnectionName, config, false, Queues: [],
            Topics: new Dictionary<string, TopicEntry> { [topic.Url] = new(loadingTopicNode, true, []) });
        var state = new ResourceState(Namespaces: [entry], SelectedResource: loadingTopicNode, ExpandedKeys: []);
        var topicsNode = new TopicsTreeNode(ConnectionName, config);
        var action = new RefreshTopicsFailureAction(topicsNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as TopicTreeNode;
        selectedNode!.IsLoading.ShouldBeFalse();
    }
}

