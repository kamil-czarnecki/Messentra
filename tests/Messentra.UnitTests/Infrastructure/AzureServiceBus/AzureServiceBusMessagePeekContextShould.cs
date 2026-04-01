using Azure.Messaging.ServiceBus;
using Messentra.Infrastructure.AzureServiceBus;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus;

public sealed class AzureServiceBusMessagePeekContextShould
{
    private readonly Mock<ServiceBusSender> _sender = new();

    private AzureServiceBusMessagePeekContext CreateSut(ServiceBusReceivedMessage message) =>
        new(message, _sender.Object);

    [Fact]
    public async Task Complete_ThrowsNotSupported()
    {
        // Arrange
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("body"));
        var sut = CreateSut(message);

        // Act
        var action = () => sut.Complete(CancellationToken.None);

        // Assert
        await action.ShouldThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task Abandon_ThrowsNotSupported()
    {
        // Arrange
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("body"));
        var sut = CreateSut(message);

        // Act
        var action = () => sut.Abandon(CancellationToken.None);

        // Assert
        await action.ShouldThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task DeadLetter_ThrowsNotSupported()
    {
        // Arrange
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("body"));
        var sut = CreateSut(message);

        // Act
        var action = () => sut.DeadLetter(CancellationToken.None);

        // Assert
        await action.ShouldThrowAsync<NotSupportedException>();
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

