using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using Moq;
using Shouldly;
using Xunit;
using static Messentra.UnitTests.Infrastructure.AzureServiceBus.TestAsyncPageableHelper;
using static Messentra.UnitTests.Infrastructure.AzureServiceBus.ServiceBusModelFactoryHelper;
using AzureSubscriptionProperties = Azure.Messaging.ServiceBus.Administration.SubscriptionProperties;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus.Subscriptions;

public sealed class AzureServiceBusSubscriptionProviderShould
{
    private const string ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey=";
    private const string Namespace = "test.servicebus.windows.net";
    private const string TopicName = "my-topic";

    private readonly Mock<IAzureServiceBusAdminClientFactory> _clientFactory = new();
    private readonly Mock<ServiceBusAdministrationClient> _adminClient = new();
    private readonly AzureServiceBusResourceSubscriptionProvider _sut;

    public AzureServiceBusSubscriptionProviderShould()
    {
        _clientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_adminClient.Object);

        _sut = new AzureServiceBusResourceSubscriptionProvider(_clientFactory.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsAllSubscriptions()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var sub1 = SubscriptionProperties(TopicName, "sub-1");
        var sub2 = SubscriptionProperties(TopicName, "sub-2");
        var runtime1 = ServiceBusModelFactory.SubscriptionRuntimeProperties(TopicName, "sub-1");
        var runtime2 = ServiceBusModelFactory.SubscriptionRuntimeProperties(TopicName, "sub-2");

        _adminClient
            .Setup(x => x.GetSubscriptionsAsync(TopicName, It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(sub1, sub2));

        _adminClient
            .Setup(x => x.GetSubscriptionsRuntimePropertiesAsync(TopicName, It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(runtime1, runtime2));

        // Act
        var result = await _sut.GetAll(info, TopicName, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAll_MapsNameUrlAndTopicName()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var sub = SubscriptionProperties(TopicName, "my-sub");
        var runtime = ServiceBusModelFactory.SubscriptionRuntimeProperties(TopicName, "my-sub");

        _adminClient
            .Setup(x => x.GetSubscriptionsAsync(TopicName, It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(sub));

        _adminClient
            .Setup(x => x.GetSubscriptionsRuntimePropertiesAsync(TopicName, It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(runtime));

        // Act
        var result = await _sut.GetAll(info, TopicName, CancellationToken.None);

        // Assert
        var mapped = result.Single();
        mapped.Name.ShouldBe("my-sub");
        mapped.TopicName.ShouldBe(TopicName);
        mapped.Url.ShouldBe($"https://{Namespace}/{TopicName}/subscriptions/my-sub");
    }

    [Fact]
    public async Task GetAll_MapsMessageCounts()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var sub = SubscriptionProperties(TopicName, "my-sub");
        var runtime = ServiceBusModelFactory.SubscriptionRuntimeProperties(
            TopicName,
            "my-sub",
            activeMessageCount: 8,
            deadLetterMessageCount: 2,
            transferMessageCount: 3,
            transferDeadLetterMessageCount: 1,
            totalMessageCount: 14);

        _adminClient
            .Setup(x => x.GetSubscriptionsAsync(TopicName, It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(sub));

        _adminClient
            .Setup(x => x.GetSubscriptionsRuntimePropertiesAsync(TopicName, It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(runtime));

        // Act
        var result = await _sut.GetAll(info, TopicName, CancellationToken.None);

        // Assert
        var mapped = result.Single();
        mapped.Overview.MessageInfo.Active.ShouldBe(8);
        mapped.Overview.MessageInfo.DeadLetter.ShouldBe(2);
        mapped.Overview.MessageInfo.Transfer.ShouldBe(3);
        mapped.Overview.MessageInfo.TransferDeadLetter.ShouldBe(1);
        mapped.Overview.MessageInfo.Total.ShouldBe(14);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyCollection_WhenNoSubscriptionsExist()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);

        _adminClient
            .Setup(x => x.GetSubscriptionsAsync(TopicName, It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<AzureSubscriptionProperties>());

        _adminClient
            .Setup(x => x.GetSubscriptionsRuntimePropertiesAsync(TopicName, It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<SubscriptionRuntimeProperties>());

        // Act
        var result = await _sut.GetAll(info, TopicName, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByName_ReturnsMappedSubscription()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var sub = SubscriptionProperties(TopicName, "my-sub");
        var runtime = ServiceBusModelFactory.SubscriptionRuntimeProperties(TopicName, "my-sub", activeMessageCount: 5);

        _adminClient
            .Setup(x => x.GetSubscriptionAsync(TopicName, "my-sub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(sub, Mock.Of<Response>()));

        _adminClient
            .Setup(x => x.GetSubscriptionRuntimePropertiesAsync(TopicName, "my-sub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(runtime, Mock.Of<Response>()));

        // Act
        var result = await _sut.GetByName(info, TopicName, "my-sub", CancellationToken.None);

        // Assert
        result.Name.ShouldBe("my-sub");
        result.TopicName.ShouldBe(TopicName);
        result.Url.ShouldBe($"https://{Namespace}/{TopicName}/subscriptions/my-sub");
        result.Overview.MessageInfo.Active.ShouldBe(5);
    }

    [Fact]
    public async Task GetByName_MapsSubscriptionProperties()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var sub = SubscriptionProperties(
            TopicName,
            "my-sub",
            lockDuration: TimeSpan.FromSeconds(60),
            defaultMessageTimeToLive: TimeSpan.FromDays(7),
            autoDeleteOnIdle: TimeSpan.FromDays(1),
            maxDeliveryCount: 10,
            deadLetteringOnMessageExpiration: true,
            requiresSession: true);
        var runtime = ServiceBusModelFactory.SubscriptionRuntimeProperties(TopicName, "my-sub");

        _adminClient
            .Setup(x => x.GetSubscriptionAsync(TopicName, "my-sub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(sub, Mock.Of<Response>()));

        _adminClient
            .Setup(x => x.GetSubscriptionRuntimePropertiesAsync(TopicName, "my-sub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(runtime, Mock.Of<Response>()));

        // Act
        var result = await _sut.GetByName(info, TopicName, "my-sub", CancellationToken.None);

        // Assert
        result.Properties.LockDuration.ShouldBe(TimeSpan.FromSeconds(60));
        result.Properties.DefaultMessageTimeToLive.ShouldBe(TimeSpan.FromDays(7));
        result.Properties.MaxDeliveryCount.ShouldBe(10);
        result.Properties.DeadLetteringOnMessageExpiration.ShouldBeTrue();
        result.Properties.RequiresSession.ShouldBeTrue();
    }
}

