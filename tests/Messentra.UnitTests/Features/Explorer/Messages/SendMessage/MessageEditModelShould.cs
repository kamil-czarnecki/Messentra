using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Features.Explorer.Resources;
using Messentra.Infrastructure.AzureServiceBus;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Messages.SendMessage;

public sealed class MessageEditModelShould
{
    [Fact]
    public void ClearMessageId_WhenCreatedFromDto()
    {
        var dto = BuildDto(messageId: "original-id");
        var model = MessageEditModel.FromMessageDto(dto);
        model.MessageId.ShouldBeNull();
    }

    [Fact]
    public void NotCopyScheduledEnqueueTimeUtc_WhenCreatedFromDto()
    {
        var dto = BuildDto();
        var model = MessageEditModel.FromMessageDto(dto);
        model.ScheduledDate.ShouldBeNull();
    }

    [Fact]
    public void CopyBody_WhenCreatedFromDto()
    {
        var dto = BuildDto(body: "{\"key\":\"value\"}");
        var model = MessageEditModel.FromMessageDto(dto);
        model.Body.ShouldBe("{\"key\":\"value\"}");
    }

    [Fact]
    public void CopyBrokerFields_WhenCreatedFromDto()
    {
        var dto = BuildDto(
            label: "my-label",
            correlationId: "corr-1",
            sessionId: "sess-1",
            replyToSessionId: "rts-1",
            partitionKey: "pk-1",
            to: "to-addr",
            replyTo: "reply-addr",
            contentType: "application/json");

        var model = MessageEditModel.FromMessageDto(dto);

        model.Label.ShouldBe("my-label");
        model.CorrelationId.ShouldBe("corr-1");
        model.SessionId.ShouldBe("sess-1");
        model.ReplyToSessionId.ShouldBe("rts-1");
        model.PartitionKey.ShouldBe("pk-1");
        model.To.ShouldBe("to-addr");
        model.ReplyTo.ShouldBe("reply-addr");
        model.ContentType.ShouldBe("application/json");
    }

    [Fact]
    public void SetTimeToLiveText_WhenNotMaxValue()
    {
        var dto = BuildDto(timeToLive: TimeSpan.FromMinutes(10));
        var model = MessageEditModel.FromMessageDto(dto);
        model.TimeToLiveText.ShouldBe(TimeSpan.FromMinutes(10).ToString());
    }

    [Fact]
    public void LeaveTimeToLiveTextNull_WhenMaxValue()
    {
        var dto = BuildDto(timeToLive: TimeSpan.MaxValue);
        var model = MessageEditModel.FromMessageDto(dto);
        model.TimeToLiveText.ShouldBeNull();
    }

    [Fact]
    public void MapStringApplicationProperty_WhenCreatedFromDto()
    {
        var props = new Dictionary<string, object> { ["key1"] = "value1" };
        var dto = BuildDto(applicationProperties: props);

        var model = MessageEditModel.FromMessageDto(dto);

        model.CustomProperties.Count.ShouldBe(1);
        model.CustomProperties[0].Key.ShouldBe("key1");
        model.CustomProperties[0].Type.ShouldBe(CustomPropertyType.String);
        model.CustomProperties[0].Value.ShouldBe("value1");
    }

    [Fact]
    public void MapNumericApplicationProperty_WhenCreatedFromDto()
    {
        var props = new Dictionary<string, object> { ["count"] = 42.0 };
        var dto = BuildDto(applicationProperties: props);

        var model = MessageEditModel.FromMessageDto(dto);

        model.CustomProperties[0].Type.ShouldBe(CustomPropertyType.Number);
        model.CustomProperties[0].NumberValue.ShouldBe(42m);
    }

    [Fact]
    public void MapBooleanApplicationProperty_WhenCreatedFromDto()
    {
        var props = new Dictionary<string, object> { ["flag"] = true };
        var dto = BuildDto(applicationProperties: props);

        var model = MessageEditModel.FromMessageDto(dto);

        model.CustomProperties[0].Type.ShouldBe(CustomPropertyType.Boolean);
        model.CustomProperties[0].BooleanValue.ShouldBeTrue();
    }

