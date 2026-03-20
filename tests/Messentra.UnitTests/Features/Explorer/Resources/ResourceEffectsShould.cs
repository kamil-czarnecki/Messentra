using Fluxor;
using Mediator;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Queues.GetAllQueueResources;
using Messentra.Features.Explorer.Resources.Queues.GetQueueResource;
using Messentra.Features.Explorer.Resources.Subscriptions.GetSubscriptionResource;
using Messentra.Features.Explorer.Resources.Topics.GetAllTopicResources;
using Messentra.Features.Explorer.Resources.Topics.GetTopicResource;
using Messentra.Infrastructure.AzureServiceBus;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Resources;

public sealed class ResourceEffectsShould
{
    private const string ConnectionName = "test-connection";

    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IDispatcher> _dispatcher = new();
    private readonly Mock<ILogger<ResourceEffects>> _logger = new();
    private readonly ResourceEffects _sut;

    public ResourceEffectsShould()
    {
        _sut = new ResourceEffects(_mediator.Object, _logger.Object);
    }
    
    [Fact]
    public async Task HandleFetchResources_WhenSuccess_DispatchesFetchResourcesSuccessAction()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var action = new FetchResourcesAction(ConnectionName, config);

        var queues = new[] { ResourceTestData.CreateQueue("queue-1") };
        var topics = new[] { ResourceTestData.CreateTopic("topic-1") };

        _mediator
            .Setup(m => m.Send(It.IsAny<GetAllQueueResourcesQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IReadOnlyCollection<Resource.Queue>>(queues));
        _mediator
            .Setup(m => m.Send(It.IsAny<GetAllTopicResourcesQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IReadOnlyCollection<Resource.Topic>>(topics));

        // Act
        await _sut.HandleFetchResources(action, _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<FetchResourcesSuccessAction>()), Times.Once);
    }

    [Fact]
    public async Task HandleFetchResources_WhenException_DispatchesFetchResourcesFailureAction()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var action = new FetchResourcesAction(ConnectionName, config);

