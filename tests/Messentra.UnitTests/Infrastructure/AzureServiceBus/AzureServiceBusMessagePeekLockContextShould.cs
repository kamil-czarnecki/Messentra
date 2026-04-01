using Azure.Messaging.ServiceBus;
using Messentra.Infrastructure.AzureServiceBus;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus;

public sealed class AzureServiceBusMessagePeekLockContextShould
{
    private readonly Mock<ServiceBusReceiver> _receiver = new();
    private readonly Mock<ServiceBusSender> _sender = new();

    private AzureServiceBusMessagePeekLockContext CreateSut(ServiceBusReceivedMessage message) =>
        new(_receiver.Object, _sender.Object, message);

    [Fact]
    public async Task Complete_DelegatesToReceiverCompleteMessageAsync()
    {
        // Arrange
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("body"),
            messageId: "msg-1");
        var sut = CreateSut(message);

        // Act
        await sut.Complete(CancellationToken.None);

        // Assert
        _receiver.Verify(
            x => x.CompleteMessageAsync(message, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Abandon_DelegatesToReceiverAbandonMessageAsync()
    {
        // Arrange
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("body"),
            messageId: "msg-1");
        var sut = CreateSut(message);

        // Act
        await sut.Abandon(CancellationToken.None);

        // Assert
        _receiver.Verify(
            x => x.AbandonMessageAsync(message, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeadLetter_DelegatesToReceiverDeadLetterMessageAsync()
    {
        // Arrange
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("body"),
            messageId: "msg-1");
        var sut = CreateSut(message);

        // Act
        await sut.DeadLetter(CancellationToken.None);

        // Assert
        _receiver.Verify(
            x => x.DeadLetterMessageAsync(message, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Resend_CallsSenderSendMessageAsync()
    {
        // Arrange
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("resend-body"),
            messageId: "msg-resend");
        var sut = CreateSut(message);

        // Act
        await sut.Resend(CancellationToken.None);

        // Assert
        _sender.Verify(
            x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Resend_CopiesBodyAndPropertiesFromOriginalMessage()
    {
        // Arrange
        var appProps = new Dictionary<string, object>
        {
            ["key1"] = "val1",
            ["key2"] = 42
        };
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("body"),
            messageId: "msg-1",
            correlationId: "corr-1",
            subject: "my-subject",
            to: "to-addr",
            replyTo: "reply-addr",
            replyToSessionId: "reply-sess",
            sessionId: "sess-1",
            partitionKey: "sess-1",
            contentType: "application/json",
            timeToLive: TimeSpan.FromHours(1),
            properties: appProps);
        var sut = CreateSut(message);

        ServiceBusMessage? captured = null;
        _sender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        // Act
        await sut.Resend(CancellationToken.None);

        // Assert
        captured!.Body.ToString().ShouldBe("body");
        captured.MessageId.ShouldBe("msg-1");
        captured.CorrelationId.ShouldBe("corr-1");
        captured.Subject.ShouldBe("my-subject");
        captured.To.ShouldBe("to-addr");
        captured.ReplyTo.ShouldBe("reply-addr");
        captured.ReplyToSessionId.ShouldBe("reply-sess");
        captured.SessionId.ShouldBe("sess-1");
        captured.PartitionKey.ShouldBe("sess-1");
        captured.ContentType.ShouldBe("application/json");
        captured.TimeToLive.ShouldBe(TimeSpan.FromHours(1));
        captured.ApplicationProperties["key1"].ShouldBe("val1");
        captured.ApplicationProperties["key2"].ShouldBe(42);
    }
}
