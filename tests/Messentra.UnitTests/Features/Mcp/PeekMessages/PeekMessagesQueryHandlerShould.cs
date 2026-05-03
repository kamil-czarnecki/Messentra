using Azure.Messaging.ServiceBus;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using SubQueue = Messentra.Features.Explorer.Messages.SubQueue;
using Messentra.Features.Mcp;
using Messentra.Features.Mcp.PeekMessages;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using Moq;
using Shouldly;
using Xunit;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;
using ServiceBusMessage = Messentra.Features.Explorer.Messages.ServiceBusMessage;

namespace Messentra.UnitTests.Features.Mcp.PeekMessages;

public sealed class PeekMessagesQueryHandlerShould
{
    private readonly Mock<IAzureServiceBusQueueMessagesProvider> _queueProvider = new();
    private readonly Mock<IAzureServiceBusSubscriptionMessagesProvider> _subscriptionProvider = new();
    private readonly PeekMessagesQueryHandler _sut;

    public PeekMessagesQueryHandlerShould()
    {
        _sut = new PeekMessagesQueryHandler(_queueProvider.Object, _subscriptionProvider.Object);
    }

    [Fact]
    public async Task PeekQueueMessages_WhenTopicNameIsNull()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), "orders", It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeMessage(sequenceNumber: 1)]);

        var result = await _sut.Handle(MakeQuery("orders"), CancellationToken.None);

        var success = result.AsT0;
        success.Messages.ShouldHaveSingleItem().SequenceNumber.ShouldBe(1L);
        _subscriptionProvider.Verify(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PeekSubscriptionMessages_WhenTopicNameIsProvided()
    {
        _subscriptionProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), "events", "sub1", It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeMessage(sequenceNumber: 5)]);

        var result = await _sut.Handle(MakeQuery("sub1", topicName: "events"), CancellationToken.None);

        var success = result.AsT0;
        success.Messages.ShouldHaveSingleItem().SequenceNumber.ShouldBe(5L);
        _queueProvider.Verify(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetNextSequenceNumber_WhenFullBatchReturned()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeMessage(sequenceNumber: 10), MakeMessage(sequenceNumber: 20)]);

        var result = await _sut.Handle(MakeQuery("orders", maxMessages: 2), CancellationToken.None);

        result.AsT0.NextSequenceNumber.ShouldBe(21L);
    }

    [Fact]
    public async Task SetNextSequenceNumberToNull_WhenPartialBatchReturned()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeMessage(sequenceNumber: 5)]);

        var result = await _sut.Handle(MakeQuery("orders", maxMessages: 10), CancellationToken.None);

        result.AsT0.NextSequenceNumber.ShouldBeNull();
    }

    [Fact]
    public async Task SetNextSequenceNumberToNull_WhenNoMessages()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(MakeQuery("orders"), CancellationToken.None);

        result.AsT0.NextSequenceNumber.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnFullBody()
    {
        var body = new string('x', 10_000);
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeMessage(body: body)]);

        var result = await _sut.Handle(MakeQuery("orders"), CancellationToken.None);

        result.AsT0.Messages.Single().Body.ShouldBe(body);
    }

    [Fact]
    public async Task PassPeekModeAndSubqueue_ToProvider()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.Handle(MakeQuery("orders", subqueue: SubQueue.DeadLetter, maxMessages: 50, fromSeq: 100L), CancellationToken.None);

        _queueProvider.Verify(p => p.Get(
            It.IsAny<ConnectionInfo>(),
            "orders",
            It.Is<FetchMessagesOptions>(o =>
                o.Mode == FetchMode.Peek &&
                o.SubQueue == SubQueue.DeadLetter &&
                o.MessageCount == 50 &&
                o.StartSequence == 100L),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnMcpError_WhenResourceNotFound()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), "missing", It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("Entity not found", ServiceBusFailureReason.MessagingEntityNotFound));

        var result = await _sut.Handle(MakeQuery("missing"), CancellationToken.None);

        result.AsT1.Message.ShouldContain("missing");
    }

    [Fact]
    public async Task MapApplicationProperties_ToStringValues()
    {
        var props = new Dictionary<string, object> { ["key"] = 42, ["flag"] = true };
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeMessage(applicationProperties: props)]);

        var result = await _sut.Handle(MakeQuery("orders"), CancellationToken.None);

        var appProps = result.AsT0.Messages.Single().ApplicationProperties;
        appProps["key"].ShouldBe("42");
        appProps["flag"].ShouldBe("True");
    }

    private static PeekMessagesQuery MakeQuery(
        string resourceName,
        string? topicName = null,
        SubQueue subqueue = SubQueue.Active,
        int maxMessages = 20,
        long? fromSeq = null)
    {
        var config = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test");
        return new PeekMessagesQuery(1L, config, resourceName, topicName, subqueue, maxMessages, fromSeq);
    }

    private static ServiceBusMessage MakeMessage(
        long sequenceNumber = 1,
        string body = "{}",
        Dictionary<string, object>? applicationProperties = null)
        => new(
            new MessageDto(
                Body: body,
                BrokerProperties: new BrokerProperties(
                    MessageId: Guid.NewGuid().ToString(),
                    SequenceNumber: sequenceNumber,
                    CorrelationId: null,
                    SessionId: null,
                    ReplyToSessionId: null,
                    EnqueuedTimeUtc: DateTime.UtcNow,
                    ScheduledEnqueueTimeUtc: default,
                    TimeToLive: TimeSpan.FromDays(1),
                    LockedUntilUtc: DateTime.UtcNow.AddMinutes(1),
                    ExpiresAtUtc: DateTime.UtcNow.AddDays(1),
                    DeliveryCount: 1,
                    Label: null,
                    To: null,
                    ReplyTo: null,
                    PartitionKey: null,
                    ContentType: "application/json",
                    DeadLetterReason: null,
                    DeadLetterErrorDescription: null),
                ApplicationProperties: applicationProperties ?? new Dictionary<string, object>()),
            new Mock<IServiceBusMessageContext>().Object);
}
