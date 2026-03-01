using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Moq;
using Shouldly;
using Xunit;
using static Messentra.UnitTests.Infrastructure.AzureServiceBus.TestAsyncPageableHelper;
using static Messentra.UnitTests.Infrastructure.AzureServiceBus.ServiceBusModelFactoryHelper;
using AzureQueueProperties = Azure.Messaging.ServiceBus.Administration.QueueProperties;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus.Queues;

public sealed class AzureServiceBusQueueProviderShould
{
    private const string ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey=";
    private const string Namespace = "test.servicebus.windows.net";

    private readonly Mock<IAzureServiceBusAdminClientFactory> _clientFactory = new();
    private readonly Mock<ServiceBusAdministrationClient> _adminClient = new();
    private readonly AzureServiceBusResourceQueueProvider _sut;

    public AzureServiceBusQueueProviderShould()
    {
        _clientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(_adminClient.Object);

        _sut = new AzureServiceBusResourceQueueProvider(_clientFactory.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsAllQueues()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);

        var queue1 = QueueProperties("queue-1");
        var queue2 = QueueProperties("queue-2");
        var runtime1 = ServiceBusModelFactory.QueueRuntimeProperties("queue-1", activeMessageCount: 5, deadLetterMessageCount: 1, scheduledMessageCount: 2, sizeInBytes: 1024);
        var runtime2 = ServiceBusModelFactory.QueueRuntimeProperties("queue-2", activeMessageCount: 10, deadLetterMessageCount: 0, scheduledMessageCount: 0, sizeInBytes: 2048);

        _adminClient
            .Setup(x => x.GetQueuesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(queue1, queue2));

        _adminClient
            .Setup(x => x.GetQueuesRuntimePropertiesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(runtime1, runtime2));

        // Act
        var result = await _sut.GetAll(info, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAll_MapsQueueNameAndUrl()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var queue = QueueProperties("my-queue");
        var runtime = ServiceBusModelFactory.QueueRuntimeProperties("my-queue");

        _adminClient
            .Setup(x => x.GetQueuesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(queue));

        _adminClient
            .Setup(x => x.GetQueuesRuntimePropertiesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(runtime));

        // Act
        var result = await _sut.GetAll(info, CancellationToken.None);

        // Assert
        var mapped = result.Single();
        mapped.Name.ShouldBe("my-queue");
        mapped.Url.ShouldBe($"https://{Namespace}/my-queue");
    }

    [Fact]
    public async Task GetAll_MapsMessageCounts()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var queue = QueueProperties("my-queue");
        var runtime = ServiceBusModelFactory.QueueRuntimeProperties(
            "my-queue",
            activeMessageCount: 10,
            deadLetterMessageCount: 3,
            scheduledMessageCount: 2,
            transferMessageCount: 1,
            transferDeadLetterMessageCount: 4,
            totalMessageCount: 20);

        _adminClient
            .Setup(x => x.GetQueuesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(queue));

        _adminClient
            .Setup(x => x.GetQueuesRuntimePropertiesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(runtime));

        // Act
        var result = await _sut.GetAll(info, CancellationToken.None);

        // Assert
        var mapped = result.Single();
        mapped.Overview.MessageInfo.Active.ShouldBe(10);
        mapped.Overview.MessageInfo.DeadLetter.ShouldBe(3);
        mapped.Overview.MessageInfo.Scheduled.ShouldBe(2);
        mapped.Overview.MessageInfo.Transfer.ShouldBe(1);
        mapped.Overview.MessageInfo.TransferDeadLetter.ShouldBe(4);
        mapped.Overview.MessageInfo.Total.ShouldBe(20);
    }

    [Fact]
    public async Task GetAll_MapsSizeInfo()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var queue = QueueProperties("my-queue", maxSizeInMegabytes: 1024);
        var runtime = ServiceBusModelFactory.QueueRuntimeProperties("my-queue", sizeInBytes: 512);

        _adminClient
            .Setup(x => x.GetQueuesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(queue));

        _adminClient
            .Setup(x => x.GetQueuesRuntimePropertiesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable(runtime));

        // Act
        var result = await _sut.GetAll(info, CancellationToken.None);

        // Assert
        var mapped = result.Single();
        mapped.Overview.SizeInfo.CurrentSizeInBytes.ShouldBe(512);
        mapped.Overview.SizeInfo.MaxSizeInMegabytes.ShouldBe(1024);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyCollection_WhenNoQueuesExist()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);

        _adminClient
            .Setup(x => x.GetQueuesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<AzureQueueProperties>());

        _adminClient
            .Setup(x => x.GetQueuesRuntimePropertiesAsync(It.IsAny<CancellationToken>()))
            .Returns(AsyncPageable<QueueRuntimeProperties>());

        // Act
        var result = await _sut.GetAll(info, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByName_ReturnsMappedQueue()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var queue = QueueProperties("my-queue", maxSizeInMegabytes: 1024);
        var runtime = ServiceBusModelFactory.QueueRuntimeProperties("my-queue", activeMessageCount: 7, sizeInBytes: 256);

        _adminClient
            .Setup(x => x.GetQueueAsync("my-queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(queue, Mock.Of<Response>()));

        _adminClient
            .Setup(x => x.GetQueueRuntimePropertiesAsync("my-queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(runtime, Mock.Of<Response>()));

        // Act
        var result = await _sut.GetByName(info, "my-queue", CancellationToken.None);

        // Assert
        result.Name.ShouldBe("my-queue");
        result.Url.ShouldBe($"https://{Namespace}/my-queue");
        result.Overview.MessageInfo.Active.ShouldBe(7);
        result.Overview.SizeInfo.CurrentSizeInBytes.ShouldBe(256);
        result.Overview.SizeInfo.MaxSizeInMegabytes.ShouldBe(1024);
    }

    [Fact]
    public async Task GetByName_MapsQueueProperties()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var queue = QueueProperties(
            "my-queue",
            lockDuration: TimeSpan.FromSeconds(30),
            defaultMessageTimeToLive: TimeSpan.FromDays(1),
            autoDeleteOnIdle: TimeSpan.FromDays(1),
            maxDeliveryCount: 5,
            deadLetteringOnMessageExpiration: true,
            requiresSession: false,
            requiresDuplicateDetection: true,
            enablePartitioning: false);
        var runtime = ServiceBusModelFactory.QueueRuntimeProperties("my-queue");

        _adminClient
            .Setup(x => x.GetQueueAsync("my-queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(queue, Mock.Of<Response>()));

        _adminClient
            .Setup(x => x.GetQueueRuntimePropertiesAsync("my-queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(runtime, Mock.Of<Response>()));

        // Act
        var result = await _sut.GetByName(info, "my-queue", CancellationToken.None);

        // Assert
        result.Properties.LockDuration.ShouldBe(TimeSpan.FromSeconds(30));
        result.Properties.DefaultMessageTimeToLive.ShouldBe(TimeSpan.FromDays(1));
        result.Properties.MaxDeliveryCount.ShouldBe(5);
        result.Properties.DeadLetteringOnMessageExpiration.ShouldBeTrue();
        result.Properties.RequiresDuplicateDetection.ShouldBeTrue();
        result.Properties.EnablePartitioning.ShouldBeFalse();
    }

    private static AzureQueueProperties CreateTestQueue(string name)
    {
        return QueueProperties(name: name, lockDuration: TimeSpan.FromSeconds(30));
    }
}

