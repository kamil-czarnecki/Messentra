using Bunit;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components.Details;
using Messentra.Infrastructure.AzureServiceBus;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components.Details;

public sealed class SendMessageDialogShould : ComponentTestBase
{
    private static ResourceTreeNode BuildQueueNode()
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

    [Fact]
    public void RenderDialogWithBodyFieldAndFormatSelector()
    {
        // Arrange & Act
        var cut = RenderDialog<SendMessageDialog>(p =>
            p[nameof(SendMessageDialog.ResourceTreeNode)] = BuildQueueNode());

        // Assert
        cut.Markup.ShouldContain("MESSAGE BODY");
    }

    [Fact]
    public async Task CloseDialogOnCancel()
    {
        // Arrange
        var cut = RenderDialog<SendMessageDialog>(
            p => p[nameof(SendMessageDialog.ResourceTreeNode)] = BuildQueueNode(),
            out var dialogRef);

        // Act
        await cut.Find("button:contains('Cancel')").ClickAsync();

        // Assert
        var result = await dialogRef.Result;
        result.ShouldNotBeNull();
        result.Canceled.ShouldBeTrue();
    }

    [Fact]
    public async Task CloseDialogWithSendMessageCommandOnSubmit()
    {
        // Arrange
        var cut = RenderDialog<SendMessageDialog>(
            p => p[nameof(SendMessageDialog.ResourceTreeNode)] = BuildQueueNode(),
            out var dialogRef);

        await cut.Find("textarea").InputAsync("{ \"test\": true }");

        // Act
        await cut.Find("button:contains('Send')").ClickAsync();

        // Assert
        var result = await dialogRef.Result;
        result.ShouldNotBeNull();
        result.Canceled.ShouldBeFalse();
        result.Data.ShouldBeOfType<SendMessageCommand>();
    }

    [Fact]
    public async Task ResolveDuplicateCustomPropertyKeysWithLastWriteWins_WhenSubmitting()
    {
        // Arrange
        var cut = RenderDialog<SendMessageDialog>(
            p => p[nameof(SendMessageDialog.ResourceTreeNode)] = BuildQueueNode(),
            out var dialogRef);

        await cut.Find("button:contains('ADD ROW')").ClickAsync();
        await cut.Find("button:contains('ADD ROW')").ClickAsync();

        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(2);

        await rows[0].QuerySelector(".custom-prop-key-cell input")!.InputAsync(" dup ");
        await rows[0].QuerySelector(".custom-prop-value-cell input")!.InputAsync("first");
        await rows[1].QuerySelector(".custom-prop-key-cell input")!.InputAsync("dup");
        await rows[1].QuerySelector(".custom-prop-value-cell input")!.InputAsync("second");

        // Act
        await cut.Find("button:contains('Send')").ClickAsync();

        // Assert
        var result = await dialogRef.Result;
        result.ShouldNotBeNull();
        result.Canceled.ShouldBeFalse();

        var command = result.Data.ShouldBeOfType<SendMessageCommand>();
        command.ApplicationProperties.Count.ShouldBe(1);
        command.ApplicationProperties["dup"].ShouldBe("second");
    }
}
