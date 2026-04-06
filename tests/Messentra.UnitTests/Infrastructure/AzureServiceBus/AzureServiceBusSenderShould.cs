using Azure.Messaging.ServiceBus;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Features.Jobs.Stages;
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
            applicationProperties: new Dictionary<string, object> { ["key1"] = "val1", ["key2"] = "val2" });

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
        var props = new Dictionary<string, object>
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
    public async Task Send_ApplicationProperties_ForwardsTypedValues()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var when = new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc);
        var props = new Dictionary<string, object>
        {
            ["num"] = 3.5m,
            ["flag"] = true,
            ["when"] = when
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
        captured.ApplicationProperties["num"].ShouldBeOfType<decimal>().ShouldBe(3.5m);
        captured.ApplicationProperties["flag"].ShouldBeOfType<bool>().ShouldBeTrue();
        captured.ApplicationProperties["when"].ShouldBeOfType<DateTime>().ShouldBe(when);
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

    [Fact]
    public async Task Send_WhenSendFailsWithNonTransientError_DoesNotInvalidateClientAndDoesNotRetry()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var command = BuildCommand(body: "hello");

        _sender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("non transient send failure"));

        // Act
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.Send(info, EntityPath, command, CancellationToken.None));

        // Assert
        _clientFactory.Verify(x => x.CreateClient(ConnectionString, It.IsAny<CancellationToken>()), Times.Once);
        _clientFactory.Verify(x => x.InvalidateClient(ConnectionString), Times.Never);
        _sender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_WhenSendFailsWithTransientError_InvalidatesClientAndDoesNotRetry()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var command = BuildCommand(body: "hello");

        _sender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("transient connection issue"));

        _clientFactory
            .Setup(x => x.InvalidateClient(ConnectionString))
            .Returns(Task.CompletedTask);

        // Act
        await Should.ThrowAsync<HttpRequestException>(() => _sut.Send(info, EntityPath, command, CancellationToken.None));

        // Assert
        _sender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _clientFactory.Verify(x => x.InvalidateClient(ConnectionString), Times.Once);
    }

    [Fact]
    public async Task Send_WhenSendFailsWithUnauthorizedAccess_InvalidatesClientAndDoesNotRetry()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var command = BuildCommand(body: "hello");

        _sender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("ip blocked"));

        _clientFactory
            .Setup(x => x.InvalidateClient(ConnectionString))
            .Returns(Task.CompletedTask);

        // Act
        await Should.ThrowAsync<UnauthorizedAccessException>(() => _sut.Send(info, EntityPath, command, CancellationToken.None));

        // Assert
        _sender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _clientFactory.Verify(x => x.InvalidateClient(ConnectionString), Times.Once);
    }

    [Fact]
    public async Task Send_ImportedMessage_MapsBodyAndCoreProperties()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);
        var dto = new ServiceBusMessageDto(
            Message: new { key = "value" },
            Properties: new ServiceBusProperties(
                ContentType: "application/json",
                CorrelationId: "corr-1",
                Subject: "subject-1",
                MessageId: "msg-1",
                To: "to-1",
                ReplyTo: "reply-1",
                TimeToLive: TimeSpan.FromMinutes(2),
                ReplyToSessionId: null,
                SessionId: null,
                PartitionKey: null,
                ScheduledEnqueueTime: null,
                TransactionPartitionKey: null,
                EnqueuedTimeUtc: null),
            ApplicationProperties: new Dictionary<string, object>
            {
                ["tenant"] = "alpha"
            });

        ServiceBusMessage? captured = null;
        _sender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Send(info, EntityPath, dto, CancellationToken.None);

        // Assert
        captured.ShouldNotBeNull();
        captured.Body.ToString().ShouldContain("\"key\"");
        captured.Body.ToString().ShouldContain("\"value\"");
        captured.ContentType.ShouldBe("application/json");
        captured.CorrelationId.ShouldBe("corr-1");
        captured.Subject.ShouldBe("subject-1");
        captured.MessageId.ShouldBe("msg-1");
        captured.ApplicationProperties["tenant"].ShouldBe("alpha");
    }

    [Fact]
    public async Task SendBatchChunk_ReturnsZeroAndSkipsClient_WhenNoMessagesProvided()
    {
        // Arrange
        var info = new ConnectionInfo.ConnectionString(ConnectionString);

        // Act
        var result = await _sut.SendBatchChunk(info, EntityPath, (IReadOnlyList<ServiceBusMessageDto>)[], CancellationToken.None);

        // Assert
        result.ShouldBe(0);
        _client.Verify(x => x.CreateSender(It.IsAny<string>()), Times.Never);
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
        IReadOnlyDictionary<string, object>? applicationProperties = null) =>
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
            ApplicationProperties: applicationProperties ?? new Dictionary<string, object>());
}

