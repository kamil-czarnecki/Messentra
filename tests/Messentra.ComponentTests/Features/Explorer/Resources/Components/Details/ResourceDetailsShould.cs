using Bunit;
using Mediator;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components.Details;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ExportMessages.EnqueueExportMessages;
using Messentra.Features.Layout.State;
using Messentra.Infrastructure.AzureServiceBus;
using Moq;
using MudBlazor;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components.Details;

public sealed class ResourceDetailsShould : ComponentTestBase
{
    private static QueueTreeNode BuildQueueNode(string name = "my-queue", string status = "Active")
    {
        var overview = new ResourceOverview(
            status,
            DateTimeOffset.Now,
            DateTimeOffset.Now,
            new MessageInfo(10, 2, 0, 0, 0, 12),
            new SizeInfo(1024, 1024));
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
        var queue = new Resource.Queue(name, $"sb://test/queues/{name}", overview, props);
        var connectionConfig = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant", "client");
        return new QueueTreeNode("TestNS", queue, connectionConfig);
    }

    [Fact]
    public void ShowWelcomeMessageWhenNoResourceSelected()
    {
        // Arrange & Act
        var cut = Render<ResourceDetails>(p => p.Add(x => x.SelectedResource, null));

        // Assert
        cut.Markup.ShouldContain("Welcome to Messentra");
    }

    [Fact]
    public void ShowResourceNameWhenResourceIsSelected()
    {
        // Arrange
        var node = BuildQueueNode();
        
        // Act
        var cut = Render<ResourceDetails>(p => p.Add(x => x.SelectedResource, node));

        // Assert
        cut.Markup.ShouldContain("my-queue");
    }

    [Fact]
    public void ShowActiveStatusChipForActiveQueue()
    {
        // Arrange
        var node = BuildQueueNode(status: "Active");
        
        // Act
        var cut = Render<ResourceDetails>(p => p.Add(x => x.SelectedResource, node));

        // Assert
        cut.Markup.ShouldContain("Active");
    }

    [Fact]
    public void DispatchRefreshQueueActionOnRefreshClick()
    {
        // Arrange
        var node = BuildQueueNode();
        var cut = Render<ResourceDetails>(p => p.Add(x => x.SelectedResource, node));

        // Act
        cut.FindComponent<MudIconButton>().Find("button").Click();

        // Assert
        MockDispatcher.Verify(x => x.Dispatch(It.IsAny<RefreshQueueAction>()), Times.Once);
    }

    [Fact]
    public void KeepExportDisabled_WhenOverviewTabIsActive()
    {
        // Arrange
        var node = BuildQueueNode();

        // Act
        var cut = Render<ResourceDetails>(p => p.Add(x => x.SelectedResource, node));

        // Assert
        cut.Find("button[title='Export']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task EnableExport_WhenMessagesTabIsActive()
    {
        // Arrange
        var node = BuildQueueNode();
        var cut = Render<ResourceDetails>(p => p.Add(x => x.SelectedResource, node));

        // Act
        await cut.Find(".mud-tab:contains('Messages')").ClickAsync();

        // Assert
        cut.Find("button[title='Export']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public async Task EnqueueDlqExportAndRefreshJobs_WhenExportConfirmedFromDeadLetterTab()
    {
        // Arrange
        var node = BuildQueueNode();
        MockMediator
            .Setup(x => x.Send(It.IsAny<EnqueueExportMessagesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var cut = Render<ResourceDetails>(p => p.Add(x => x.SelectedResource, node));
        await cut.Find(".mud-tab:contains('Dead-letter')").ClickAsync();

        // Act
        cut.Find("button[title='Export']").Click();
        await MudDialog.Find("button:contains('Export')").ClickAsync();

        // Assert
        MockMediator.Verify(
            x => x.Send(
                It.Is<EnqueueExportMessagesCommand>(command => IsDeadLetterQueueExport(command)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        MockDispatcher.Verify(x => x.Dispatch(It.IsAny<FetchJobsAction>()), Times.Once);
        MockDispatcher.Verify(
            x => x.Dispatch(It.Is<LogActivityAction>(a =>
                a.Log.Connection == "TestNS" &&
                a.Log.Level == "Info" &&
                a.Log.Message.Contains("Export job enqueued"))),
            Times.Once);

    }

    private static bool IsDeadLetterQueueExport(EnqueueExportMessagesCommand command)
    {
        if (command.Request.Target is not ResourceTarget.Queue queueTarget)
            return false;

        return queueTarget.QueueName == "my-queue" &&
               queueTarget.SubQueue == SubQueue.DeadLetter &&
               command.Request.TotalNumberOfMessagesToFetch == 2;
    }
}
