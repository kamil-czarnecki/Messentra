using Messentra.Domain;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components.Details.Tabs.Overview;
using Messentra.Infrastructure.AzureServiceBus;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components.Details.Tabs;

public sealed class OverviewTabShould : ComponentTestBase
{
    private static ResourceOverview BuildOverview(string status = "Active") =>
        new(
            status,
            DateTimeOffset.Now,
            DateTimeOffset.Now,
            new MessageInfo(10, 2, 3, 1, 0, 16),
            new SizeInfo(1024, 1024));

    private static ConnectionConfig BuildConnectionConfig() =>
        ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant", "client");

    private static QueueTreeNode BuildQueueNode(string status = "Active")
    {
        var props = new QueueProperties(
            TimeSpan.FromDays(14), TimeSpan.FromMinutes(5), TimeSpan.MaxValue,
            10, false, null, null, false, false, TimeSpan.Zero, false, null, "");
        var queue = new Resource.Queue("my-queue", "sb://test/queues/my-queue", BuildOverview(status), props);
        return new QueueTreeNode("TestNS", queue, BuildConnectionConfig());
    }

    private static TopicTreeNode BuildTopicNode()
    {
        var props = new TopicProperties(
            TimeSpan.FromDays(14), TimeSpan.MaxValue, false, false, TimeSpan.Zero, null, "");
        var topic = new Resource.Topic("my-topic", "sb://test/topics/my-topic", BuildOverview(), props, []);
        return new TopicTreeNode("TestNS", topic, BuildConnectionConfig());
    }

    private static SubscriptionTreeNode BuildSubscriptionNode()
    {
        var props = new SubscriptionProperties(
            TimeSpan.FromDays(14), TimeSpan.FromMinutes(5), TimeSpan.MaxValue,
            10, false, null, null, false, "");
        var sub = new Resource.Subscription("my-sub", "my-topic", "sb://test/topics/my-topic/subscriptions/my-sub", BuildOverview(), props);
        return new SubscriptionTreeNode("TestNS", sub, BuildConnectionConfig());
    }

    [Fact]
    public void RenderStatusForQueueResource()
    {
        // Arrange & Act
        var cut = Render<OverviewTab>(p => p.Add(x => x.Resource, (ResourceTreeNode)BuildQueueNode()));

        // Assert
        cut.Markup.ShouldContain("Active");
    }

    [Fact]
    public void RenderMessageCountsForQueueResource()
    {
        // Arrange & Act
        var cut = Render<OverviewTab>(p => p.Add(x => x.Resource, (ResourceTreeNode)BuildQueueNode()));

        // Assert
        cut.Markup.ShouldContain("10");
    }

    [Fact]
    public void RenderStatusForTopicResource()
    {
        // Arrange & Act
        var cut = Render<OverviewTab>(p => p.Add(x => x.Resource, (ResourceTreeNode)BuildTopicNode()));

        // Assert
        cut.Markup.ShouldContain("Active");
    }

    [Fact]
    public void RenderStatusForSubscriptionResource()
    {
        // Arrange & Act
        var cut = Render<OverviewTab>(p => p.Add(x => x.Resource, (ResourceTreeNode)BuildSubscriptionNode()));

        // Assert
        cut.Markup.ShouldContain("Active");
    }

    [Fact]
    public void RenderWithoutCrashingForNamespaceNode()
    {
        // Arrange
        var connectionConfig = BuildConnectionConfig();
        var node = new NamespaceTreeNode("TestNS", connectionConfig);

        // Act
        var cut = Render<OverviewTab>(p => p.Add(x => x.Resource, (ResourceTreeNode)node));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
    }
}
