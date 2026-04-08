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
            ConnectionId: 1L, ConnectionName, config, false,
            Queues: new Dictionary<string, QueueEntry> { [queue.Url] = new(queueNode, false) },
            Topics: [], Folders: []);
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
            ConnectionId: 1L, ConnectionName, config, false,
            Queues: [],
            Topics: new Dictionary<string, TopicEntry> { [topic.Url] = new(topicNode, false, subs) },
            Folders: []);

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
        var action = new FetchResourcesAction(ConnectionId: 1L, ConnectionName, config);

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
        var loadingEntry = new NamespaceEntry(ConnectionId: 1L, ConnectionName, config, IsLoading: true, Queues: [], Topics: [], Folders: []);
        var state = new ResourceState(Namespaces: [loadingEntry], SelectedResource: null, ExpandedKeys: []);
        var queue = ResourceTestData.CreateQueue("queue-1");
        var action = new FetchResourcesSuccessAction(ConnectionId: 1L, ConnectionName, config, [queue], [], []);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces.Count.ShouldBe(1);
        newState.Namespaces[0].IsLoading.ShouldBeFalse();
        newState.Namespaces[0].Queues.ShouldContainKey(queue.Url);
    }

    [Fact]
    public void FetchResourcesFailureAction_RemovesFailedLoadingNamespace()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var loadingEntry = new NamespaceEntry(1L, ConnectionName, config, true, [], [], []);
        var state = new ResourceState(
            Namespaces: [loadingEntry],
            SelectedResource: null,
            ExpandedKeys: [$"ns:{ConnectionName}"]);
        var action = new FetchResourcesFailureAction(ConnectionName, "some error");

        // Act
        var newState = ResourceReducers.OnFetchResourcesFailure(state, action);

        // Assert
        newState.Namespaces.ShouldBeEmpty();
        newState.ExpandedKeys.ShouldNotContain($"ns:{ConnectionName}");
        newState.SelectedResource.ShouldBeNull();
    }

    [Fact]
    public void CancelFetchResourcesAction_RemovesLoadingNamespace()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var loadingEntry = new NamespaceEntry(1L, ConnectionName, config, true, [], [], []);
        var state = new ResourceState(
            Namespaces: [loadingEntry],
            SelectedResource: null,
            ExpandedKeys: [$"ns:{ConnectionName}"]);
        var action = new CancelFetchResourcesAction(ConnectionName);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces.ShouldBeEmpty();
        newState.ExpandedKeys.ShouldNotContain($"ns:{ConnectionName}");
        newState.SelectedResource.ShouldBeNull();
    }

    [Fact]
    public void FetchResourcesCanceledAction_RemovesLoadingNamespace()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var loadingEntry = new NamespaceEntry(1L, ConnectionName, config, true, [], [], []);
        var state = new ResourceState(
            Namespaces: [loadingEntry],
            SelectedResource: null,
            ExpandedKeys: [$"ns:{ConnectionName}"]);
        var action = new FetchResourcesCanceledAction(ConnectionName);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces.ShouldBeEmpty();
        newState.ExpandedKeys.ShouldNotContain($"ns:{ConnectionName}");
        newState.SelectedResource.ShouldBeNull();
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
        var entry = new NamespaceEntry(1L, ConnectionName, config, false, [], [], []);
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
    public void DisconnectResourceAction_ClearsFoldersTreeNodeWhenSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var node = new FoldersTreeNode(1L, ConnectionName, config);
        var state = new ResourceState([], node, []);
        var action = new DisconnectResourceAction(ConnectionName);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.SelectedResource.ShouldBeNull();
    }

    [Fact]
    public void DisconnectResourceAction_ClearsFolderTreeNodeWhenSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var node = new FolderTreeNode(10L, 1L, "My Team", ConnectionName, config);
        var state = new ResourceState([], node, []);
        var action = new DisconnectResourceAction(ConnectionName);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
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
            ConnectionId: 1L, ConnectionName, config, false,
            Queues: new Dictionary<string, QueueEntry> { [queue.Url] = new(loadingNode, true) },
            Topics: [], Folders: []);
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
            ConnectionId: 1L, ConnectionName, config, false,
            Queues: new Dictionary<string, QueueEntry> { [queue.Url] = new(loadingNode, true) },
            Topics: [], Folders: []);
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
            ConnectionId: 1L, ConnectionName, config, false, Queues: [],
            Topics: new Dictionary<string, TopicEntry> { [topic.Url] = new(loadingNode, true, []) }, Folders: []);
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
            ConnectionId: 1L, ConnectionName, config, false, Queues: [],
            Topics: new Dictionary<string, TopicEntry> { [topic.Url] = new(loadingNode, true, []) }, Folders: []);
        var state = new ResourceState(Namespaces: [entry], SelectedResource: loadingNode, ExpandedKeys: []);
        var topicsNode = new TopicsTreeNode(ConnectionName, config);
        var action = new RefreshTopicsFailureAction(topicsNode, "error");

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
            ConnectionId: 1L, ConnectionName, config, false, Queues: [],
            Topics: new Dictionary<string, TopicEntry>
            {
                [topic.Url] = new(topicNode, false,
                    new Dictionary<string, SubscriptionEntry> { [sub.Url] = new(loadingSubNode, true) })
            }, Folders: []);
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
            ConnectionId: 1L, ConnectionName, config, false, Queues: [],
            Topics: new Dictionary<string, TopicEntry>
            {
                [topic.Url] = new(topicNode, false,
                    new Dictionary<string, SubscriptionEntry> { [sub.Url] = new(loadingSubNode, true) })
            }, Folders: []);
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
            ConnectionId: 1L, ConnectionName, config, false,
            Queues: new Dictionary<string, QueueEntry> { [queue.Url] = new(loadingQueueNode, true) },
            Topics: [], Folders: []);
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
            ConnectionId: 1L, ConnectionName, config, false,
            Queues: new Dictionary<string, QueueEntry> { [queue.Url] = new(loadingQueueNode, true) },
            Topics: [], Folders: []);
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
            ConnectionId: 1L, ConnectionName, config, false, Queues: [],
            Topics: new Dictionary<string, TopicEntry> { [topic.Url] = new(loadingTopicNode, true, []) }, Folders: []);
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
            ConnectionId: 1L, ConnectionName, config, false, Queues: [],
            Topics: new Dictionary<string, TopicEntry> { [topic.Url] = new(loadingTopicNode, true, []) }, Folders: []);
        var state = new ResourceState(Namespaces: [entry], SelectedResource: loadingTopicNode, ExpandedKeys: []);
        var topicsNode = new TopicsTreeNode(ConnectionName, config);
        var action = new RefreshTopicsFailureAction(topicsNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource as TopicTreeNode;
        selectedNode!.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void SetSearchPhraseAction_SetsSearchPhrase()
    {
        // Arrange
        var state = new ResourceState(Namespaces: [], SelectedResource: null, ExpandedKeys: []);
        var action = new SetSearchPhraseAction("queue2");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.SearchPhrase.ShouldBe("queue2");
    }

    [Fact]
    public void SetSearchPhraseAction_NormalizesEmptyStringToNull()
    {
        // Arrange
        var state = new ResourceState(Namespaces: [], SelectedResource: null, ExpandedKeys: [], SearchPhrase: "queue2");
        var action = new SetSearchPhraseAction("");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.SearchPhrase.ShouldBeNull();
    }

    [Fact]
    public void SetSearchPhraseAction_NullPhraseResetsSearchPhrase()
    {
        // Arrange
        var state = new ResourceState(Namespaces: [], SelectedResource: null, ExpandedKeys: [], SearchPhrase: "queue2");
        var action = new SetSearchPhraseAction(null);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.SearchPhrase.ShouldBeNull();
    }

    [Fact]
    public void DisconnectResourceAction_PreservesSearchPhrase()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var entry = new NamespaceEntry(1L, ConnectionName, config, false, Queues: [], Topics: [], Folders: []);
        var state = new ResourceState(Namespaces: [entry], SelectedResource: null, ExpandedKeys: [], SearchPhrase: "queue2");
        var action = new DisconnectResourceAction(ConnectionName);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.SearchPhrase.ShouldBe("queue2");
    }

    [Fact]
    public void CreateFolderSuccessAction_AddsFolderEntryToNamespace()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var entry = new NamespaceEntry(1L, ConnectionName, config, false, [], [], []);
        var state = new ResourceState([entry], null, []);
        var folderNode = new FolderTreeNode(10L, 1L, "My Team", ConnectionName, config);
        var folderEntry = new FolderEntry(folderNode, new HashSet<string>());
        var action = new CreateFolderSuccessAction(1L, ConnectionName, folderEntry);

        // Act
        var newState = ResourceReducers.ReduceCreateFolderSuccess(state, action);

        // Assert
        newState.Namespaces[0].Folders.ShouldContainKey(10L);
        newState.Namespaces[0].Folders[10L].Node.Name.ShouldBe("My Team");
    }

    [Fact]
    public void RenameFolderSuccessAction_UpdatesFolderName()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var folderNode = new FolderTreeNode(10L, 1L, "Old Name", ConnectionName, config);
        var folderEntry = new FolderEntry(folderNode, new HashSet<string>());
        var entry = new NamespaceEntry(1L, ConnectionName, config, false, [], [], new Dictionary<long, FolderEntry> { [10L] = folderEntry });
        var state = new ResourceState([entry], null, []);
        var action = new RenameFolderSuccessAction(10L, 1L, ConnectionName, "New Name");

        // Act
        var newState = ResourceReducers.ReduceRenameFolderSuccess(state, action);

        // Assert
        newState.Namespaces[0].Folders[10L].Node.Name.ShouldBe("New Name");
    }

    [Fact]
    public void DeleteFolderSuccessAction_RemovesFolderFromNamespace()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var folderNode = new FolderTreeNode(10L, 1L, "My Team", ConnectionName, config);
        var folderEntry = new FolderEntry(folderNode, new HashSet<string>());
        var entry = new NamespaceEntry(1L, ConnectionName, config, false, [], [], new Dictionary<long, FolderEntry> { [10L] = folderEntry });
        var state = new ResourceState([entry], null, []);
        var action = new DeleteFolderSuccessAction(10L, 1L, ConnectionName);

        // Act
        var newState = ResourceReducers.ReduceDeleteFolderSuccess(state, action);

        // Assert
        newState.Namespaces[0].Folders.ShouldNotContainKey(10L);
    }

    [Fact]
    public void AddResourceToFolderSuccessAction_AddsUrlToFolderEntry()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var folderNode = new FolderTreeNode(10L, 1L, "My Team", ConnectionName, config);
        var folderEntry = new FolderEntry(folderNode, new HashSet<string>());
        var entry = new NamespaceEntry(1L, ConnectionName, config, false, [], [], new Dictionary<long, FolderEntry> { [10L] = folderEntry });
        var state = new ResourceState([entry], null, []);
        var action = new AddResourceToFolderSuccessAction(10L, 1L, ConnectionName, "queue:orders");

        // Act
        var newState = ResourceReducers.ReduceAddResourceToFolderSuccess(state, action);

        // Assert
        newState.Namespaces[0].Folders[10L].ResourceUrls.ShouldContain("queue:orders");
    }

    [Fact]
    public void RemoveResourceFromFolderSuccessAction_RemovesUrlFromFolderEntry()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var folderNode = new FolderTreeNode(10L, 1L, "My Team", ConnectionName, config);
        var folderEntry = new FolderEntry(folderNode, new HashSet<string>(["queue:orders"]));
        var entry = new NamespaceEntry(1L, ConnectionName, config, false, [], [], new Dictionary<long, FolderEntry> { [10L] = folderEntry });
        var state = new ResourceState([entry], null, []);
        var action = new RemoveResourceFromFolderSuccessAction(10L, 1L, ConnectionName, "queue:orders");

        // Act
        var newState = ResourceReducers.ReduceRemoveResourceFromFolderSuccess(state, action);

        // Assert
        newState.Namespaces[0].Folders[10L].ResourceUrls.ShouldNotContain("queue:orders");
    }

    [Fact]
    public void DisconnectResourceAction_RemovesOnlyExpandedKeysForDisconnectedNamespace()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();

        const string sharedTopicUrl = "https://shared/topics/orders";
        const string sharedSubscriptionUrl = "https://shared/topics/orders/subscriptions/processor";

        var queueA = ResourceTestData.CreateQueue("queue-a");
        var subscriptionA = ResourceTestData.CreateSubscription("sub-a", "topic-a") with { Url = sharedSubscriptionUrl };
        var topicA = ResourceTestData.CreateTopic("topic-a", [subscriptionA]) with { Url = sharedTopicUrl };
        var namespaceA = new NamespaceEntry(
            1L,
            "connection-a",
            config,
            false,
            new Dictionary<string, QueueEntry>
            {
                [queueA.Url] = new(new QueueTreeNode("connection-a", queueA, config), false)
            },
            new Dictionary<string, TopicEntry>
            {
                [topicA.Url] = new(
                    new TopicTreeNode("connection-a", topicA, config),
                    false,
                    new Dictionary<string, SubscriptionEntry>
                    {
                        [subscriptionA.Url] = new(new SubscriptionTreeNode("connection-a", subscriptionA, config), false)
                    })
            },
            new Dictionary<long, FolderEntry>
            {
                [10L] = new(new FolderTreeNode(10L, 1L, "folder-a", "connection-a", config), new HashSet<string>())
            });

        var queueB = ResourceTestData.CreateQueue("queue-b");
        var subscriptionB = ResourceTestData.CreateSubscription("sub-b", "topic-b") with { Url = sharedSubscriptionUrl };
        var topicB = ResourceTestData.CreateTopic("topic-b", [subscriptionB]) with { Url = sharedTopicUrl };
        var namespaceB = new NamespaceEntry(
            2L,
            "connection-b",
            config,
            false,
            new Dictionary<string, QueueEntry>
            {
                [queueB.Url] = new(new QueueTreeNode("connection-b", queueB, config), false)
            },
            new Dictionary<string, TopicEntry>
            {
                [topicB.Url] = new(
                    new TopicTreeNode("connection-b", topicB, config),
                    false,
                    new Dictionary<string, SubscriptionEntry>
                    {
                        [subscriptionB.Url] = new(new SubscriptionTreeNode("connection-b", subscriptionB, config), false)
                    })
            },
            new Dictionary<long, FolderEntry>
            {
                [20L] = new(new FolderTreeNode(20L, 2L, "folder-b", "connection-b", config), new HashSet<string>())
            });

        var state = new ResourceState(
            Namespaces: [namespaceA, namespaceB],
            SelectedResource: null,
            ExpandedKeys: new HashSet<string>
            {
                "ns:connection-a", "queues:connection-a", "topics:connection-a", "folders:connection-a",
                $"queue:connection-a:{queueA.Url}", $"topic:connection-a:{topicA.Url}", $"sub:connection-a:{subscriptionA.Url}", "folder:connection-a:10",
                $"topic:connection-a:{topicA.Url}|folder:connection-a:10",
                $"sub:connection-a:{subscriptionA.Url}|folder:connection-a:10",
                "ns:connection-b", "queues:connection-b", "topics:connection-b", "folders:connection-b",
                $"queue:connection-b:{queueB.Url}", $"topic:connection-b:{topicB.Url}", $"sub:connection-b:{subscriptionB.Url}", "folder:connection-b:20",
                $"topic:connection-b:{topicB.Url}|folder:connection-b:20",
                $"sub:connection-b:{subscriptionB.Url}|folder:connection-b:20"
            });

        var action = new DisconnectResourceAction("connection-a");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Namespaces.Select(n => n.ConnectionName).ShouldBe(new[] { "connection-b" });

        newState.ExpandedKeys.ShouldNotContain("ns:connection-a");
        newState.ExpandedKeys.ShouldNotContain("queues:connection-a");
        newState.ExpandedKeys.ShouldNotContain("topics:connection-a");
        newState.ExpandedKeys.ShouldNotContain("folders:connection-a");
        newState.ExpandedKeys.ShouldNotContain($"queue:connection-a:{queueA.Url}");
        newState.ExpandedKeys.ShouldNotContain($"topic:connection-a:{topicA.Url}");
        newState.ExpandedKeys.ShouldNotContain($"sub:connection-a:{subscriptionA.Url}");
        newState.ExpandedKeys.ShouldNotContain("folder:connection-a:10");
        newState.ExpandedKeys.ShouldNotContain($"topic:connection-a:{topicA.Url}|folder:connection-a:10");
        newState.ExpandedKeys.ShouldNotContain($"sub:connection-a:{subscriptionA.Url}|folder:connection-a:10");

        newState.ExpandedKeys.ShouldContain("ns:connection-b");
        newState.ExpandedKeys.ShouldContain("queues:connection-b");
        newState.ExpandedKeys.ShouldContain("topics:connection-b");
        newState.ExpandedKeys.ShouldContain("folders:connection-b");
        newState.ExpandedKeys.ShouldContain($"queue:connection-b:{queueB.Url}");
        newState.ExpandedKeys.ShouldContain($"topic:connection-b:{topicB.Url}");
        newState.ExpandedKeys.ShouldContain($"sub:connection-b:{subscriptionB.Url}");
        newState.ExpandedKeys.ShouldContain("folder:connection-b:20");
        newState.ExpandedKeys.ShouldContain($"topic:connection-b:{topicB.Url}|folder:connection-b:20");
        newState.ExpandedKeys.ShouldContain($"sub:connection-b:{subscriptionB.Url}|folder:connection-b:20");
    }
}

