using Azure.Messaging.ServiceBus;
using Messentra.Domain;
using Messentra.Features.Explorer.Resources.Queues;
using Messentra.Features.Explorer.Resources.Queues.GetQueueResource;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Moq;
using Shouldly;
using Xunit;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.UnitTests.Features.Explorer.Resources.Queues.GetQueueResource;

public sealed class GetQueueResourceQueryHandlerShould
{
    private readonly Mock<IAzureServiceBusQueueProvider> _providerMock = new();
    private readonly GetQueueResourceQueryHandler _sut;

    public GetQueueResourceQueryHandlerShould()
    {
        _sut = new GetQueueResourceQueryHandler(_providerMock.Object);
    }

    [Fact]
    public async Task ReturnQueueWhenFound_WithConnectionString()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetQueueResourceQuery("test-queue", connectionConfig);
        var expectedQueue = CreateQueue("test-queue");

        _providerMock
            .Setup(x => x.GetByName(
                It.Is<ConnectionInfo.ConnectionString>(c => c.Value == connectionConfig.ConnectionStringConfig!.ConnectionString),
                "test-queue",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedQueue);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBe(expectedQueue);
    }

    [Fact]
    public async Task ReturnQueueWhenFound_WithEntraId()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant-id", "client-id");
        var query = new GetQueueResourceQuery("test-queue", connectionConfig);
        var expectedQueue = CreateQueue("test-queue");

        _providerMock
            .Setup(x => x.GetByName(
                It.Is<ConnectionInfo.ManagedIdentity>(c =>
                    c.FullyQualifiedNamespace == connectionConfig.EntraIdConfig!.Namespace &&
                    c.TenantId == connectionConfig.EntraIdConfig.TenantId &&
                    c.ClientId == connectionConfig.EntraIdConfig.ClientId),
                "test-queue",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedQueue);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBe(expectedQueue);
    }

    [Fact]
    public async Task ReturnQueueNotFoundWhenEntityDoesNotExist()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetQueueResourceQuery("missing-queue", connectionConfig);

        _providerMock
            .Setup(x => x.GetByName(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("Entity not found", ServiceBusFailureReason.MessagingEntityNotFound));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<QueueNotFound>();
    }

    [Fact]
    public async Task RethrowServiceBusExceptionForOtherReasons()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetQueueResourceQuery("test-queue", connectionConfig);

        _providerMock
            .Setup(x => x.GetByName(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("Service communication problem", ServiceBusFailureReason.ServiceCommunicationProblem));

        // Act & Assert
        await Should.ThrowAsync<ServiceBusException>(() => _sut.Handle(query, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task ThrowInvalidOperationExceptionForUnsupportedConnectionType()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig((ConnectionType)999, null, null);
        var query = new GetQueueResourceQuery("test-queue", connectionConfig);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.Handle(query, CancellationToken.None).AsTask());
    }

    private static Resource.Queue CreateQueue(string name) =>
        new(
            name,
            $"https://test.servicebus.windows.net/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0),
                new SizeInfo(0, 1024)),
            new QueueProperties(
                TimeSpan.FromDays(14),
                TimeSpan.FromSeconds(60),
                TimeSpan.MaxValue,
                10,
                false,
                null,
                null,
                false,
                false,
                TimeSpan.FromMinutes(1),
                false,
                256,
                string.Empty));
}

