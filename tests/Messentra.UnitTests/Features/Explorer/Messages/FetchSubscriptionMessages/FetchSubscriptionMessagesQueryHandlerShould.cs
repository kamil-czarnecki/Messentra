using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Messages.FetchSubscriptionMessages;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using Moq;
using Shouldly;
using Xunit;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;
using ServiceBusMessage = Messentra.Features.Explorer.Messages.ServiceBusMessage;

namespace Messentra.UnitTests.Features.Explorer.Messages.FetchSubscriptionMessages;

public sealed class FetchSubscriptionMessagesQueryHandlerShould
{
    private readonly Mock<IAzureServiceBusSubscriptionMessagesProvider> _providerMock = new();
    private readonly FetchSubscriptionMessagesQueryHandler _sut;

    private static readonly FetchMessagesOptions DefaultOptions = new(
        FetchMode.Peek,
        FetchReceiveMode.PeekLock,
        MessageCount: 10,
        StartSequence: null,
        WaitTime: TimeSpan.FromSeconds(5));

    public FetchSubscriptionMessagesQueryHandlerShould()
    {
        _sut = new FetchSubscriptionMessagesQueryHandler(_providerMock.Object);
    }

    [Fact]
    public async Task ReturnMessages_WithConnectionString()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new FetchSubscriptionMessagesQuery("my-topic", "my-subscription", connectionConfig, DefaultOptions);
        var expectedMessages = new[] { CreateMessage(), CreateMessage() };

        _providerMock
            .Setup(x => x.Get(
                It.Is<ConnectionInfo.ConnectionString>(c => c.Value == connectionConfig.ConnectionStringConfig!.ConnectionString),
                query.TopicName,
                query.SubscriptionName,
                query.Options,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMessages);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedMessages);
    }

    [Fact]
    public async Task ReturnMessages_WithEntraId()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant-id", "client-id");
        var query = new FetchSubscriptionMessagesQuery("my-topic", "my-subscription", connectionConfig, DefaultOptions);
        var expectedMessages = new[] { CreateMessage() };

        _providerMock
            .Setup(x => x.Get(
                It.Is<ConnectionInfo.ManagedIdentity>(c =>
                    c.FullyQualifiedNamespace == connectionConfig.EntraIdConfig!.Namespace &&
                    c.TenantId == connectionConfig.EntraIdConfig.TenantId &&
                    c.ClientId == connectionConfig.EntraIdConfig.ClientId),
                query.TopicName,
                query.SubscriptionName,
                query.Options,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMessages);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedMessages);
    }

    [Fact]
    public async Task ReturnEmptyCollection_WhenNoMessagesExist()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new FetchSubscriptionMessagesQuery("my-topic", "my-subscription", connectionConfig, DefaultOptions);

        _providerMock
            .Setup(x => x.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task PassTopicAndSubscriptionNameToProvider()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new FetchSubscriptionMessagesQuery("specific-topic", "specific-subscription", connectionConfig, DefaultOptions);

        _providerMock
            .Setup(x => x.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        _providerMock.Verify(x => x.Get(
            It.IsAny<ConnectionInfo>(),
            "specific-topic",
            "specific-subscription",
            It.IsAny<FetchMessagesOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PassOptionsToProvider()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var customOptions = new FetchMessagesOptions(FetchMode.Receive, FetchReceiveMode.ReceiveAndDelete, 50, 100L, TimeSpan.FromSeconds(30), SubQueue.DeadLetter);
        var query = new FetchSubscriptionMessagesQuery("my-topic", "my-subscription", connectionConfig, customOptions);

        _providerMock
            .Setup(x => x.Get(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FetchMessagesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        _providerMock.Verify(x => x.Get(
            It.IsAny<ConnectionInfo>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            customOptions,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ThrowInvalidOperationException_ForUnsupportedConnectionType()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig((ConnectionType)999, null, null);
        var query = new FetchSubscriptionMessagesQuery("my-topic", "my-subscription", connectionConfig, DefaultOptions);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.Handle(query, CancellationToken.None).AsTask());
    }

    private static ServiceBusMessage CreateMessage() =>
        new(
            new MessageDto(
                Body: "{}",
                BrokerProperties: new BrokerProperties(
                    MessageId: Guid.NewGuid().ToString(),
                    SequenceNumber: 1,
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
                ApplicationProperties: new Dictionary<string, object>()),
            new Mock<IServiceBusMessageContext>().Object);
}

