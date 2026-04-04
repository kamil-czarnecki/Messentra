using Fluxor;
using Messentra.Domain;
using Messentra.Features.Explorer.Resources;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Resources;

public sealed class ResourceSelectorShould
{
    private const string ConnectionName = "test-ns";
    private static readonly ConnectionConfig Config = ResourceTestData.CreateConnectionConfig();

    [Fact]
    public void SortTopicsAlphabeticallyWhenInsertedOutOfOrder()
    {
        // Arrange
        var state = StateWithTopics(
            ("zeta-topic", []),
            ("alpha-topic", []),
            ("mu-topic", []));
        var selector = BuildSelector(state);

        // Act
        var topicNames = ExtractTopicNames(selector);

        // Assert
        topicNames.ShouldBe(["alpha-topic", "mu-topic", "zeta-topic"]);
    }

    [Fact]
    public void SortTopicsCaseInsensitively()
    {
        // Arrange
        var state = StateWithTopics(
            ("Zebra-topic", []),
            ("apple-topic", []),
            ("Mango-topic", []));
        var selector = BuildSelector(state);

        // Act
        var topicNames = ExtractTopicNames(selector);

        // Assert
        topicNames.ShouldBe(["apple-topic", "Mango-topic", "Zebra-topic"]);
    }

    [Fact]
    public void SortSubscriptionsAlphabeticallyWithinTopic()
    {
        // Arrange
        var state = StateWithTopics(
            ("orders-topic", ["zebra-sub", "alpha-sub", "mu-sub"]));
        var selector = BuildSelector(state);

        // Act
        var subNames = ExtractSubscriptionNames(selector, "orders-topic");

        // Assert
        subNames.ShouldBe(["alpha-sub", "mu-sub", "zebra-sub"]);
    }

    [Fact]
    public void SortQueuesAlphabeticallyWhenInsertedOutOfOrder()
    {
        // Arrange
        var state = StateWithQueues("zeta-queue", "alpha-queue", "mu-queue");
        var selector = BuildSelector(state);

        // Act
        var queueNames = ExtractQueueNames(selector);

        // Assert
        queueNames.ShouldBe(["alpha-queue", "mu-queue", "zeta-queue"]);
    }

    private static ResourceSelector BuildSelector(ResourceState state)
    {
        var mockFeature = new Mock<IFeature<ResourceState>>();
        mockFeature.Setup(f => f.State).Returns(state);
        return new ResourceSelector(mockFeature.Object);
    }

    private static ResourceState StateWithTopics(params (string name, string[] subscriptions)[] topics)
    {
        var topicEntries = new Dictionary<string, TopicEntry>();

        foreach (var (name, subs) in topics)
        {
            var topic = ResourceTestData.CreateTopic(name);
            var node = new TopicTreeNode(ConnectionName, topic, Config);

            var subEntries = new Dictionary<string, SubscriptionEntry>();
            foreach (var subName in subs)
            {
                var sub = ResourceTestData.CreateSubscription(subName, name);
                var subNode = new SubscriptionTreeNode(ConnectionName, sub, Config);
                subEntries[sub.Url] = new SubscriptionEntry(subNode, false);
            }

            topicEntries[topic.Url] = new TopicEntry(node, false, subEntries);
        }

        return new ResourceState(
            [new NamespaceEntry(1L, ConnectionName, Config, false, [], topicEntries, [])],
            null,
            [$"ns:{ConnectionName}"]);
    }

    private static ResourceState StateWithQueues(params string[] names)
    {
        var queueEntries = new Dictionary<string, QueueEntry>();
        foreach (var name in names)
        {
            var queue = ResourceTestData.CreateQueue(name);
            queueEntries[queue.Url] = new QueueEntry(new QueueTreeNode(ConnectionName, queue, Config), false);
        }

        return new ResourceState(
            [new NamespaceEntry(1L, ConnectionName, Config, false, queueEntries, [], [])],
            null,
            [$"ns:{ConnectionName}"]);
    }

    private static List<string?> ExtractTopicNames(ResourceSelector selector) =>
        selector.TreeItems.Value
            .First()
            .Children!.OfType<ResourceTreeItemData>()
            .First(g => g.Text == "Topics")
            .Children!.OfType<ResourceTreeItemData>()
            .Select(t => t.Text)
            .ToList();

    private static List<string?> ExtractQueueNames(ResourceSelector selector) =>
        selector.TreeItems.Value
            .First()
            .Children!.OfType<ResourceTreeItemData>()
            .First(g => g.Text == "Queues")
            .Children!.OfType<ResourceTreeItemData>()
            .Select(q => q.Text)
            .ToList();

    private static List<string?> ExtractSubscriptionNames(ResourceSelector selector, string topicName) =>
        selector.TreeItems.Value
            .First()
            .Children!.OfType<ResourceTreeItemData>()
            .First(g => g.Text == "Topics")
            .Children!.OfType<ResourceTreeItemData>()
            .First(t => t.Text == topicName)
            .Children!.OfType<ResourceTreeItemData>()
            .Select(s => s.Text)
            .ToList();

    [Fact]
    public void PlaceFoldersGroupAsFirstChildOfNamespace()
    {
        // Arrange
        var state = StateWithQueues("queue-1");
        var selector = BuildSelector(state);

        // Act
        var firstChild = selector.TreeItems.Value
            .First()
            .Children!.OfType<ResourceTreeItemData>()
            .First();

        // Assert
        firstChild.Value.ShouldBeOfType<FoldersTreeNode>();
    }

    [Fact]
    public void RenderFolderWithItsResources()
    {
        // Arrange
        var queue = ResourceTestData.CreateQueue("orders");
        var queueEntry = new QueueEntry(new QueueTreeNode(ConnectionName, queue, Config), false);
        var folderEntry = ResourceTestData.CreateFolderEntry(10L, "My Team", ConnectionName, resourceUrls: [queue.Url]);
        var namespaceEntry = new NamespaceEntry(
            ConnectionId: 1L, ConnectionName, Config, false,
            Queues: new Dictionary<string, QueueEntry> { [queue.Url] = queueEntry },
            Topics: [],
            Folders: new Dictionary<long, FolderEntry> { [10L] = folderEntry });
        var state = new ResourceState([namespaceEntry], null, [$"ns:{ConnectionName}"]);
        var selector = BuildSelector(state);

        // Act
        var foldersGroup = selector.TreeItems.Value.First()
            .Children!.OfType<ResourceTreeItemData>()
            .First(c => c.Value is FoldersTreeNode);
        var folder = foldersGroup.Children!.OfType<ResourceTreeItemData>().First();
        var resourceInFolder = folder.Children!.OfType<ResourceTreeItemData>().First();

        // Assert
        folder.Value.ShouldBeOfType<FolderTreeNode>();
        ((FolderTreeNode)folder.Value!).Name.ShouldBe("My Team");
        resourceInFolder.Value.ShouldBeOfType<QueueTreeNode>();
        ((QueueTreeNode)resourceInFolder.Value!).Resource.Url.ShouldBe(queue.Url);
    }
}
