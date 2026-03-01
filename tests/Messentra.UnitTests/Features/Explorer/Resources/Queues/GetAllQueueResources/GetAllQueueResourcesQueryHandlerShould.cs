using Messentra.Domain;
using Messentra.Features.Explorer.Resources.Queues.GetAllQueueResources;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Moq;
using Shouldly;
using Xunit;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.UnitTests.Features.Explorer.Resources.Queues.GetAllQueueResources;

public sealed class GetAllQueueResourcesQueryHandlerShould
{
    private readonly Mock<IAzureServiceBusQueueProvider> _providerMock = new();
    private readonly GetAllQueueResourcesQueryHandler _sut;

    public GetAllQueueResourcesQueryHandlerShould()
    {
        _sut = new GetAllQueueResourcesQueryHandler(_providerMock.Object);
    }

    [Fact]
    public async Task ReturnAllQueues_WithConnectionString()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetAllQueueResourcesQuery(connectionConfig);
        var expectedQueues = new[] { CreateQueue("queue-1"), CreateQueue("queue-2") };

        _providerMock
            .Setup(x => x.GetAll(
                It.Is<ConnectionInfo.ConnectionString>(c => c.Value == connectionConfig.ConnectionStringConfig!.ConnectionString),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedQueues);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldBe(expectedQueues);
    }

    [Fact]
    public async Task ReturnAllQueues_WithEntraId()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant-id", "client-id");
        var query = new GetAllQueueResourcesQuery(connectionConfig);
        var expectedQueues = new[] { CreateQueue("queue-1") };

        _providerMock
            .Setup(x => x.GetAll(
                It.Is<ConnectionInfo.ManagedIdentity>(c =>
                    c.FullyQualifiedNamespace == connectionConfig.EntraIdConfig!.Namespace &&
                    c.TenantId == connectionConfig.EntraIdConfig.TenantId &&
                    c.ClientId == connectionConfig.EntraIdConfig.ClientId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedQueues);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldBe(expectedQueues);
    }

    [Fact]
    public async Task ReturnEmptyCollectionWhenNoQueuesExist()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetAllQueueResourcesQuery(connectionConfig);

        _providerMock
            .Setup(x => x.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ThrowInvalidOperationExceptionForUnsupportedConnectionType()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig((ConnectionType)999, null, null);
        var query = new GetAllQueueResourcesQuery(connectionConfig);

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

