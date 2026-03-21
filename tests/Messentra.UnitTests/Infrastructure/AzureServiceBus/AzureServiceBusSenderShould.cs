using Azure.Messaging.ServiceBus;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus;

public sealed class AzureServiceBusSenderShould
{
    private const string ConnectionString =
        "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey=";
    private const string EntityPath = "my-queue";

    private readonly Mock<IAzureServiceBusClientFactory> _clientFactory = new();
    private readonly Mock<ServiceBusClient> _client = new();
    private readonly Mock<ServiceBusSender> _sender = new();
    private readonly AzureServiceBusSender _sut;

    public AzureServiceBusSenderShould()
    {
        _clientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_client.Object);

        _client
            .Setup(x => x.CreateSender(It.IsAny<string>()))
            .Returns(_sender.Object);

        _sut = new AzureServiceBusSender(_clientFactory.Object);
    }

    [Fact]
    public async Task Send_MinimalCommand_CallsSendMessageAsyncOnce()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var command = BuildCommand(body: "hello");

        // Act
        await _sut.Send(info, EntityPath, command, CancellationToken.None);

        // Assert
        _sender.Verify(
            x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Send_MinimalCommand_CreatesSenderWithCorrectEntityPath()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var command = BuildCommand(body: "hello");

        // Act
        await _sut.Send(info, "specific-entity", command, CancellationToken.None);

        // Assert
        _client.Verify(x => x.CreateSender("specific-entity"), Times.Once);
    }

    [Fact]
    public async Task Send_MinimalCommand_SetsBodyOnly()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var command = BuildCommand(body: "hello");
        ServiceBusMessage? captured = null;

        _sender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Send(info, EntityPath, command, CancellationToken.None);

        // Assert
        captured.ShouldNotBeNull();
        captured.Body.ToString().ShouldBe("hello");
        captured.MessageId.ShouldBeNull();
        captured.Subject.ShouldBeNull();
        captured.CorrelationId.ShouldBeNull();
        captured.SessionId.ShouldBeNull();
        captured.ContentType.ShouldBeNull();
        captured.ApplicationProperties.ShouldBeEmpty();
    }

    [Fact]
    public async Task Send_AllFieldsPopulated_MapsEveryPropertyOntoMessage()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var scheduledTime = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var command = BuildCommand(
            body: "full-body",
            messageId: "msg-1",
            label: "my-label",
            correlationId: "corr-1",
            sessionId: "sess-1",
            replyToSessionId: "reply-sess-1",
            partitionKey: "sess-1",   // SDK requires PartitionKey == SessionId when both are set
            scheduledEnqueueTimeUtc: scheduledTime,
            timeToLive: TimeSpan.FromHours(2),
            to: "to-address",
            replyTo: "reply-address",
            contentType: "application/json",
            applicationProperties: new Dictionary<string, string> { ["key1"] = "val1", ["key2"] = "val2" });

        ServiceBusMessage? captured = null;
        _sender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Send(info, EntityPath, command, CancellationToken.None);

        // Assert
        captured.ShouldNotBeNull();
        captured.Body.ToString().ShouldBe("full-body");
        captured.MessageId.ShouldBe("msg-1");
        captured.Subject.ShouldBe("my-label");
        captured.CorrelationId.ShouldBe("corr-1");
        captured.SessionId.ShouldBe("sess-1");
        captured.ReplyToSessionId.ShouldBe("reply-sess-1");
        captured.PartitionKey.ShouldBe("sess-1");
        captured.ScheduledEnqueueTime.ShouldBe(new DateTimeOffset(scheduledTime, TimeSpan.Zero));
        captured.TimeToLive.ShouldBe(TimeSpan.FromHours(2));
        captured.To.ShouldBe("to-address");
        captured.ReplyTo.ShouldBe("reply-address");
        captured.ContentType.ShouldBe("application/json");
        captured.ApplicationProperties["key1"].ShouldBe("val1");
        captured.ApplicationProperties["key2"].ShouldBe("val2");
    }

    [Fact]
    public async Task Send_ScheduledEnqueueTimeUtc_ConvertsToUtcDateTimeOffset()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var utcTime = new DateTime(2026, 3, 15, 8, 30, 0, DateTimeKind.Utc);
        var command = BuildCommand(body: "body", scheduledEnqueueTimeUtc: utcTime);

        ServiceBusMessage? captured = null;
        _sender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Send(info, EntityPath, command, CancellationToken.None);

        // Assert
        captured.ShouldNotBeNull();
        captured.ScheduledEnqueueTime.Offset.ShouldBe(TimeSpan.Zero);
        captured.ScheduledEnqueueTime.UtcDateTime.ShouldBe(utcTime);
    }

    [Fact]
    public async Task Send_ApplicationProperties_ForwardsAllKeyValuePairs()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var props = new Dictionary<string, string>
        {
            ["prop-a"] = "value-a",
            ["prop-b"] = "value-b",
            ["prop-c"] = "value-c"
        };
        var command = BuildCommand(body: "body", applicationProperties: props);

        ServiceBusMessage? captured = null;
        _sender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Send(info, EntityPath, command, CancellationToken.None);

        // Assert
        captured.ShouldNotBeNull();
        captured.ApplicationProperties.Count.ShouldBe(3);
        captured.ApplicationProperties["prop-a"].ShouldBe("value-a");
        captured.ApplicationProperties["prop-b"].ShouldBe("value-b");
        captured.ApplicationProperties["prop-c"].ShouldBe("value-c");
    }

    [Fact]
    public async Task Send_ManagedIdentityConnectionInfo_UsesNamespaceAndCredentials()
    {
        // Arrange
        var info = new ConnectionInfo.ManagedIdentity(
            "test.servicebus.windows.net",
            "tenant-id",
            "client-id");

        _clientFactory
            .Setup(x => x.CreateClient(
                "test.servicebus.windows.net",
                "tenant-id",
                "client-id",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_client.Object);

        var command = BuildCommand(body: "hello");

        // Act
        await _sut.Send(info, EntityPath, command, CancellationToken.None);

        // Assert
        _clientFactory.Verify(
            x => x.CreateClient(
                "test.servicebus.windows.net",
                "tenant-id",
                "client-id",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static SendMessageCommand BuildCommand(
        string body = "body",
        string? messageId = null,
        string? label = null,
        string? correlationId = null,
        string? sessionId = null,
        string? replyToSessionId = null,
        string? partitionKey = null,
        DateTime? scheduledEnqueueTimeUtc = null,
        TimeSpan? timeToLive = null,
        string? to = null,
        string? replyTo = null,
        string? contentType = null,
        IReadOnlyDictionary<string, string>? applicationProperties = null) =>
        new(
            ResourceTreeNode: null!,
            Body: body,
            MessageId: messageId,
            Label: label,
            CorrelationId: correlationId,
            SessionId: sessionId,
            ReplyToSessionId: replyToSessionId,
            PartitionKey: partitionKey,
            ScheduledEnqueueTimeUtc: scheduledEnqueueTimeUtc,
            TimeToLive: timeToLive,
            To: to,
            ReplyTo: replyTo,
            ContentType: contentType,
            ApplicationProperties: applicationProperties ?? new Dictionary<string, string>());
}

