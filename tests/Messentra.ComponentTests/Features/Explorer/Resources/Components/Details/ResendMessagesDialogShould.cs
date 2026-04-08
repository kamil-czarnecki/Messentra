using Bunit;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components.Details;
using Messentra.Infrastructure.AzureServiceBus;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components.Details;

public sealed class ResendMessagesDialogShould : ComponentTestBase
{
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

    private static ServiceBusMessage BuildMessage(
        long sequenceNumber = 1,
        string? messageId = "msg-1",
        string body = "{}")
    {
        var brokerProperties = new BrokerProperties(
            MessageId: messageId,
            SequenceNumber: sequenceNumber,
            CorrelationId: null,
            SessionId: null,
            ReplyToSessionId: null,
            EnqueuedTimeUtc: DateTime.UtcNow,
            ScheduledEnqueueTimeUtc: DateTime.UtcNow,
            TimeToLive: TimeSpan.MaxValue,
            LockedUntilUtc: DateTime.UtcNow.AddMinutes(5),
            ExpiresAtUtc: DateTime.UtcNow.AddDays(14),
            DeliveryCount: 1,
            Label: null,
            To: null,
            ReplyTo: null,
            PartitionKey: null,
            ContentType: null,
            DeadLetterReason: null,
            DeadLetterErrorDescription: null);
        var dto = new MessageDto(body, brokerProperties, new Dictionary<string, object>());
        return new ServiceBusMessage(dto, new Mock<IServiceBusMessageContext>().Object);
    }

    [Fact]
    public void RenderListEntryForEachMessage()
    {
        var messages = new List<ServiceBusMessage>
        {
            BuildMessage(sequenceNumber: 1, messageId: "msg-1"),
            BuildMessage(sequenceNumber: 2, messageId: "msg-2")
        };

        var cut = RenderDialog<ResendMessagesDialog>(p =>
        {
            p[nameof(ResendMessagesDialog.Messages)] = messages;
            p[nameof(ResendMessagesDialog.ResourceTreeNode)] = BuildQueueNode();
        });

        cut.Markup.ShouldContain("SEQ #1");
        cut.Markup.ShouldContain("SEQ #2");
        cut.Markup.ShouldContain("msg-1");
        cut.Markup.ShouldContain("msg-2");
    }

    [Fact]
    public async Task ReturnCancelledResult_WhenCancelClicked()
    {
        var messages = new List<ServiceBusMessage> { BuildMessage() };

        var cut = RenderDialog<ResendMessagesDialog>(p =>
        {
            p[nameof(ResendMessagesDialog.Messages)] = messages;
            p[nameof(ResendMessagesDialog.ResourceTreeNode)] = BuildQueueNode();
        }, out var dialogRef);

        await cut.Find("button:contains('Cancel')").ClickAsync();

        var result = await dialogRef.Result;
        result.ShouldNotBeNull();
        result.Canceled.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnOneCommandPerMessage_WhenResendAllClicked()
    {
        var messages = new List<ServiceBusMessage>
        {
            BuildMessage(sequenceNumber: 1, messageId: "msg-1"),
            BuildMessage(sequenceNumber: 2, messageId: "msg-2")
        };

        var cut = RenderDialog<ResendMessagesDialog>(p =>
        {
            p[nameof(ResendMessagesDialog.Messages)] = messages;
            p[nameof(ResendMessagesDialog.ResourceTreeNode)] = BuildQueueNode();
        }, out var dialogRef);

        await cut.Find("button:contains('Resend All')").ClickAsync();

        var result = await dialogRef.Result;
        result.ShouldNotBeNull();
        result.Canceled.ShouldBeFalse();
        var commands = result.Data.ShouldBeAssignableTo<IReadOnlyList<SendMessageBatchItem>>();
        commands!.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ClearMessageId_InReturnedCommands()
    {
        var messages = new List<ServiceBusMessage>
        {
            BuildMessage(messageId: "original-id")
        };

        var cut = RenderDialog<ResendMessagesDialog>(p =>
        {
            p[nameof(ResendMessagesDialog.Messages)] = messages;
            p[nameof(ResendMessagesDialog.ResourceTreeNode)] = BuildQueueNode();
        }, out var dialogRef);

        await cut.Find("button:contains('Resend All')").ClickAsync();

        var result = await dialogRef.Result;
        var commands = result!.Data.ShouldBeAssignableTo<IReadOnlyList<SendMessageBatchItem>>();
        commands![0].MessageId.ShouldBeNull();
    }

    [Fact]
    public async Task PersistEdits_WhenSwitchingBetweenMessages()
    {
        var messages = new List<ServiceBusMessage>
        {
            BuildMessage(sequenceNumber: 1, messageId: "msg-1", body: "{}"),
            BuildMessage(sequenceNumber: 2, messageId: "msg-2", body: "{}")
        };

        var cut = RenderDialog<ResendMessagesDialog>(p =>
        {
            p[nameof(ResendMessagesDialog.Messages)] = messages;
            p[nameof(ResendMessagesDialog.ResourceTreeNode)] = BuildQueueNode();
        }, out var dialogRef);

        // Edit body of first message (first is selected by default)
        await cut.Find("textarea").InputAsync("edited body");

        // Switch to second message
        var listItems = cut.FindAll(".resend-message-item");
        await listItems[1].ClickAsync();

        // Switch back to first
        listItems = cut.FindAll(".resend-message-item");
        await listItems[0].ClickAsync();

        // Confirm — edited body should appear in first command
        await cut.Find("button:contains('Resend All')").ClickAsync();

        var result = await dialogRef.Result;
        var commands = result!.Data.ShouldBeAssignableTo<IReadOnlyList<SendMessageBatchItem>>();
        commands![0].Body.ShouldBe("edited body");
    }
}