        _mediator
            .Setup(m => m.Send(It.IsAny<GetAllQueueResourcesQuery>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("connection failed"));

        // Act
        await _sut.HandleFetchResources(action, _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<FetchResourcesFailureAction>()), Times.Once);
    }
    
    [Fact]
    public async Task HandleRefreshQueue_WhenSuccess_DispatchesRefreshQueueSuccessAction()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var queueNode = new QueueTreeNode(ConnectionName, queue, config);
        var action = new RefreshQueueAction(queueNode);

        _mediator
            .Setup(m => m.Send(It.IsAny<GetQueueResourceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<GetQueueResult>(queue));

        // Act
        await _sut.HandleRefreshQueue(action, _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshQueueSuccessAction>()), Times.Once);
    }

    [Fact]
    public async Task HandleRefreshQueue_WhenException_DispatchesRefreshQueueFailureAction()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("queue-1");
        var queueNode = new QueueTreeNode(ConnectionName, queue, config);
        var action = new RefreshQueueAction(queueNode);

        _mediator
            .Setup(m => m.Send(It.IsAny<GetQueueResourceQuery>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("refresh failed"));

        // Act
        await _sut.HandleRefreshQueue(action, _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshQueueFailureAction>()), Times.Once);
    }
    
    [Fact]
    public async Task HandleRefreshTopic_WhenSuccess_DispatchesRefreshTopicSuccessAction()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var topicNode = new TopicTreeNode(ConnectionName, topic, config);
        var action = new RefreshTopicAction(topicNode);

        _mediator
            .Setup(m => m.Send(It.IsAny<GetTopicResourceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<GetTopicResult>(topic));

        // Act
        await _sut.HandleRefreshTopic(action, _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshTopicSuccessAction>()), Times.Once);
    }

    [Fact]
    public async Task HandleRefreshTopic_WhenException_DispatchesRefreshTopicFailureAction()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topic = ResourceTestData.CreateTopic("topic-1");
        var topicNode = new TopicTreeNode(ConnectionName, topic, config);
        var action = new RefreshTopicAction(topicNode);

        _mediator
            .Setup(m => m.Send(It.IsAny<GetTopicResourceQuery>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("refresh failed"));

        // Act
        await _sut.HandleRefreshTopic(action, _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshTopicFailureAction>()), Times.Once);
    }
    
    [Fact]
    public async Task HandleRefreshSubscription_WhenSuccess_DispatchesRefreshSubscriptionSuccessAction()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var sub = ResourceTestData.CreateSubscription("sub-1", "topic-1");
        var subNode = new SubscriptionTreeNode(ConnectionName, sub, config);
        var action = new RefreshSubscriptionAction(subNode);

        _mediator
            .Setup(m => m.Send(It.IsAny<GetSubscriptionResourceQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<GetSubscriptionResult>(sub));

        // Act
        await _sut.HandleRefreshSubscription(action, _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshSubscriptionSuccessAction>()), Times.Once);
    }

    [Fact]
    public async Task HandleRefreshSubscription_WhenException_DispatchesRefreshSubscriptionFailureAction()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var sub = ResourceTestData.CreateSubscription("sub-1", "topic-1");
        var subNode = new SubscriptionTreeNode(ConnectionName, sub, config);
        var action = new RefreshSubscriptionAction(subNode);

        _mediator
            .Setup(m => m.Send(It.IsAny<GetSubscriptionResourceQuery>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("refresh failed"));

        // Act
        await _sut.HandleRefreshSubscription(action, _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshSubscriptionFailureAction>()), Times.Once);
    }
    
    [Fact]
    public async Task HandleRefreshQueues_WhenSuccess_DispatchesRefreshQueuesSuccessAction()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queuesNode = new QueuesTreeNode(ConnectionName, config);
        var action = new RefreshQueuesAction(queuesNode);

        var queues = new[] { ResourceTestData.CreateQueue("queue-1") };
        _mediator
            .Setup(m => m.Send(It.IsAny<GetAllQueueResourcesQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IReadOnlyCollection<Resource.Queue>>(queues));

        // Act
        await _sut.HandleRefreshQueues(action, _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshQueuesSuccessAction>()), Times.Once);
    }

    [Fact]
    public async Task HandleRefreshQueues_WhenException_DispatchesRefreshQueuesFailureAction()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queuesNode = new QueuesTreeNode(ConnectionName, config);
        var action = new RefreshQueuesAction(queuesNode);

        _mediator
            .Setup(m => m.Send(It.IsAny<GetAllQueueResourcesQuery>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("refresh failed"));

        // Act
        await _sut.HandleRefreshQueues(action, _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshQueuesFailureAction>()), Times.Once);
    }
    
    [Fact]
    public async Task HandleRefreshTopics_WhenSuccess_DispatchesRefreshTopicsSuccessAction()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topicsNode = new TopicsTreeNode(ConnectionName, config);
        var action = new RefreshTopicsAction(topicsNode);

        var topics = new[] { ResourceTestData.CreateTopic("topic-1") };
        _mediator
            .Setup(m => m.Send(It.IsAny<GetAllTopicResourcesQuery>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IReadOnlyCollection<Resource.Topic>>(topics));

        // Act
        await _sut.HandleRefreshTopics(action, _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshTopicsSuccessAction>()), Times.Once);
    }

    [Fact]
    public async Task HandleRefreshTopics_WhenException_DispatchesRefreshTopicsFailureAction()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var topicsNode = new TopicsTreeNode(ConnectionName, config);
        var action = new RefreshTopicsAction(topicsNode);

        _mediator
            .Setup(m => m.Send(It.IsAny<GetAllTopicResourcesQuery>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("refresh failed"));

        // Act
        await _sut.HandleRefreshTopics(action, _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshTopicsFailureAction>()), Times.Once);
    }
}

