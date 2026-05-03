using Azure.Messaging.ServiceBus;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using SubQueue = Messentra.Features.Explorer.Messages.SubQueue;
using Messentra.Features.Mcp.GetDlqSummary;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using Moq;
using Shouldly;
using Xunit;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;
using ServiceBusMessage = Messentra.Features.Explorer.Messages.ServiceBusMessage;

namespace Messentra.UnitTests.Features.Mcp.GetDlqSummary;

public sealed class GetDlqSummaryQueryHandlerShould
{
    private readonly Mock<IAzureServiceBusQueueMessagesProvider> _queueProvider = new();
    private readonly Mock<IAzureServiceBusSubscriptionMessagesProvider> _subscriptionProvider = new();
    private readonly GetDlqSummaryQueryHandler _sut;

    public GetDlqSummaryQueryHandlerShould()
    {
        _sut = new GetDlqSummaryQueryHandler(_queueProvider.Object, _subscriptionProvider.Object);
    }

    [Fact]
    public async Task ReturnSampledCount_MatchingNumberOfPeekedMessages()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeMessage("MaxDeliveryCountExceeded"), MakeMessage("MaxDeliveryCountExceeded"), MakeMessage("Expired")]);

        var result = await _sut.Handle(MakeQuery("orders"), CancellationToken.None);

        result.AsT0.SampledCount.ShouldBe(3);
    }

    [Fact]
    public async Task GroupMessagesByReason()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                MakeMessage("MaxDeliveryCountExceeded"),
                MakeMessage("MaxDeliveryCountExceeded"),
                MakeMessage("Expired")
            ]);

        var result = await _sut.Handle(MakeQuery("orders"), CancellationToken.None);

        var groups = result.AsT0.Groups;
        groups.Count.ShouldBe(2);
        groups[0].GroupKey["deadLetterReason"].ShouldBe("MaxDeliveryCountExceeded");
        groups[0].Count.ShouldBe(2);
        groups[1].GroupKey["deadLetterReason"].ShouldBe("Expired");
        groups[1].Count.ShouldBe(1);
    }

    [Fact]
    public async Task OrderGroupsByCountDescending()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                MakeMessage("Rare"),
                MakeMessage("Common"), MakeMessage("Common"), MakeMessage("Common"),
                MakeMessage("SomewhatCommon"), MakeMessage("SomewhatCommon")
            ]);

        var result = await _sut.Handle(MakeQuery("orders"), CancellationToken.None);

        var groups = result.AsT0.Groups;
        groups[0].GroupKey["deadLetterReason"].ShouldBe("Common");
        groups[1].GroupKey["deadLetterReason"].ShouldBe("SomewhatCommon");
        groups[2].GroupKey["deadLetterReason"].ShouldBe("Rare");
    }

    [Fact]
    public async Task GroupByBothReasonAndErrorDescription()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                MakeMessage("MaxDeliveryCountExceeded", "Timeout"),
                MakeMessage("MaxDeliveryCountExceeded", "NullRef"),
                MakeMessage("MaxDeliveryCountExceeded", "Timeout")
            ]);

        var result = await _sut.Handle(MakeQuery("orders"), CancellationToken.None);

        result.AsT0.Groups.Count.ShouldBe(2);
    }

    [Fact]
    public async Task IncludeSampleBody_FromFirstMessageInGroup()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                MakeMessage("MaxDeliveryCountExceeded", body: "first-body"),
                MakeMessage("MaxDeliveryCountExceeded", body: "second-body")
            ]);

        var result = await _sut.Handle(MakeQuery("orders"), CancellationToken.None);

        result.AsT0.Groups.Single().SampleBody.ShouldBe("first-body");
    }

    [Fact]
    public async Task PeekFromSubscription_WhenTopicNameIsProvided()
    {
        _subscriptionProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), "events", "sub1", It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeMessage("MaxDeliveryCountExceeded")]);

        var result = await _sut.Handle(MakeQuery("sub1", topicName: "events"), CancellationToken.None);

        result.AsT0.SampledCount.ShouldBe(1);
        _queueProvider.Verify(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PassDeadLetterSubqueueAndSampleSize_ToProvider()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.Handle(MakeQuery("orders", sampleSize: 300), CancellationToken.None);

        _queueProvider.Verify(p => p.Get(
            It.IsAny<ConnectionInfo>(),
            "orders",
            It.Is<FetchMessagesOptions>(o =>
                o.Mode == FetchMode.Peek &&
                o.SubQueue == SubQueue.DeadLetter &&
                o.MessageCount == 300),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnEmptySummary_WhenNoDlqMessages()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(MakeQuery("orders"), CancellationToken.None);

        var summary = result.AsT0;
        summary.SampledCount.ShouldBe(0);
        summary.Groups.ShouldBeEmpty();
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
    public async Task SetNextSequenceNumber_WhenFullBatchReturned()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeMessage(sequenceNumber: 10), MakeMessage(sequenceNumber: 20)]);

        var result = await _sut.Handle(MakeQuery("orders", sampleSize: 2), CancellationToken.None);

        result.AsT0.NextSequenceNumber.ShouldBe(21L);
    }

    [Fact]
    public async Task SetNextSequenceNumberToNull_WhenPartialBatchReturned()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeMessage(sequenceNumber: 5)]);

        var result = await _sut.Handle(MakeQuery("orders", sampleSize: 10), CancellationToken.None);

        result.AsT0.NextSequenceNumber.ShouldBeNull();
    }

    [Fact]
    public async Task PassFromSequenceNumber_ToProvider()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.Handle(MakeQuery("orders", fromSequenceNumber: 100L), CancellationToken.None);

        _queueProvider.Verify(p => p.Get(
            It.IsAny<ConnectionInfo>(),
            It.IsAny<string>(),
            It.Is<FetchMessagesOptions>(o => o.StartSequence == 100L),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GroupByApplicationProperty_WhenGroupByIsSpecified()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                MakeMessage(appProperties: new Dictionary<string, object> { ["PayloadTypeId"] = "OrderPlaced" }),
                MakeMessage(appProperties: new Dictionary<string, object> { ["PayloadTypeId"] = "OrderPlaced" }),
                MakeMessage(appProperties: new Dictionary<string, object> { ["PayloadTypeId"] = "OrderCancelled" })
            ]);

        var result = await _sut.Handle(MakeQuery("orders", groupBy: ["PayloadTypeId"]), CancellationToken.None);

        var groups = result.AsT0.Groups;
        groups.Count.ShouldBe(2);
        groups[0].GroupKey["PayloadTypeId"].ShouldBe("OrderPlaced");
        groups[0].Count.ShouldBe(2);
        groups[1].GroupKey["PayloadTypeId"].ShouldBe("OrderCancelled");
        groups[1].Count.ShouldBe(1);
    }

    [Fact]
    public async Task GroupKeyContainsAllRequestedFields_WhenGroupByHasMultipleFields()
    {
        _queueProvider.Setup(p => p.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                MakeMessage(deadLetterReason: "MaxDeliveryCountExceeded", appProperties: new Dictionary<string, object> { ["PayloadTypeId"] = "OrderPlaced" }),
                MakeMessage(deadLetterReason: "MaxDeliveryCountExceeded", appProperties: new Dictionary<string, object> { ["PayloadTypeId"] = "OrderCancelled" })
            ]);

        var result = await _sut.Handle(MakeQuery("orders", groupBy: ["deadLetterReason", "PayloadTypeId"]), CancellationToken.None);

        var groups = result.AsT0.Groups;
        groups.Count.ShouldBe(2);
        groups[0].GroupKey.Keys.ShouldBe(["deadLetterReason", "PayloadTypeId"], ignoreOrder: true);
    }

    private static GetDlqSummaryQuery MakeQuery(
        string resourceName,
        string? topicName = null,
        int sampleSize = 200,
        long? fromSequenceNumber = null,
        string[]? groupBy = null)
    {
        var config = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test");
        return new GetDlqSummaryQuery(config, resourceName, topicName, sampleSize, fromSequenceNumber, groupBy);
    }

    private static ServiceBusMessage MakeMessage(
        string? deadLetterReason = null,
        string? deadLetterErrorDescription = null,
        string body = "{}",
        long sequenceNumber = 1,
        Dictionary<string, object>? appProperties = null)
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
                    DeliveryCount: 3,
                    Label: null,
                    To: null,
                    ReplyTo: null,
                    PartitionKey: null,
                    ContentType: "application/json",
                    DeadLetterReason: deadLetterReason,
                    DeadLetterErrorDescription: deadLetterErrorDescription),
                ApplicationProperties: appProperties ?? new Dictionary<string, object>()),
            new Mock<IServiceBusMessageContext>().Object);
}
