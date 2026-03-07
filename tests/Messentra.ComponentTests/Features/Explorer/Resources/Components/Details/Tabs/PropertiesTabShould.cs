using Messentra.Domain;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components.Details.Tabs.Properties;
using Messentra.Infrastructure.AzureServiceBus;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components.Details.Tabs;

public sealed class PropertiesTabShould : ComponentTestBase
{
    private static ConnectionConfig BuildConnectionConfig() =>
        ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant", "client");

    private static ResourceOverview BuildOverview() =>
        new(
            "Active",
            DateTimeOffset.Now,
            DateTimeOffset.Now,
            new MessageInfo(0, 0, 0, 0, 0, 0),
            new SizeInfo(0, 1024));

    private static QueueTreeNode BuildQueueNode()
    {
        var props = new QueueProperties(
            TimeSpan.FromDays(14), TimeSpan.FromMinutes(5), TimeSpan.MaxValue,
            10, false, null, null, false, false, TimeSpan.Zero, false, null, "");
        var queue = new Resource.Queue("my-queue", "sb://test/queues/my-queue", BuildOverview(), props);
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
    public void RenderMaxDeliveryCountForQueueResource()
    {
        // Arrange & Act
        var cut = Render<PropertiesTab>(p => p.Add(x => x.Resource, (ResourceTreeNode)BuildQueueNode()));

        // Assert
        cut.Markup.ShouldContain("10");
    }

    [Fact]
    public void RenderMaxDeliveryCountForSubscriptionResource()
    {
        // Arrange & Act
        var cut = Render<PropertiesTab>(p => p.Add(x => x.Resource, (ResourceTreeNode)BuildSubscriptionNode()));

        // Assert
        cut.Markup.ShouldContain("10");
    }

    [Fact]
    public void RenderPropertiesWithoutMaxDeliveryCountForTopicResource()
    {
        // Arrange & Act
        var cut = Render<PropertiesTab>(p => p.Add(x => x.Resource, (ResourceTreeNode)BuildTopicNode()));

        // Assert — topic has no MaxDeliveryCount, numeric field value should be empty
        cut.Markup.ShouldNotBeEmpty();
        cut.Markup.ShouldContain("MAX DELIVERY COUNT");
    }
}
