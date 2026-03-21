using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using Messentra.Infrastructure.AzureServiceBus.Topics;
using Moq;
using Shouldly;
using Xunit;
using static Messentra.UnitTests.Infrastructure.AzureServiceBus.TestAsyncPageableHelper;
using static Messentra.UnitTests.Infrastructure.AzureServiceBus.ServiceBusModelFactoryHelper;
using AzureTopicProperties = Azure.Messaging.ServiceBus.Administration.TopicProperties;
using SubscriptionProperties = Messentra.Infrastructure.AzureServiceBus.SubscriptionProperties;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus.Topics;

public sealed class AzureServiceBusTopicProviderShould
{
    private const string ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey=";
    private const string Namespace = "test.servicebus.windows.net";

    private readonly Mock<IAzureServiceBusAdminClientFactory> _clientFactory = new();
    private readonly Mock<ServiceBusAdministrationClient> _adminClient = new();
    private readonly Mock<IAzureServiceBusSubscriptionProvider> _subscriptionProvider = new();
    private readonly AzureServiceBusResourceTopicProvider _sut;

    public AzureServiceBusTopicProviderShould()
    {
        _clientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .ReturnsAsync(_adminClient.Object);

        _sut = new AzureServiceBusResourceTopicProvider(_clientFactory.Object, _subscriptionProvider.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsAllTopics()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var topic1 = TopicProperties("topic-1");
        var topic2 = TopicProperties("topic-2");
        var runtime1 = ServiceBusModelFactory.TopicRuntimeProperties("topic-1");
        var runtime2 = ServiceBusModelFactory.TopicRuntimeProperties("topic-2");

        _adminClient
            .Setup(x => x.GetTopicsAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(topic1, topic2));

        _adminClient
            .Setup(x => x.GetTopicsRuntimePropertiesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(runtime1, runtime2));

        _subscriptionProvider
            .Setup(x => x.GetAll(info, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetAll(info, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAll_MapsTopicNameAndUrl()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var topic = TopicProperties("my-topic");
        var runtime = ServiceBusModelFactory.TopicRuntimeProperties("my-topic");

        _adminClient
            .Setup(x => x.GetTopicsAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(topic));

        _adminClient
            .Setup(x => x.GetTopicsRuntimePropertiesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(runtime));

        _subscriptionProvider
            .Setup(x => x.GetAll(info, "my-topic", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetAll(info, CancellationToken.None);

        // Assert
        var mapped = result.Single();
        mapped.Name.ShouldBe("my-topic");
        mapped.Url.ShouldBe($"https://{Namespace}/my-topic");
    }

    [Fact]
    public async Task GetAll_LoadsSubscriptionsForEachTopic()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var topic1 = TopicProperties("topic-1");
        var topic2 = TopicProperties("topic-2");
        var runtime = ServiceBusModelFactory.TopicRuntimeProperties("topic-1");
        var runtime2 = ServiceBusModelFactory.TopicRuntimeProperties("topic-2");

        var sub1 = new Resource.Subscription("sub-a", "topic-1", "https://url", new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new MessageInfo(0, 0, 0, 0, 0, 0), new SizeInfo(0, 0)), new SubscriptionProperties(TimeSpan.FromDays(1), TimeSpan.FromSeconds(30), TimeSpan.MaxValue, 10, false, null, null, false, string.Empty));
        var sub2 = new Resource.Subscription("sub-b", "topic-2", "https://url", new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new MessageInfo(0, 0, 0, 0, 0, 0), new SizeInfo(0, 0)), new SubscriptionProperties(TimeSpan.FromDays(1), TimeSpan.FromSeconds(30), TimeSpan.MaxValue, 10, false, null, null, false, string.Empty));

        _adminClient
            .Setup(x => x.GetTopicsAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(topic1, topic2));

        _adminClient
            .Setup(x => x.GetTopicsRuntimePropertiesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(runtime, runtime2));

        _subscriptionProvider
            .Setup(x => x.GetAll(It.IsAny<ConnectionInfo>(), "topic-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([sub1]);

        _subscriptionProvider
            .Setup(x => x.GetAll(It.IsAny<ConnectionInfo>(), "topic-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync([sub2]);

        // Act
        var result = await _sut.GetAll(info, CancellationToken.None);

        // Assert
        var topics = result.ToDictionary(t => t.Name);
        topics["topic-1"].Subscriptions.Single().Name.ShouldBe("sub-a");
        topics["topic-2"].Subscriptions.Single().Name.ShouldBe("sub-b");
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyCollection_WhenNoTopicsExist()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);

        _adminClient
            .Setup(x => x.GetTopicsAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<AzureTopicProperties>());

        _adminClient
            .Setup(x => x.GetTopicsRuntimePropertiesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<TopicRuntimeProperties>());

        // Act
        var result = await _sut.GetAll(info, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAll_MapsScheduledMessageCount()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var topic = TopicProperties("my-topic");
        var runtime = ServiceBusModelFactory.TopicRuntimeProperties("my-topic", scheduledMessageCount: 15);

        _adminClient
            .Setup(x => x.GetTopicsAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(topic));

        _adminClient
            .Setup(x => x.GetTopicsRuntimePropertiesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(runtime));

        _subscriptionProvider
            .Setup(x => x.GetAll(info, "my-topic", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetAll(info, CancellationToken.None);

        // Assert
        result.Single().Overview.MessageInfo.Scheduled.ShouldBe(15);
    }

    [Fact]
    public async Task GetByName_ReturnsMappedTopic()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var topic = TopicProperties("my-topic");
        var runtime = ServiceBusModelFactory.TopicRuntimeProperties("my-topic", scheduledMessageCount: 3);

        _adminClient
            .Setup(x => x.GetTopicAsync("my-topic", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(topic, Mock.Of<Response>()));

        _adminClient
            .Setup(x => x.GetTopicRuntimePropertiesAsync("my-topic", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(runtime, Mock.Of<Response>()));

        _subscriptionProvider
            .Setup(x => x.GetAll(info, "my-topic", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetByName(info, "my-topic", CancellationToken.None);

        // Assert
        result.Name.ShouldBe("my-topic");
        result.Url.ShouldBe($"https://{Namespace}/my-topic");
        result.Overview.MessageInfo.Scheduled.ShouldBe(3);
    }

    [Fact]
    public async Task GetByName_MapsTopicProperties()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var topic = TopicProperties(
            "my-topic",
            defaultMessageTimeToLive: TimeSpan.FromDays(3),
            autoDeleteOnIdle: TimeSpan.FromDays(14),
            enablePartitioning: true,
            requiresDuplicateDetection: false);
        var runtime = ServiceBusModelFactory.TopicRuntimeProperties("my-topic");

        _adminClient
            .Setup(x => x.GetTopicAsync("my-topic", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(topic, Mock.Of<Response>()));

        _adminClient
            .Setup(x => x.GetTopicRuntimePropertiesAsync("my-topic", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(runtime, Mock.Of<Response>()));

        _subscriptionProvider
            .Setup(x => x.GetAll(info, "my-topic", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetByName(info, "my-topic", CancellationToken.None);

        // Assert
        result.Properties.DefaultMessageTimeToLive.ShouldBe(TimeSpan.FromDays(3));
        result.Properties.AutoDeleteOnIdle.ShouldBe(TimeSpan.FromDays(14));
        result.Properties.EnablePartitioning.ShouldBeTrue();
        result.Properties.RequiresDuplicateDetection.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByName_LoadsSubscriptionsForTopic()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var topic = TopicProperties("my-topic");
        var runtime = ServiceBusModelFactory.TopicRuntimeProperties("my-topic");

        var sub = new Resource.Subscription("my-sub", "my-topic", "https://url", new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new MessageInfo(0, 0, 0, 0, 0, 0), new SizeInfo(0, 0)), new SubscriptionProperties(TimeSpan.FromDays(1), TimeSpan.FromSeconds(30), TimeSpan.MaxValue, 10, false, null, null, false, string.Empty));

        _adminClient
            .Setup(x => x.GetTopicAsync("my-topic", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(topic, Mock.Of<Response>()));

        _adminClient
            .Setup(x => x.GetTopicRuntimePropertiesAsync("my-topic", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(runtime, Mock.Of<Response>()));

        _subscriptionProvider
            .Setup(x => x.GetAll(info, "my-topic", It.IsAny<CancellationToken>()))
            .ReturnsAsync([sub]);

        // Act
        var result = await _sut.GetByName(info, "my-topic", CancellationToken.None);

        // Assert
        result.Subscriptions.Single().Name.ShouldBe("my-sub");
    }
}