    [Fact]
    public void NormaliseWhitespaceOnlyFieldsToNull_WhenBuildingCommand()
    {
        var model = new MessageEditModel
        {
            Body = "{}",
            Label = "   ",
            CorrelationId = " ",
            MessageId = "\t"
        };
        var node = BuildQueueNode();

        var command = model.ToSendMessageCommand(node);

        command.Label.ShouldBeNull();
        command.CorrelationId.ShouldBeNull();
        command.MessageId.ShouldBeNull();
    }

    [Fact]
    public void ReturnNullTimeToLive_WhenTimeToLiveTextIsInvalid()
    {
        var model = new MessageEditModel { Body = "{}", TimeToLiveText = "not-a-timespan" };
        var node = BuildQueueNode();

        var command = model.ToSendMessageCommand(node);

        command.TimeToLive.ShouldBeNull();
    }

    [Fact]
    public void ParseTimeToLive_WhenTimeToLiveTextIsValid()
    {
        var model = new MessageEditModel { Body = "{}", TimeToLiveText = "00:10:00" };
        var node = BuildQueueNode();

        var command = model.ToSendMessageCommand(node);

        command.TimeToLive.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void ExcludeBlankKeyedCustomProperties_WhenBuildingCommand()
    {
        var model = new MessageEditModel { Body = "{}" };
        model.CustomProperties.Add(new CustomProperty { Key = "valid", Value = "v" });
        model.CustomProperties.Add(new CustomProperty { Key = "   ", Value = "ignored" });
        model.CustomProperties.Add(new CustomProperty { Key = "", Value = "also-ignored" });
        var node = BuildQueueNode();

        var command = model.ToSendMessageCommand(node);

        command.ApplicationProperties.Count.ShouldBe(1);
        command.ApplicationProperties.Keys.ShouldContain("valid");
    }

    private static QueueTreeNode BuildQueueNode()
    {
        var overview = new ResourceOverview(
            "Active",
            DateTimeOffset.Now,
            DateTimeOffset.Now,
            new MessageInfo(0, 0, 0, 0, 0, 0),
            new SizeInfo(0, 1024));
        var props = new QueueProperties(
            TimeSpan.FromDays(14),
            TimeSpan.FromMinutes(5),
            TimeSpan.MaxValue,
            10,
            false,
            null,
            null,
            false,
            false,
            TimeSpan.Zero,
            false,
            null,
            "");
        var queue = new Resource.Queue("my-queue", "sb://test/queues/my-queue", overview, props);
        var connectionConfig = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant", "client");
        return new QueueTreeNode("TestNS", queue, connectionConfig);
    }

    private static MessageDto BuildDto(
        string body = "{}",
        string? messageId = "msg-1",
        string? label = null,
        string? correlationId = null,
        string? sessionId = null,
        string? replyToSessionId = null,
        string? partitionKey = null,
        string? to = null,
        string? replyTo = null,
        string? contentType = null,
        TimeSpan? timeToLive = null,
        IReadOnlyDictionary<string, object>? applicationProperties = null)
    {
        var brokerProperties = new BrokerProperties(
            MessageId: messageId,
            SequenceNumber: 1,
            CorrelationId: correlationId,
            SessionId: sessionId,
            ReplyToSessionId: replyToSessionId,
            EnqueuedTimeUtc: DateTime.UtcNow,
            ScheduledEnqueueTimeUtc: DateTime.UtcNow,
            TimeToLive: timeToLive ?? TimeSpan.MaxValue,
            LockedUntilUtc: DateTime.UtcNow.AddMinutes(5),
            ExpiresAtUtc: DateTime.UtcNow.AddDays(14),
            DeliveryCount: 1,
            Label: label,
            To: to,
            ReplyTo: replyTo,
            PartitionKey: partitionKey,
            ContentType: contentType,
            DeadLetterReason: null,
            DeadLetterErrorDescription: null);

        return new MessageDto(body, brokerProperties, applicationProperties ?? new Dictionary<string, object>());
    }
}
