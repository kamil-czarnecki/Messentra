using Bunit;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components.Details.Tabs;
using Messentra.Features.Explorer.Resources.Components.Details.Tabs.Messages;
using Messentra.Infrastructure.AzureServiceBus;
using Microsoft.AspNetCore.Components;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components.Details.Tabs.Messages;

public sealed class MessagesTabShould : ComponentTestBase
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
            TimeSpan.FromDays(14), TimeSpan.FromMinutes(5), TimeSpan.MaxValue,
            10, false, null, null, false, false, TimeSpan.Zero, false, null, "");
        var queue = new Resource.Queue("my-queue", "sb://test/queues/my-queue", overview, props);
        var connectionConfig = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant", "client");
        return new QueueTreeNode("TestNS", queue, connectionConfig);
    }

    [Fact]
    public void RenderMessageGridComponent()
    {
        // Arrange & Act
        var cut = Render<MessagesTab>(p => p
            .Add(x => x.ResourceTreeNode, BuildQueueNode())
            .Add(x => x.SubQueue, SubQueue.Active)
            .Add(x => x.OnRefresh, EventCallback.Empty));

        // Assert
        cut.FindComponent<MessageGrid>().ShouldNotBeNull();
    }

    [Fact]
    public void PassCorrectSubQueueParameterToMessageGrid()
    {
        // Arrange & Act
        var cut = Render<MessagesTab>(p => p
            .Add(x => x.ResourceTreeNode, BuildQueueNode())
            .Add(x => x.SubQueue, SubQueue.DeadLetter)
            .Add(x => x.OnRefresh, EventCallback.Empty));

        // Assert
        cut.FindComponent<MessageGrid>().Instance.SubQueue.ShouldBe(SubQueue.DeadLetter);
    }
}
