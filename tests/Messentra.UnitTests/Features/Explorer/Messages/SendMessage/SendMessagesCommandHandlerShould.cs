using Messentra.Domain;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Features.Explorer.Resources;
using Messentra.Infrastructure.AzureServiceBus;
using Moq;
using Shouldly;
using Xunit;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.UnitTests.Features.Explorer.Messages.SendMessage;

public sealed class SendMessagesCommandHandlerShould
{
    private readonly Mock<IAzureServiceBusSender> _senderMock = new();
    private readonly SendMessagesCommandHandler _sut;

    private static readonly ConnectionConfig ConnectionStringConfig =
        ConnectionConfig.CreateConnectionString(
            "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");

    public SendMessagesCommandHandlerShould()
    {
        _sut = new SendMessagesCommandHandler(_senderMock.Object);
    }

    [Fact]
    public async Task SendAllMessages_WhenBatchChunkCanFitAll()
    {
        // Arrange
        var command = BuildCommand([BuildBatchItem(1), BuildBatchItem(2)]);

        _senderMock
            .Setup(x => x.SendBatchChunk(
                It.IsAny<ConnectionInfo>(),
                "my-queue",
                It.IsAny<IReadOnlyList<SendMessageBatchItem>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.TotalCount.ShouldBe(2);
        result.SentCount.ShouldBe(2);
        result.FailedCount.ShouldBe(0);
        result.SentSequenceNumbers.ShouldBe([1, 2]);
        _senderMock.Verify(x => x.Send(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<SendMessageBatchItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReturnPartialResult_WhenSingleFallbackFailsForSomeMessages()
    {
        // Arrange
        var command = BuildCommand([BuildBatchItem(1), BuildBatchItem(2)]);

        _senderMock
            .Setup(x => x.SendBatchChunk(
                It.IsAny<ConnectionInfo>(),
                "my-queue",
                It.IsAny<IReadOnlyList<SendMessageBatchItem>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _senderMock
            .Setup(x => x.Send(
                It.IsAny<ConnectionInfo>(),
                "my-queue",
                It.Is<SendMessageBatchItem>(m => m.SourceSequenceNumber == 1),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _senderMock
            .Setup(x => x.Send(
                It.IsAny<ConnectionInfo>(),
                "my-queue",
                It.Is<SendMessageBatchItem>(m => m.SourceSequenceNumber == 2),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("send failed"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.TotalCount.ShouldBe(2);
        result.SentCount.ShouldBe(1);
        result.FailedCount.ShouldBe(1);
        result.SentSequenceNumbers.ShouldBe([1]);
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].SourceSequenceNumber.ShouldBe(2);
        result.Errors[0].Message.ShouldBe("send failed");
    }

    [Fact]
    public async Task FallbackToSingleSend_WhenBatchReturnsZeroForRemainingMessages()
    {
        // Arrange
        var command = BuildCommand([BuildBatchItem(1), BuildBatchItem(2)]);

        _senderMock
            .Setup(x => x.SendBatchChunk(
                It.IsAny<ConnectionInfo>(),
                "my-queue",
                It.Is<IReadOnlyList<SendMessageBatchItem>>(messages => messages.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _senderMock
            .Setup(x => x.SendBatchChunk(
                It.IsAny<ConnectionInfo>(),
                "my-queue",
                It.Is<IReadOnlyList<SendMessageBatchItem>>(messages => messages.Count == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _senderMock
            .Setup(x => x.Send(
                It.IsAny<ConnectionInfo>(),
                "my-queue",
                It.Is<SendMessageBatchItem>(m => m.SourceSequenceNumber == 2),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.SentCount.ShouldBe(2);
        result.FailedCount.ShouldBe(0);
        _senderMock.Verify(x => x.SendBatchChunk(It.IsAny<ConnectionInfo>(), "my-queue", It.IsAny<IReadOnlyList<SendMessageBatchItem>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _senderMock.Verify(x => x.Send(It.IsAny<ConnectionInfo>(), "my-queue", It.IsAny<SendMessageBatchItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PreserveInputOrder_WhenCallingBatchChunk()
    {
        // Arrange
        var command = BuildCommand([BuildBatchItem(3), BuildBatchItem(1), BuildBatchItem(2)]);
        IReadOnlyList<SendMessageBatchItem>? captured = null;

        _senderMock
            .Setup(x => x.SendBatchChunk(
                It.IsAny<ConnectionInfo>(),
                "my-queue",
                It.IsAny<IReadOnlyList<SendMessageBatchItem>>(),
                It.IsAny<CancellationToken>()))
            .Callback<ConnectionInfo, string, IReadOnlyList<SendMessageBatchItem>, CancellationToken>((_, _, list, _) => captured = list)
            .ReturnsAsync(3);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        captured.ShouldNotBeNull();
        captured.Select(x => x.SourceSequenceNumber).ShouldBe([3, 1, 2]);
    }

    private static SendMessagesCommand BuildCommand(IReadOnlyList<SendMessageBatchItem> messages) =>
        new(
            ResourceTreeNode: new QueueTreeNode("conn", CreateQueue("my-queue"), ConnectionStringConfig),
            Messages: messages);

    private static SendMessageBatchItem BuildBatchItem(long sourceSequenceNumber) =>
        new(
            SourceSequenceNumber: sourceSequenceNumber,
            Body: "hello",
            MessageId: null,
            Label: null,
            CorrelationId: null,
            SessionId: null,
            ReplyToSessionId: null,
            PartitionKey: null,
            ScheduledEnqueueTimeUtc: null,
            TimeToLive: null,
            To: null,
            ReplyTo: null,
            ContentType: null,
            ApplicationProperties: new Dictionary<string, object>());

    private static Resource.Queue CreateQueue(string name) =>
        new(
            name,
            $"https://test.servicebus.windows.net/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0),
                new SizeInfo(0, 1024)),
            new QueueProperties(
                TimeSpan.FromDays(14), TimeSpan.FromSeconds(60), TimeSpan.MaxValue,
                10, false, null, null, false, false, TimeSpan.FromMinutes(1), false, 256, string.Empty));
}


