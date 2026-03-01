using Messentra.Domain;
using Messentra.Features.Explorer.Resources.Subscriptions.GetAllSubscriptionResources;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using Moq;
using Shouldly;
using Xunit;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.UnitTests.Features.Explorer.Resources.Subscriptions.GetAllSubscriptionResources;

public sealed class GetAllSubscriptionResourcesQueryHandlerShould
{
    private readonly Mock<IAzureServiceBusSubscriptionProvider> _providerMock = new();
    private readonly GetAllSubscriptionResourcesQueryHandler _sut;

    public GetAllSubscriptionResourcesQueryHandlerShould()
    {
        _sut = new GetAllSubscriptionResourcesQueryHandler(_providerMock.Object);
    }

    [Fact]
    public async Task ReturnAllSubscriptions_WithConnectionString()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetAllSubscriptionResourcesQuery("test-topic", connectionConfig);
        var expectedSubscriptions = new[]
        {
            CreateSubscription("sub-1", "test-topic"),
            CreateSubscription("sub-2", "test-topic")
        };

        _providerMock
            .Setup(x => x.GetAll(
                It.Is<ConnectionInfo.ConnectionString>(c => c.Value == connectionConfig.ConnectionStringConfig!.ConnectionString),
                "test-topic",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSubscriptions);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldBe(expectedSubscriptions);
    }

    [Fact]
    public async Task ReturnAllSubscriptions_WithEntraId()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant-id", "client-id");
        var query = new GetAllSubscriptionResourcesQuery("test-topic", connectionConfig);
        var expectedSubscriptions = new[] { CreateSubscription("sub-1", "test-topic") };

        _providerMock
            .Setup(x => x.GetAll(
                It.Is<ConnectionInfo.ManagedIdentity>(c =>
                    c.FullyQualifiedNamespace == connectionConfig.EntraIdConfig!.Namespace &&
                    c.TenantId == connectionConfig.EntraIdConfig.TenantId &&
                    c.ClientId == connectionConfig.EntraIdConfig.ClientId),
                "test-topic",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSubscriptions);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldBe(expectedSubscriptions);
    }

    [Fact]
    public async Task ReturnEmptyCollectionWhenNoSubscriptionsExist()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetAllSubscriptionResourcesQuery("test-topic", connectionConfig);

        _providerMock
            .Setup(x => x.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task PassTopicNameToProvider()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetAllSubscriptionResourcesQuery("my-specific-topic", connectionConfig);

        _providerMock
            .Setup(x => x.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        _providerMock.Verify(x => x.GetAll(It.IsAny<ConnectionInfo>(), "my-specific-topic", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ThrowInvalidOperationExceptionForUnsupportedConnectionType()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig((ConnectionType)999, null, null);
        var query = new GetAllSubscriptionResourcesQuery("test-topic", connectionConfig);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.Handle(query, CancellationToken.None).AsTask());
    }

    private static Resource.Subscription CreateSubscription(string name, string topicName) =>
        new(
            name,
            topicName,
            $"https://test.servicebus.windows.net/{topicName}/subscriptions/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0),
                new SizeInfo(0, 1024)),
            new SubscriptionProperties(
                TimeSpan.FromDays(14),
                TimeSpan.FromSeconds(60),
                TimeSpan.MaxValue,
                10,
                false,
                null,
                null,
                false,
                string.Empty));
}

