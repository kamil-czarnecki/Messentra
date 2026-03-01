using Azure.Messaging.ServiceBus;
using Messentra.Domain;
using Messentra.Features.Explorer.Resources.Subscriptions;
using Messentra.Features.Explorer.Resources.Subscriptions.GetSubscriptionResource;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using Moq;
using Shouldly;
using Xunit;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.UnitTests.Features.Explorer.Resources.Subscriptions.GetSubscriptionResource;

public sealed class GetSubscriptionResourceQueryHandlerShould
{
    private readonly Mock<IAzureServiceBusSubscriptionProvider> _providerMock = new();
    private readonly GetSubscriptionResourceQueryHandler _sut;

    public GetSubscriptionResourceQueryHandlerShould()
    {
        _sut = new GetSubscriptionResourceQueryHandler(_providerMock.Object);
    }

    [Fact]
    public async Task ReturnSubscriptionWhenFound_WithConnectionString()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetSubscriptionResourceQuery("test-topic", "test-subscription", connectionConfig);
        var expectedSubscription = CreateSubscription("test-subscription", "test-topic");

        _providerMock
            .Setup(x => x.GetByName(
                It.Is<ConnectionInfo.ConnectionString>(c => c.Value == connectionConfig.ConnectionStringConfig!.ConnectionString),
                "test-topic",
                "test-subscription",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSubscription);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBe(expectedSubscription);
    }

    [Fact]
    public async Task ReturnSubscriptionWhenFound_WithEntraId()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant-id", "client-id");
        var query = new GetSubscriptionResourceQuery("test-topic", "test-subscription", connectionConfig);
        var expectedSubscription = CreateSubscription("test-subscription", "test-topic");

        _providerMock
            .Setup(x => x.GetByName(
                It.Is<ConnectionInfo.ManagedIdentity>(c =>
                    c.FullyQualifiedNamespace == connectionConfig.EntraIdConfig!.Namespace &&
                    c.TenantId == connectionConfig.EntraIdConfig.TenantId &&
                    c.ClientId == connectionConfig.EntraIdConfig.ClientId),
                "test-topic",
                "test-subscription",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSubscription);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBe(expectedSubscription);
    }

    [Fact]
    public async Task ReturnSubscriptionNotFoundWhenEntityDoesNotExist()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetSubscriptionResourceQuery("test-topic", "missing-subscription", connectionConfig);

        _providerMock
            .Setup(x => x.GetByName(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("Entity not found", ServiceBusFailureReason.MessagingEntityNotFound));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<SubscriptionNotFound>();
    }

    [Fact]
    public async Task RethrowServiceBusExceptionForOtherReasons()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetSubscriptionResourceQuery("test-topic", "test-subscription", connectionConfig);

        _providerMock
            .Setup(x => x.GetByName(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("Service communication problem", ServiceBusFailureReason.ServiceCommunicationProblem));

        // Act & Assert
        await Should.ThrowAsync<ServiceBusException>(() => _sut.Handle(query, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task ThrowInvalidOperationExceptionForUnsupportedConnectionType()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig((ConnectionType)999, null, null);
        var query = new GetSubscriptionResourceQuery("test-topic", "test-subscription", connectionConfig);

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

