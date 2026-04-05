using Bunit;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Messages.FetchQueueMessages;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components.Details.Tabs;
using Messentra.Infrastructure.AzureServiceBus;
using Microsoft.AspNetCore.Components;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components.Details.Tabs.Messages;

public sealed class MessageGridShould : ComponentTestBase
{
    private static QueueTreeNode BuildQueueNode(string connectionName = "TestNS", string queueName = "my-queue")
    {
        var overview = new ResourceOverview(
            "Active",
            DateTimeOffset.Now,
            DateTimeOffset.Now,
            new MessageInfo(0, 0, 0, 0, 0, 0),
            new SizeInfo(0, 1024));
        var props = new QueueProperties(
            TimeSpan.FromDays(14), TimeSpan.FromMinutes(5), TimeSpan.MaxValue,
            10, false, null, null, false, false, TimeSpan.Zero, false, null, "");
        var queue = new Resource.Queue(queueName, $"sb://test/queues/{queueName}", overview, props);
        var connectionConfig = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant", "client");
        return new QueueTreeNode(connectionName, queue, connectionConfig);
    }

    private static ServiceBusMessage BuildServiceBusMessage(Mock<IServiceBusMessageContext>? contextMock = null, string messageId = "msg-1")
    {
        var brokerProperties = new BrokerProperties(
            MessageId: messageId,
            SequenceNumber: 1,
            CorrelationId: null, SessionId: null, ReplyToSessionId: null,
            EnqueuedTimeUtc: DateTime.UtcNow, ScheduledEnqueueTimeUtc: DateTime.UtcNow,
            TimeToLive: TimeSpan.FromDays(1),
            LockedUntilUtc: DateTime.UtcNow.AddMinutes(5),
            ExpiresAtUtc: DateTime.UtcNow.AddDays(1),
            DeliveryCount: 1,
            Label: null, To: null, ReplyTo: null, PartitionKey: null, ContentType: null,
            DeadLetterReason: null, DeadLetterErrorDescription: null);
        var dto = new MessageDto("Hello", brokerProperties, new Dictionary<string, object>());
        return new ServiceBusMessage(dto, contextMock?.Object ?? new Mock<IServiceBusMessageContext>().Object);
    }

    private IRenderedComponent<MessageGrid> RenderMessageGrid(ResourceTreeNode node) =>
        Render<MessageGrid>(p => p
            .Add(x => x.ResourceTreeNode, node)
            .Add(x => x.SubQueue, SubQueue.Active)
            .Add(x => x.OnRefresh, EventCallback.Empty));

