using Bunit;
using Mediator;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Messages.FetchQueueMessages;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components.Details.Tabs;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ExportSelectedMessages.EnqueueExportSelectedMessages;
using Messentra.Infrastructure.AzureServiceBus;
using Microsoft.AspNetCore.Components;
using Moq;
using Shouldly;
using System.Text.RegularExpressions;
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

    private static ServiceBusMessage BuildServiceBusMessage(
        Mock<IServiceBusMessageContext>? contextMock = null,
        string messageId = "msg-1",
        long sequenceNumber = 1)
    {
        var brokerProperties = new BrokerProperties(
            MessageId: messageId,
            SequenceNumber: sequenceNumber,
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

    private IRenderedComponent<MessageGrid> RenderMessageGrid(ResourceTreeNode node, SubQueue subQueue = SubQueue.Active) =>
        Render<MessageGrid>(p => p
            .Add(x => x.ResourceTreeNode, node)
            .Add(x => x.SubQueue, subQueue)
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
        cut.WaitForAssertion(() =>
            cut.FindAll("tbody .mud-checkbox input[type='checkbox']").Count.ShouldBeGreaterThan(0));
        cut.FindAll("tbody .mud-checkbox input[type='checkbox']").First().Change(true);
    }

    private static bool HasSelectedCountLabel(IRenderedComponent<MessageGrid> cut)
    {
        return cut.FindAll(".mud-typography")
            .Select(x => x.TextContent.Trim())
            .Any(x => Regex.IsMatch(x, @"^\d+\s+selected$", RegexOptions.IgnoreCase));
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
            .Setup(x => x.Send(It.IsAny<SendMessagesCommand>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new SendMessagesResult(
                TotalCount: 1,
                SentCount: 1,
                SentSequenceNumbers: new HashSet<long> { 1 },
                Errors: [])));

        var cut = RenderMessageGrid(BuildQueueNode(), SubQueue.DeadLetter);

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
            MockMediator.Verify(x => x.Send(It.IsAny<SendMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            contextMock.Invocations
                .Count(x => x.Method.Name == nameof(IServiceBusMessageContext.Complete))
                .ShouldBeGreaterThan(0);
        });
    }

    [Fact]
    public async Task ResendWithoutAutoCompleteSendsCommandButDoesNotCompleteSelectedMessage()
    {
        // Arrange
        var contextMock = new Mock<IServiceBusMessageContext>();
        contextMock.Setup(x => x.Complete(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var message = BuildServiceBusMessage(contextMock, "no-auto-complete-msg");
        SetupFetchResponse(message);

        MockMediator
            .Setup(x => x.Send(It.IsAny<SendMessagesCommand>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new SendMessagesResult(
                TotalCount: 1,
                SentCount: 1,
                SentSequenceNumbers: new HashSet<long> { 1 },
                Errors: [])));

        var cut = RenderMessageGrid(BuildQueueNode(), SubQueue.DeadLetter);

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
            MockMediator.Verify(x => x.Send(It.IsAny<SendMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            contextMock.Verify(x => x.Complete(It.IsAny<CancellationToken>()), Times.Never);
        });
    }

    [Fact]
    public async Task ResendWithAutoCompleteOnlyCompletesSuccessfullyResentMessages()
    {
        // Arrange
        var firstContext = new Mock<IServiceBusMessageContext>();
        firstContext.Setup(x => x.Complete(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var secondContext = new Mock<IServiceBusMessageContext>();
        secondContext.Setup(x => x.Complete(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var firstMessage = BuildServiceBusMessage(firstContext, "partial-1", sequenceNumber: 1);
        var secondMessage = BuildServiceBusMessage(secondContext, "partial-2", sequenceNumber: 2);
        SetupFetchResponse(firstMessage, secondMessage);

        MockMediator
            .Setup(x => x.Send(It.IsAny<SendMessagesCommand>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new SendMessagesResult(
                TotalCount: 2,
                SentCount: 1,
                SentSequenceNumbers: new HashSet<long> { 2 },
                Errors: [new SendMessagesError(1, "oversized")])));

        var cut = RenderMessageGrid(BuildQueueNode(), SubQueue.DeadLetter);

        // Act
        await FetchMessagesThroughUi(cut, FetchMode.Receive);
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("partial-1"));
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("partial-2"));

        var checkboxes = cut.FindAll("tbody .mud-checkbox input[type='checkbox']");
        checkboxes[0].Change(true);
        checkboxes[1].Change(true);

        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Resend").Click();
        await MudDialog.Find("button:contains('Resend All')").ClickAsync();

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            MockMediator.Verify(x => x.Send(It.IsAny<SendMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            firstContext.Verify(x => x.Complete(It.IsAny<CancellationToken>()), Times.Never);
            secondContext.Verify(x => x.Complete(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Fact]
    public async Task EnqueueExportSelectedMessagesJobWhenExportConfirmed()
    {
        // Arrange
        var message = BuildServiceBusMessage(messageId: "export-msg-1", sequenceNumber: 1);
        SetupFetchResponse(message);
        MockMediator
            .Setup(x => x.Send(It.IsAny<EnqueueExportSelectedMessagesCommand>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(Unit.Value));

        var cut = RenderMessageGrid(BuildQueueNode());

        // Act — fetch, select, click Export selected, confirm dialog
        await FetchMessagesThroughUi(cut);
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("export-msg-1"));
        SelectFirstMessageInGrid(cut);

        cut.FindAll("button").Single(x => x.TextContent.Trim().StartsWith("Export selected")).Click();

        await MudDialog.Find("button.mud-button-text-primary").ClickAsync();

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            MockMediator.Verify(
                x => x.Send(It.IsAny<EnqueueExportSelectedMessagesCommand>(), It.IsAny<CancellationToken>()),
                Times.Once);
            MockDispatcher.Verify(x => x.Dispatch(It.IsAny<FetchJobsAction>()), Times.Once);
        });
    }

    [Fact]
    public async Task NotShowSelectedCountLabelWhenNoItemsSelected()
    {
        // Arrange
        var message = BuildServiceBusMessage(messageId: "no-selection-msg", sequenceNumber: 1);
        SetupFetchResponse(message);
        var cut = RenderMessageGrid(BuildQueueNode());

        // Act
        await FetchMessagesThroughUi(cut);
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("no-selection-msg"));

        // Assert
        HasSelectedCountLabel(cut).ShouldBeFalse();
    }

    [Fact]
    public async Task ShowCorrectCountInSelectedCountLabel()
    {
        // Arrange
        var message = BuildServiceBusMessage(messageId: "count-test-msg", sequenceNumber: 1);
        SetupFetchResponse(message);
        var cut = RenderMessageGrid(BuildQueueNode());

        // Act
        await FetchMessagesThroughUi(cut);
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("count-test-msg"));
        SelectFirstMessageInGrid(cut);

        // Assert
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("1 selected"));
    }

    [Fact]
    public async Task KeepSelectedCountLabelVisibleWhenFilterHidesSelectedMessage()
    {
        // Arrange
        var selectedMessage = BuildServiceBusMessage(messageId: "hidden-after-filter", sequenceNumber: 1);
        var otherMessage = BuildServiceBusMessage(messageId: "show-only-this", sequenceNumber: 2);
        SetupFetchResponse(selectedMessage, otherMessage);
        var cut = RenderMessageGrid(BuildQueueNode());

        // Act
        await FetchMessagesThroughUi(cut);
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("hidden-after-filter"));
        SelectFirstMessageInGrid(cut);
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("1 selected"));

        cut.Find("input[placeholder='Search']").Input("show-only-this");

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            cut.Markup.ShouldNotContain("hidden-after-filter");
            cut.Markup.ShouldContain("1 selected");
        });
    }

    [Fact]
    public async Task HideSelectedCountLabelAfterDeselectingAllItems()
    {
        // Arrange
        var message = BuildServiceBusMessage(messageId: "deselect-me-msg", sequenceNumber: 1);
        SetupFetchResponse(message);
        var cut = RenderMessageGrid(BuildQueueNode());

        // Act
        await FetchMessagesThroughUi(cut);
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("deselect-me-msg"));
        SelectFirstMessageInGrid(cut);
        await cut.WaitForAssertionAsync(() => cut.Markup.ShouldContain("1 selected"));

        cut.Find(".mud-table-body .mud-checkbox input[type='checkbox']").Change(false);

        // Assert
        await cut.WaitForAssertionAsync(() => HasSelectedCountLabel(cut).ShouldBeFalse());
    }
}
