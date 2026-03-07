using Bunit;
using Messentra.Domain;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components.Details;
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
}
