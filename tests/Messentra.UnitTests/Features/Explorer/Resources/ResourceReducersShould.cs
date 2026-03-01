using Messentra.Domain;
using Messentra.Features.Explorer.Resources;
using Messentra.Infrastructure.AzureServiceBus;
using MudBlazor;
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
        var queueItem = new ResourceTreeItemData { Text = queue.Name, Value = queueNode };
        var queuesNode = new QueuesTreeNode(ConnectionName, config);
        var queuesItem = new ResourceTreeItemData
        {
            Text = "Queues",
            Value = queuesNode,
            Children = [queueItem]
        };
        var namespaceItem = new ResourceTreeItemData
        {
            Text = ConnectionName,
            Value = new NamespaceTreeNode(ConnectionName, config),
            Children = [queuesItem]
        };
        return new ResourceState(
            Resources: [namespaceItem],
            SelectedResource: queueSelected ? new ResourceTreeItemData { Text = queue.Name, Value = queueNode } : null);
    }

    private static ResourceState BuildStateWithTopic(
        ConnectionConfig config,
        Resource.Topic topic,
        bool topicSelected = false,
        bool subscriptionSelected = false)
    {
        var topicNode = new TopicTreeNode(ConnectionName, topic, config);
        var subscriptionItems = topic.Subscriptions
            .Select(s => new ResourceTreeItemData
            {
                Text = s.Name,
                Value = new SubscriptionTreeNode(ConnectionName, s, config)
            })
            .ToList();

        var topicItem = new ResourceTreeItemData
        {
            Text = topic.Name,
            Value = topicNode,
            Children = subscriptionItems.Cast<TreeItemData<ResourceTreeNode>>().ToList()
        };
        var topicsItem = new ResourceTreeItemData
        {
            Text = "Topics",
            Value = new TopicsTreeNode(ConnectionName, config),
            Children = [topicItem]
        };
        var namespaceItem = new ResourceTreeItemData
        {
            Text = ConnectionName,
            Value = new NamespaceTreeNode(ConnectionName, config),
            Children = [topicsItem]
        };

        ResourceTreeItemData? selected = null;
        if (topicSelected)
            selected = new ResourceTreeItemData { Text = topic.Name, Value = topicNode };
        else if (subscriptionSelected && subscriptionItems.Count > 0)
            selected = subscriptionItems[0];

        return new ResourceState(Resources: [namespaceItem], SelectedResource: selected);
    }

    private static ResourceTreeItemData GetQueueItem(ResourceState state) =>
        state.Resources[0]
            .Children!.OfType<ResourceTreeItemData>().First()
            .Children!.OfType<ResourceTreeItemData>().First();

    private static ResourceTreeItemData GetTopicItem(ResourceState state) =>
        state.Resources[0]
            .Children!.OfType<ResourceTreeItemData>().First()
            .Children!.OfType<ResourceTreeItemData>().First();

    private static ResourceTreeItemData GetSubscriptionItem(ResourceState state) =>
        GetTopicItem(state)
            .Children!.OfType<ResourceTreeItemData>().First();

    [Fact]
    public void FetchResourcesAction_AppendsNamespaceNodeInLoadingState()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var state = new ResourceState(Resources: [], SelectedResource: null);
        var action = new FetchResourcesAction(ConnectionName, config);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Resources.Count.ShouldBe(1);
        var node = newState.Resources[0].Value as NamespaceTreeNode;
        node.ShouldNotBeNull();
        node.ConnectionName.ShouldBe(ConnectionName);
        node.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void FetchResourcesSuccessAction_ReplacesLoadingNodeWithRealNode()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var loadingItem = new ResourceTreeItemData
        {
            Text = ConnectionName,
            Value = new NamespaceTreeNode(ConnectionName, config, IsLoading: true)
        };
        var state = new ResourceState(Resources: [loadingItem], SelectedResource: null);

        var realItem = new ResourceTreeItemData
        {
            Text = ConnectionName,
            Value = new NamespaceTreeNode(ConnectionName, config)
        };
        var action = new FetchResourcesSuccessAction(realItem);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Resources.Count.ShouldBe(1);
        newState.Resources[0].ShouldBe(realItem);
    }
    
    [Fact]
    public void FetchResourcesFailureAction_StateUnchanged()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var item = new ResourceTreeItemData
        {
            Text = ConnectionName,
            Value = new NamespaceTreeNode(ConnectionName, config)
        };
        var state = new ResourceState(Resources: [item], SelectedResource: null);
        var action = new FetchResourcesFailureAction("some error");

        // Act
        var newState = ResourceReducers.OnFetchResourcesFailure(state, action);

        // Assert
        newState.Resources.ShouldBe(state.Resources);
    }
    
    [Fact]
    public void SelectResourceAction_SetsSelectedResource()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var state = BuildStateWithQueue(config, queue);
        var queueItem = GetQueueItem(state);
        var action = new SelectResourceAction(queueItem);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.SelectedResource.ShouldNotBeNull();
        newState.SelectedResource.Value.ShouldBe(queueItem.Value);
    }
    
    [Fact]
    public void DisconnectResourceAction_RemovesNamespaceNodeAndClearsSelection()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var namespaceItem = new ResourceTreeItemData
        {
            Text = ConnectionName,
            Value = new NamespaceTreeNode(ConnectionName, config)
        };
        var state = new ResourceState(Resources: [namespaceItem], SelectedResource: namespaceItem);
        var action = new DisconnectResourceAction(namespaceItem);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        newState.Resources.ShouldBeEmpty();
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
        var updatedNode = GetQueueItem(newState).Value as QueueTreeNode;
        updatedNode!.IsLoading.ShouldBeTrue();
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
        var selectedNode = newState.SelectedResource!.Value as QueueTreeNode;
        selectedNode!.IsLoading.ShouldBeTrue();
    }
    
    [Fact]
    public void RefreshQueueSuccessAction_ReplacesQueueNode()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var state = BuildStateWithQueue(config, queue);
        var updatedQueue = ResourceTestData.CreateQueue("queue-1"); // same URL
        var updatedNode = new QueueTreeNode(ConnectionName, updatedQueue, config);
        var action = new RefreshQueueSuccessAction(updatedNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var resultNode = GetQueueItem(newState).Value as QueueTreeNode;
        resultNode!.Resource.Name.ShouldBe("queue-1");
        resultNode.IsLoading.ShouldBeFalse();
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
        var selectedNode = newState.SelectedResource!.Value as QueueTreeNode;
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
        var queueItem = new ResourceTreeItemData { Text = queue.Name, Value = loadingNode };
        var queuesItem = new ResourceTreeItemData
        {
            Text = "Queues",
            Value = new QueuesTreeNode(ConnectionName, config),
            Children = [queueItem]
        };
        var namespaceItem = new ResourceTreeItemData
        {
            Text = ConnectionName,
            Value = new NamespaceTreeNode(ConnectionName, config),
            Children = [queuesItem]
        };
        var state = new ResourceState(Resources: [namespaceItem], SelectedResource: null);
        var action = new RefreshQueueFailureAction(loadingNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var resultNode = GetQueueItem(newState).Value as QueueTreeNode;
        resultNode!.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshQueueFailureAction_ClearsSelectedResourceLoadingWhenQueueSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var loadingNode = new QueueTreeNode(ConnectionName, queue, config, IsLoading: true);
        var queueItem = new ResourceTreeItemData { Text = queue.Name, Value = loadingNode };
        var queuesItem = new ResourceTreeItemData
        {
            Text = "Queues", Value = new QueuesTreeNode(ConnectionName, config), Children = [queueItem]
        };
        var namespaceItem = new ResourceTreeItemData
        {
            Text = ConnectionName, Value = new NamespaceTreeNode(ConnectionName, config), Children = [queuesItem]
        };
        var selectedItem = new ResourceTreeItemData { Text = queue.Name, Value = loadingNode };
        var state = new ResourceState(Resources: [namespaceItem], SelectedResource: selectedItem);
        var action = new RefreshQueueFailureAction(loadingNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource!.Value as QueueTreeNode;
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
        var updatedNode = GetTopicItem(newState).Value as TopicTreeNode;
        updatedNode!.IsLoading.ShouldBeTrue();
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
        var selectedNode = newState.SelectedResource!.Value as TopicTreeNode;
        selectedNode!.IsLoading.ShouldBeTrue();
    }
    
    [Fact]
    public void RefreshTopicSuccessAction_ReplacesTopicNode()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var state = BuildStateWithTopic(config, topic);
        var updatedTopic = ResourceTestData.CreateTopic("topic-1"); // same URL
        var updatedNode = new TopicTreeNode(ConnectionName, updatedTopic, config);
        var action = new RefreshTopicSuccessAction(updatedNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var resultNode = GetTopicItem(newState).Value as TopicTreeNode;
        resultNode!.Resource.Name.ShouldBe("topic-1");
        resultNode.IsLoading.ShouldBeFalse();
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
        var selectedNode = newState.SelectedResource!.Value as TopicTreeNode;
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
        var topicItem = new ResourceTreeItemData { Text = topic.Name, Value = loadingNode };
        var topicsItem = new ResourceTreeItemData
        {
            Text = "Topics", Value = new TopicsTreeNode(ConnectionName, config), Children = [topicItem]
        };
        var namespaceItem = new ResourceTreeItemData
        {
            Text = ConnectionName, Value = new NamespaceTreeNode(ConnectionName, config), Children = [topicsItem]
        };
        var state = new ResourceState(Resources: [namespaceItem], SelectedResource: null);
        var action = new RefreshTopicFailureAction(loadingNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var resultNode = GetTopicItem(newState).Value as TopicTreeNode;
        resultNode!.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshTopicFailureAction_ClearsSelectedResourceLoadingWhenTopicSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var loadingNode = new TopicTreeNode(ConnectionName, topic, config, IsLoading: true);
        var topicItem = new ResourceTreeItemData { Text = topic.Name, Value = loadingNode };
        var topicsItem = new ResourceTreeItemData
        {
            Text = "Topics", Value = new TopicsTreeNode(ConnectionName, config), Children = [topicItem]
        };
        var namespaceItem = new ResourceTreeItemData
        {
            Text = ConnectionName, Value = new NamespaceTreeNode(ConnectionName, config), Children = [topicsItem]
        };
        var selectedItem = new ResourceTreeItemData { Text = topic.Name, Value = loadingNode };
        var state = new ResourceState(Resources: [namespaceItem], SelectedResource: selectedItem);
        var action = new RefreshTopicFailureAction(loadingNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource!.Value as TopicTreeNode;
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
        var updatedNode = GetSubscriptionItem(newState).Value as SubscriptionTreeNode;
        updatedNode!.IsLoading.ShouldBeTrue();
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
        var selectedNode = newState.SelectedResource!.Value as SubscriptionTreeNode;
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
        var updatedSub = ResourceTestData.CreateSubscription("sub-1", "topic-1"); // same URL
        var updatedNode = new SubscriptionTreeNode(ConnectionName, updatedSub, config);
        var action = new RefreshSubscriptionSuccessAction(updatedNode);

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var resultNode = GetSubscriptionItem(newState).Value as SubscriptionTreeNode;
        resultNode!.Resource.Name.ShouldBe("sub-1");
        resultNode.IsLoading.ShouldBeFalse();
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
        var selectedNode = newState.SelectedResource!.Value as SubscriptionTreeNode;
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
        var subItem = new ResourceTreeItemData { Text = sub.Name, Value = loadingSubNode };
        var topicNode = new TopicTreeNode(ConnectionName, topic, config);
        var topicItem = new ResourceTreeItemData { Text = topic.Name, Value = topicNode, Children = [subItem] };
        var topicsItem = new ResourceTreeItemData
        {
            Text = "Topics", Value = new TopicsTreeNode(ConnectionName, config), Children = [topicItem]
        };
        var namespaceItem = new ResourceTreeItemData
        {
            Text = ConnectionName, Value = new NamespaceTreeNode(ConnectionName, config), Children = [topicsItem]
        };
        var state = new ResourceState(Resources: [namespaceItem], SelectedResource: null);
        var action = new RefreshSubscriptionFailureAction(loadingSubNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var resultNode = GetSubscriptionItem(newState).Value as SubscriptionTreeNode;
        resultNode!.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshSubscriptionFailureAction_ClearsSelectedResourceLoadingWhenSubscriptionSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var sub = ResourceTestData.CreateSubscription("sub-1", "topic-1");
        var topic = ResourceTestData.CreateTopic("topic-1", [sub]);
        var loadingSubNode = new SubscriptionTreeNode(ConnectionName, sub, config, IsLoading: true);
        var subItem = new ResourceTreeItemData { Text = sub.Name, Value = loadingSubNode };
        var topicNode = new TopicTreeNode(ConnectionName, topic, config);
        var topicItem = new ResourceTreeItemData { Text = topic.Name, Value = topicNode, Children = [subItem] };
        var topicsItem = new ResourceTreeItemData
        {
            Text = "Topics", Value = new TopicsTreeNode(ConnectionName, config), Children = [topicItem]
        };
        var namespaceItem = new ResourceTreeItemData
        {
            Text = ConnectionName, Value = new NamespaceTreeNode(ConnectionName, config), Children = [topicsItem]
        };
        var selectedItem = new ResourceTreeItemData { Text = sub.Name, Value = loadingSubNode };
        var state = new ResourceState(Resources: [namespaceItem], SelectedResource: selectedItem);
        var action = new RefreshSubscriptionFailureAction(loadingSubNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource!.Value as SubscriptionTreeNode;
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
        var queueNode = GetQueueItem(newState).Value as QueueTreeNode;
        queueNode!.IsLoading.ShouldBeTrue();
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
        var selectedNode = newState.SelectedResource!.Value as QueueTreeNode;
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
        var resultNode = GetQueueItem(newState).Value as QueueTreeNode;
        resultNode!.Resource.Name.ShouldBe("queue-1");
        resultNode.IsLoading.ShouldBeFalse();
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
        var selectedNode = newState.SelectedResource!.Value as QueueTreeNode;
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
        var queueItem = new ResourceTreeItemData { Text = queue.Name, Value = loadingQueueNode };
        var queuesNode = new QueuesTreeNode(ConnectionName, config);
        var queuesItem = new ResourceTreeItemData
        {
            Text = "Queues", Value = queuesNode, Children = [queueItem]
        };
        var namespaceItem = new ResourceTreeItemData
        {
            Text = ConnectionName, Value = new NamespaceTreeNode(ConnectionName, config), Children = [queuesItem]
        };
        var state = new ResourceState(Resources: [namespaceItem], SelectedResource: null);
        var action = new RefreshQueuesFailureAction(queuesNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var resultNode = GetQueueItem(newState).Value as QueueTreeNode;
        resultNode!.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshQueuesFailureAction_ClearsSelectedResourceLoadingWhenQueueSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var loadingQueueNode = new QueueTreeNode(ConnectionName, queue, config, IsLoading: true);
        var queueItem = new ResourceTreeItemData { Text = queue.Name, Value = loadingQueueNode };
        var queuesNode = new QueuesTreeNode(ConnectionName, config);
        var queuesItem = new ResourceTreeItemData
        {
            Text = "Queues", Value = queuesNode, Children = [queueItem]
        };
        var namespaceItem = new ResourceTreeItemData
        {
            Text = ConnectionName, Value = new NamespaceTreeNode(ConnectionName, config), Children = [queuesItem]
        };
        var selectedItem = new ResourceTreeItemData { Text = queue.Name, Value = loadingQueueNode };
        var state = new ResourceState(Resources: [namespaceItem], SelectedResource: selectedItem);
        var action = new RefreshQueuesFailureAction(queuesNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource!.Value as QueueTreeNode;
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
        var topicNode = GetTopicItem(newState).Value as TopicTreeNode;
        topicNode!.IsLoading.ShouldBeTrue();
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
        var selectedNode = newState.SelectedResource!.Value as TopicTreeNode;
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
        var resultNode = GetTopicItem(newState).Value as TopicTreeNode;
        resultNode!.Resource.Name.ShouldBe("topic-1");
        resultNode.IsLoading.ShouldBeFalse();
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
        var selectedNode = newState.SelectedResource!.Value as TopicTreeNode;
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
        var topicItem = new ResourceTreeItemData { Text = topic.Name, Value = loadingTopicNode };
        var topicsNode = new TopicsTreeNode(ConnectionName, config);
        var topicsItem = new ResourceTreeItemData
        {
            Text = "Topics", Value = topicsNode, Children = [topicItem]
        };
        var namespaceItem = new ResourceTreeItemData
        {
            Text = ConnectionName, Value = new NamespaceTreeNode(ConnectionName, config), Children = [topicsItem]
        };
        var state = new ResourceState(Resources: [namespaceItem], SelectedResource: null);
        var action = new RefreshTopicsFailureAction(topicsNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var resultNode = GetTopicItem(newState).Value as TopicTreeNode;
        resultNode!.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void RefreshTopicsFailureAction_ClearsSelectedResourceLoadingWhenTopicSelected()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var loadingTopicNode = new TopicTreeNode(ConnectionName, topic, config, IsLoading: true);
        var topicItem = new ResourceTreeItemData { Text = topic.Name, Value = loadingTopicNode };
        var topicsNode = new TopicsTreeNode(ConnectionName, config);
        var topicsItem = new ResourceTreeItemData
        {
            Text = "Topics", Value = topicsNode, Children = [topicItem]
        };
        var namespaceItem = new ResourceTreeItemData
        {
            Text = ConnectionName, Value = new NamespaceTreeNode(ConnectionName, config), Children = [topicsItem]
        };
        var selectedItem = new ResourceTreeItemData { Text = topic.Name, Value = loadingTopicNode };
        var state = new ResourceState(Resources: [namespaceItem], SelectedResource: selectedItem);
        var action = new RefreshTopicsFailureAction(topicsNode, "error");

        // Act
        var newState = ResourceReducers.Reduce(state, action);

        // Assert
        var selectedNode = newState.SelectedResource!.Value as TopicTreeNode;
        selectedNode!.IsLoading.ShouldBeFalse();
    }
}

