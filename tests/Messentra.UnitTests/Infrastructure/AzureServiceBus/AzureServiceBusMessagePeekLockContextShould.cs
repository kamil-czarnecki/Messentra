using Azure.Messaging.ServiceBus;
using Messentra.Infrastructure.AzureServiceBus;
using Moq;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus;

public sealed class AzureServiceBusMessagePeekLockContextShould
{
    private readonly Mock<ServiceBusReceiver> _receiver = new();

    private AzureServiceBusMessagePeekLockContext CreateSut(ServiceBusReceivedMessage message) =>
        new(_receiver.Object, message);

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
}
