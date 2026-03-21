using Azure.Messaging.ServiceBus;
using Messentra.Features.Explorer.Messages;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Moq;
using Shouldly;
using Xunit;
using SubQueue = Messentra.Features.Explorer.Messages.SubQueue;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus.Queues;

public sealed class AzureServiceBusQueueMessagesProviderShould
{
    private const string ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey=";
    private const string QueueName = "my-queue";

    private readonly Mock<IAzureServiceBusClientFactory> _clientFactory = new();
    private readonly Mock<ServiceBusClient> _client = new();
    private readonly Mock<ServiceBusReceiver> _receiver = new();
    private readonly AzureServiceBusQueueMessagesProvider _sut;

    public AzureServiceBusQueueMessagesProviderShould()
    {
        _clientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_client.Object);

        _client
            .Setup(x => x.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>()))
            .Returns(_receiver.Object);

        _sut = new AzureServiceBusQueueMessagesProvider(_clientFactory.Object);
    }

    [Fact]
    public async Task Get_PeekMode_CallsPeekMessagesAsync()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var options = new FetchMessagesOptions(FetchMode.Peek, FetchReceiveMode.PeekLock, 5, null, TimeSpan.FromSeconds(10));
        var messages = CreateMessages(5);

        _receiver
            .Setup(x => x.PeekMessagesAsync(5, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _sut.Get(info, QueueName, options, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(5);
        _receiver.Verify(x => x.PeekMessagesAsync(5, null, It.IsAny<CancellationToken>()), Times.Once);
        _receiver.Verify(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Get_ReceiveMode_CallsReceiveMessagesAsync()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var options = new FetchMessagesOptions(FetchMode.Receive, FetchReceiveMode.PeekLock, 5, null, TimeSpan.FromSeconds(10));
        var messages = CreateMessages(5);

        _receiver
            .Setup(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _sut.Get(info, QueueName, options, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(5);
        _receiver.Verify(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
        _receiver.Verify(x => x.PeekMessagesAsync(It.IsAny<int>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Get_PeekMode_PassesStartSequenceNumber()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var options = new FetchMessagesOptions(FetchMode.Peek, FetchReceiveMode.PeekLock, 3, 42L, TimeSpan.FromSeconds(10));

        _receiver
            .Setup(x => x.PeekMessagesAsync(3, 42L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMessages(3));

        // Act
        await _sut.Get(info, QueueName, options, CancellationToken.None);

        // Assert
        _receiver.Verify(x => x.PeekMessagesAsync(3, 42L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_DeadLetterSubQueue_CreatesReceiverWithDeadLetterSubQueue()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var options = new FetchMessagesOptions(FetchMode.Peek, FetchReceiveMode.PeekLock, 1, null, TimeSpan.FromSeconds(10), SubQueue.DeadLetter);

        _receiver
            .Setup(x => x.PeekMessagesAsync(It.IsAny<int>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMessages(1));

        // Act
        await _sut.Get(info, QueueName, options, CancellationToken.None);

        // Assert
        _client.Verify(x => x.CreateReceiver(
            QueueName,
            It.Is<ServiceBusReceiverOptions>(o => o.SubQueue == Azure.Messaging.ServiceBus.SubQueue.DeadLetter)),
            Times.Once);
    }

    [Fact]
    public async Task Get_ActiveSubQueue_CreatesReceiverWithNoneSubQueue()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var options = new FetchMessagesOptions(FetchMode.Peek, FetchReceiveMode.PeekLock, 1, null, TimeSpan.FromSeconds(10));

        _receiver
            .Setup(x => x.PeekMessagesAsync(It.IsAny<int>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMessages(1));

        // Act
        await _sut.Get(info, QueueName, options, CancellationToken.None);

        // Assert
        _client.Verify(x => x.CreateReceiver(
            QueueName,
            It.Is<ServiceBusReceiverOptions>(o => o.SubQueue == Azure.Messaging.ServiceBus.SubQueue.None)),
            Times.Once);
    }

    [Fact]
    public async Task Get_PeekMode_UsesReceiveModeOfPeekLock()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var options = new FetchMessagesOptions(FetchMode.Peek, FetchReceiveMode.PeekLock, 1, null, TimeSpan.FromSeconds(10));

        _receiver
            .Setup(x => x.PeekMessagesAsync(It.IsAny<int>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMessages(1));

        // Act
        await _sut.Get(info, QueueName, options, CancellationToken.None);

        // Assert
        _client.Verify(x => x.CreateReceiver(
            QueueName,
            It.Is<ServiceBusReceiverOptions>(o => o.ReceiveMode == ServiceBusReceiveMode.PeekLock)),
            Times.Once);
    }

    [Fact]
    public async Task Get_ReceiveModeReceiveAndDelete_UsesReceiveAndDeleteMode()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var options = new FetchMessagesOptions(FetchMode.Receive, FetchReceiveMode.ReceiveAndDelete, 1, null, TimeSpan.FromSeconds(10));

        _receiver
            .Setup(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMessages(1));

        // Act
        await _sut.Get(info, QueueName, options, CancellationToken.None);

        // Assert
        _client.Verify(x => x.CreateReceiver(
            QueueName,
            It.Is<ServiceBusReceiverOptions>(o => o.ReceiveMode == ServiceBusReceiveMode.ReceiveAndDelete)),
            Times.Once);
    }

    [Fact]
    public async Task Get_StopsFetchingWhenBatchIsEmpty()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var options = new FetchMessagesOptions(FetchMode.Receive, FetchReceiveMode.PeekLock, 10, null, TimeSpan.FromSeconds(10));

        _receiver
            .SetupSequence(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMessages(3))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.Get(info, QueueName, options, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(3);
        _receiver.Verify(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Get_FetchesMultipleBatchesUntilMessageCountReached()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var options = new FetchMessagesOptions(FetchMode.Receive, FetchReceiveMode.PeekLock, 5, null, TimeSpan.FromSeconds(10));

        _receiver
            .SetupSequence(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMessages(3))
            .ReturnsAsync(CreateMessages(2));

        // Act
        var result = await _sut.Get(info, QueueName, options, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(5);
        _receiver.Verify(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Get_ReturnsEmptyCollection_WhenFirstBatchIsEmpty()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var options = new FetchMessagesOptions(FetchMode.Receive, FetchReceiveMode.PeekLock, 5, null, TimeSpan.FromSeconds(10));

        _receiver
            .Setup(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.Get(info, QueueName, options, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_MapsMessageBodyAndBrokerProperties()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var options = new FetchMessagesOptions(FetchMode.Peek, FetchReceiveMode.PeekLock, 1, null, TimeSpan.FromSeconds(10));
        var rawMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{\"key\":\"value\"}"),
            messageId: "msg-1",
            sequenceNumber: 100,
            correlationId: "corr-1",
            subject: "test-subject",
            contentType: "application/json");

        _receiver
            .Setup(x => x.PeekMessagesAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([rawMessage]);

        // Act
        var result = await _sut.Get(info, QueueName, options, CancellationToken.None);

        // Assert
        var msg = result.Single();
        msg.Message.Body.ShouldBe("{\"key\":\"value\"}");
        msg.Message.BrokerProperties.MessageId.ShouldBe("msg-1");
        msg.Message.BrokerProperties.SequenceNumber.ShouldBe(100L);
        msg.Message.BrokerProperties.CorrelationId.ShouldBe("corr-1");
        msg.Message.BrokerProperties.Label.ShouldBe("test-subject");
        msg.Message.BrokerProperties.ContentType.ShouldBe("application/json");
    }

    [Fact]
    public async Task Get_CreatesReceiverWithCorrectQueueName()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var options = new FetchMessagesOptions(FetchMode.Peek, FetchReceiveMode.PeekLock, 1, null, TimeSpan.FromSeconds(10));

        _receiver
            .Setup(x => x.PeekMessagesAsync(It.IsAny<int>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMessages(1));

        // Act
        await _sut.Get(info, "specific-queue", options, CancellationToken.None);

        // Assert
        _client.Verify(x => x.CreateReceiver("specific-queue", It.IsAny<ServiceBusReceiverOptions>()), Times.Once);
    }

    private static IReadOnlyList<ServiceBusReceivedMessage> CreateMessages(int count) =>
        Enumerable.Range(0, count)
            .Select(i => ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromString($"message-{i}"),
                messageId: $"id-{i}"))
            .ToList();
}