    private void SetupFetchResponse(params ServiceBusMessage[] messages)
    {
        MockMediator
            .Setup(x => x.Send(It.IsAny<FetchQueueMessagesQuery>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<IReadOnlyCollection<ServiceBusMessage>>(messages));
    }

    private static Task SetResourceNode(IRenderedComponent<MessageGrid> cut, ResourceTreeNode node)
    {
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(MessageGrid.ResourceTreeNode)] = node,
            [nameof(MessageGrid.SubQueue)] = SubQueue.Active,
            [nameof(MessageGrid.OnRefresh)] = EventCallback.Empty
        });

        return cut.InvokeAsync(() => cut.Instance.SetParametersAsync(parameters));
    }

    private async Task FetchMessagesThroughUi(IRenderedComponent<MessageGrid> cut, FetchMode mode = FetchMode.Peek)
    {
        // ClickAsync causes deadlock 
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Fetch").Click();

        if (mode == FetchMode.Receive)
        {
            await MudDialog.FindAll(".mud-toggle-item").Last().ClickAsync();
        }

        await MudDialog.FindAll("button").Single(x => x.TextContent.Trim() == "Fetch").ClickAsync();
        await cut.InvokeAsync(() => Task.CompletedTask);
    }

    private static void SelectFirstMessageInGrid(IRenderedComponent<MessageGrid> cut)
    {
        cut.WaitForElements(".mud-table-body .mud-checkbox input[type='checkbox']", 1);
        cut.Find(".mud-table-body .mud-checkbox input[type='checkbox']").Change(true);
    }

    [Fact]
    public async Task RetainMessagesWhenSameResourceNodeIsRefreshed()
    {
        // Arrange
        var node = BuildQueueNode();
        var message = BuildServiceBusMessage(messageId: "same-node-msg");
        SetupFetchResponse(message);
        var cut = RenderMessageGrid(node);

        // Act
        await FetchMessagesThroughUi(cut);
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("same-node-msg"));
        var refreshedNode = node with { IsLoading = false };
        await SetResourceNode(cut, refreshedNode);

        // Assert
        cut.Markup.ShouldContain("same-node-msg");
    }

    [Fact]
    public async Task ClearMessagesWhenNavigatingToDifferentResource()
    {
        // Arrange
        var node = BuildQueueNode(queueName: "queue-a");
        var message = BuildServiceBusMessage(messageId: "other-resource-msg");
        SetupFetchResponse(message);
        var cut = RenderMessageGrid(node);

        // Act
        await FetchMessagesThroughUi(cut);
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("other-resource-msg"));
        var differentNode = BuildQueueNode(queueName: "queue-b");
        await SetResourceNode(cut, differentNode);

        // Assert
        cut.Markup.ShouldNotContain("other-resource-msg");
    }

    [Fact]
    public async Task ClearMessagesWhenNavigatingToDifferentConnection()
    {
        // Arrange
        var node = BuildQueueNode(connectionName: "NS-1");
        var message = BuildServiceBusMessage(messageId: "other-connection-msg");
        SetupFetchResponse(message);
        var cut = RenderMessageGrid(node);

        // Act
        await FetchMessagesThroughUi(cut);
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("other-connection-msg"));
        var differentConnection = BuildQueueNode(connectionName: "NS-2");
        await SetResourceNode(cut, differentConnection);

        // Assert
        cut.Markup.ShouldNotContain("other-connection-msg");
    }

    [Fact]
    public async Task ResendWithAutoCompleteSendsCommandAndCompletesSelectedMessage()
    {
        // Arrange
        var contextMock = new Mock<IServiceBusMessageContext>();
        contextMock.Setup(x => x.Complete(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var message = BuildServiceBusMessage(contextMock, "auto-complete-msg");
        SetupFetchResponse(message);

        MockMediator
            .Setup(x => x.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new SendMessageResult(new Success())));

        var cut = RenderMessageGrid(BuildQueueNode());

        // Act
        await FetchMessagesThroughUi(cut, FetchMode.Receive);
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("auto-complete-msg"));
        SelectFirstMessageInGrid(cut);
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Resend").Click();

        // Confirm resend in the dialog
        await MudDialog.Find("button:contains('Resend All')").ClickAsync();

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            MockMediator.Verify(x => x.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            contextMock.Verify(x => x.Complete(It.IsAny<CancellationToken>()), Times.Once);
            cut.Markup.ShouldNotContain("auto-complete-msg");
        });
    }

    [Fact]
    public async Task ResendWithoutAutoCompleteSendsCommandButDoesNotCompleteAndKeepsMessageInGrid()
    {
        // Arrange
        var contextMock = new Mock<IServiceBusMessageContext>();
        contextMock.Setup(x => x.Complete(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var message = BuildServiceBusMessage(contextMock, "no-auto-complete-msg");
        SetupFetchResponse(message);

        MockMediator
            .Setup(x => x.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new SendMessageResult(new Success())));

        var cut = RenderMessageGrid(BuildQueueNode());

        // Act
        await FetchMessagesThroughUi(cut);
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("no-auto-complete-msg"));
        SelectFirstMessageInGrid(cut);
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Resend").Click();

        // Confirm resend in the dialog
        await MudDialog.Find("button:contains('Resend All')").ClickAsync();

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            MockMediator.Verify(x => x.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            contextMock.Verify(x => x.Complete(It.IsAny<CancellationToken>()), Times.Never);
            cut.Markup.ShouldContain("no-auto-complete-msg");
        });
    }
}
