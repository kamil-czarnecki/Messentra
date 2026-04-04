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

    [Fact]
    public void GroupSubscriptionUnderDerivedTopicHeaderInsideFolder()
    {
        // Arrange
        var state = StateWithFolderContainingSubs("orders-topic", ["sub-1"], "my-folder");
        var selector = BuildSelector(state);

        // Act
        var folder = GetFolderItem(selector, "my-folder");
        var topicHeader = folder.Children!.OfType<ResourceTreeItemData>().Single();

        // Assert
        topicHeader.Value.ShouldBeOfType<TopicTreeNode>();
        topicHeader.Text.ShouldBe("orders-topic");
        topicHeader.Children!.OfType<ResourceTreeItemData>().Single().Text.ShouldBe("sub-1");
    }

    [Fact]
    public void GroupMultipleSubsFromSameTopicUnderOneDerivedHeader()
    {
        // Arrange
        var state = StateWithFolderContainingSubs("orders-topic", ["sub-a", "sub-b"], "my-folder");
        var selector = BuildSelector(state);

        // Act
        var folder = GetFolderItem(selector, "my-folder");

        // Assert
        folder.Children!.OfType<ResourceTreeItemData>().Count().ShouldBe(1);
        folder.Children!.OfType<ResourceTreeItemData>().Single()
            .Children!.OfType<ResourceTreeItemData>().Select(c => c.Text)
            .ShouldBe(["sub-a", "sub-b"]);
    }

    [Fact]
    public void SortFolderTopLevelItemsAlphabetically()
    {
        // Arrange
        var queue = ResourceTestData.CreateQueue("zeta-queue");
        var queueEntry = new QueueEntry(new QueueTreeNode(ConnectionName, queue, Config), false);
        var sub = ResourceTestData.CreateSubscription("sub-1", "alpha-topic");
        var topic = ResourceTestData.CreateTopic("alpha-topic");
        var subEntry = new SubscriptionEntry(new SubscriptionTreeNode(ConnectionName, sub, Config), false);
        var topicEntry = new TopicEntry(new TopicTreeNode(ConnectionName, topic, Config), false,
            new Dictionary<string, SubscriptionEntry> { [sub.Url] = subEntry });
        var folder = ResourceTestData.CreateFolderEntry(10L, "f", ConnectionName, resourceUrls: [queue.Url, sub.Url]);
        var ns = ResourceTestData.CreateNamespaceEntry(ConnectionName,
            queues: new Dictionary<string, QueueEntry> { [queue.Url] = queueEntry },
            topics: new Dictionary<string, TopicEntry> { [topic.Url] = topicEntry },
            folders: new Dictionary<long, FolderEntry> { [10L] = folder });
        var state = new ResourceState([ns], null, [$"ns:{ConnectionName}", "folder:10"]);
        var selector = BuildSelector(state);

        // Act
        var topLevelNames = GetFolderItem(selector, "f")
            .Children!.OfType<ResourceTreeItemData>().Select(i => i.Text).ToList();

        // Assert
        topLevelNames.ShouldBe(["alpha-topic", "zeta-queue"]);
    }

    [Fact]
    public void SortSubsAlphabeticallyWithinDerivedTopicHeader()
    {
        // Arrange
        var state = StateWithFolderContainingSubs("orders-topic", ["zebra-sub", "alpha-sub"], "my-folder");
        var selector = BuildSelector(state);

        // Act
        var subNames = GetFolderItem(selector, "my-folder")
            .Children!.OfType<ResourceTreeItemData>().Single()
            .Children!.OfType<ResourceTreeItemData>().Select(c => c.Text).ToList();

        // Assert
        subNames.ShouldBe(["alpha-sub", "zebra-sub"]);
    }

    [Fact]
    public void SetParentFolderNodeOnDerivedTopicHeader()
    {
        // Arrange
        var state = StateWithFolderContainingSubs("orders-topic", ["sub-1"], "my-folder");
        var selector = BuildSelector(state);

        // Act
        var topicHeader = GetFolderItem(selector, "my-folder")
            .Children!.OfType<ResourceTreeItemData>().Single();

        // Assert
        topicHeader.ParentFolderNode.ShouldNotBeNull();
        topicHeader.ParentFolderNode!.Name.ShouldBe("my-folder");
    }

    [Fact]
    public void SetParentFolderNodeOnSubUnderDerivedTopicHeader()
    {
        // Arrange
        var state = StateWithFolderContainingSubs("orders-topic", ["sub-1"], "my-folder");
        var selector = BuildSelector(state);

        // Act
        var sub = GetFolderItem(selector, "my-folder")
            .Children!.OfType<ResourceTreeItemData>().Single()
            .Children!.OfType<ResourceTreeItemData>().Single();

        // Assert
        sub.ParentFolderNode.ShouldNotBeNull();
        sub.ParentFolderNode!.Name.ShouldBe("my-folder");
    }

    private static ResourceTreeItemData GetFolderItem(ResourceSelector selector, string folderName) =>
        selector.TreeItems.Value
            .First()
            .Children!.OfType<ResourceTreeItemData>()
            .First(c => c.Value is FoldersTreeNode)
            .Children!.OfType<ResourceTreeItemData>()
            .First(f => f.Text == folderName);

    private static ResourceState StateWithFolderContainingSubs(
        string topicName, string[] subNames, string folderName, long folderId = 10L)
    {
        var subs = subNames.Select(n => ResourceTestData.CreateSubscription(n, topicName)).ToList();
        var topic = ResourceTestData.CreateTopic(topicName);
        var subEntries = subs.ToDictionary(
            s => s.Url,
            s => new SubscriptionEntry(new SubscriptionTreeNode(ConnectionName, s, Config), false));
        var topicEntry = new TopicEntry(new TopicTreeNode(ConnectionName, topic, Config), false, subEntries);
        var folderEntry = ResourceTestData.CreateFolderEntry(
            folderId, folderName, ConnectionName, resourceUrls: subs.Select(s => s.Url));
        var ns = ResourceTestData.CreateNamespaceEntry(
            connectionName: ConnectionName,
            topics: new Dictionary<string, TopicEntry> { [topic.Url] = topicEntry },
            folders: new Dictionary<long, FolderEntry> { [folderId] = folderEntry });
        return new ResourceState([ns], null, [$"ns:{ConnectionName}", $"folder:{folderId}"]);
    }
}
