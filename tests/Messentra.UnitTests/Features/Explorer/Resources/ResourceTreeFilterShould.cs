using Messentra.Domain;
using Messentra.Features.Explorer.Resources;
using Messentra.Infrastructure.AzureServiceBus;
using MudBlazor;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Resources;

public sealed class ResourceTreeFilterShould
{
    private static readonly ConnectionConfig Config = ResourceTestData.CreateConnectionConfig();

    private static ResourceTreeItemData QueueItem(string name, long dlq = 0)
    {
        var queue = new Resource.Queue(name, $"https://test/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, dlq, 0, 0, 0, dlq), new SizeInfo(0, 1024)),
            new QueueProperties(TimeSpan.FromDays(14), TimeSpan.FromSeconds(60), TimeSpan.MaxValue,
                10, false, null, null, false, false, TimeSpan.FromMinutes(1), false, 256, string.Empty));
        return new ResourceTreeItemData { Text = name, Value = new QueueTreeNode("ns", queue, Config) };
    }

    private static ResourceTreeItemData SubscriptionItem(string name, long dlq = 0)
    {
        var sub = new Resource.Subscription(name, "topic", $"https://test/topic/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, dlq, 0, 0, 0, dlq), new SizeInfo(0, 1024)),
            new SubscriptionProperties(TimeSpan.FromDays(14), TimeSpan.FromSeconds(60),
                TimeSpan.MaxValue, 10, false, null, null, false, string.Empty));
        return new ResourceTreeItemData { Text = name, Value = new SubscriptionTreeNode("ns", sub, Config) };
    }

    private static ResourceTreeItemData TopicWithSubs(string name, params ResourceTreeItemData[] subs) =>
        new() { Text = name, Expandable = true, Expanded = true, Children = subs.ToList<TreeItemData<ResourceTreeNode>>() };

    private static ResourceTreeItemData QueuesGroup(params ResourceTreeItemData[] queues) =>
        new() { Text = "Queues", IsReadonly = true, Expandable = true, Expanded = true, Children = queues.ToList<TreeItemData<ResourceTreeNode>>() };

    private static ResourceTreeItemData TopicsGroup(params ResourceTreeItemData[] topics) =>
        new() { Text = "Topics", IsReadonly = true, Expandable = true, Expanded = true, Children = topics.ToList<TreeItemData<ResourceTreeNode>>() };

    private static List<ResourceTreeItemData> Ns(string name, params ResourceTreeItemData[] groups) =>
        [new() { Text = name, IsReadonly = true, Expandable = true, Expanded = true, Children = groups.ToList<TreeItemData<ResourceTreeNode>>() }];

    // --- Empty query ---

    [Fact]
    public void ReturnOriginalListForEmptyQuery()
    {
        var resources = Ns("ns1", QueuesGroup(QueueItem("q1")));
        ResourceTreeFilter.Filter(resources, SearchQuery.Empty).ShouldBeSameAs(resources);
    }

    // --- name phrase ---

    [Fact]
    public void NamePhrase_ShowsOnlyMatchingQueue()
    {
        var resources = Ns("ns1", QueuesGroup(QueueItem("alpha"), QueueItem("beta")));
        var result = ResourceTreeFilter.Filter(resources, SearchQueryParser.Parse("alpha"));
        var queues = result[0].Children!.OfType<ResourceTreeItemData>().First().Children!
            .OfType<ResourceTreeItemData>().ToList();
        queues.Count.ShouldBe(1);
        queues[0].Text.ShouldBe("alpha");
    }

    [Fact]
    public void NamePhrase_HidesNonMatchingQueues()
    {
        var resources = Ns("ns1", QueuesGroup(QueueItem("alpha")));
        var result = ResourceTreeFilter.Filter(resources, SearchQueryParser.Parse("beta"));
        result.ShouldBeEmpty();
    }

    // --- namespace: ---

    [Fact]
    public void NamespaceFilter_ShowsOnlyMatchingNamespace()
    {
        var prod = Ns("prod-ns", QueuesGroup(QueueItem("q1")))[0];
        var dev  = Ns("dev-ns",  QueuesGroup(QueueItem("q2")))[0];
        var result = ResourceTreeFilter.Filter([prod, dev], SearchQueryParser.Parse("namespace:prod"));
        result.Count.ShouldBe(1);
        result[0].Text.ShouldBe("prod-ns");
    }

    [Fact]
    public void NamespaceFilter_HidesNonMatchingNamespace()
    {
        var resources = Ns("dev-ns", QueuesGroup(QueueItem("q1")));
        ResourceTreeFilter.Filter(resources, SearchQueryParser.Parse("namespace:prod")).ShouldBeEmpty();
    }

    [Fact]
    public void NamespaceFilter_ShowsAllChildrenWithNoOtherConditions()
    {
        var resources = Ns("prod-ns", QueuesGroup(QueueItem("q1"), QueueItem("q2")));
        var result = ResourceTreeFilter.Filter(resources, SearchQueryParser.Parse("namespace:prod"));
        var queues = result[0].Children!.OfType<ResourceTreeItemData>().First().Children!
            .OfType<ResourceTreeItemData>().ToList();
        queues.Count.ShouldBe(2);
    }

    [Fact]
    public void NamespaceAndNamePhrase_ShowsMatchingQueuesInMatchingNamespace()
    {
        var prod = Ns("prod-ns", QueuesGroup(QueueItem("alpha"), QueueItem("beta")))[0];
        var dev  = Ns("dev-ns",  QueuesGroup(QueueItem("alpha")))[0];
        var result = ResourceTreeFilter.Filter([prod, dev], SearchQueryParser.Parse("namespace:prod alpha"));
        result.Count.ShouldBe(1);
        var queues = result[0].Children!.OfType<ResourceTreeItemData>().First().Children!
            .OfType<ResourceTreeItemData>().ToList();
        queues.Count.ShouldBe(1);
        queues[0].Text.ShouldBe("alpha");
    }

    // --- has:dlq ---

    [Fact]
    public void HasDlq_ShowsOnlyQueuesWithDlqMessages()
    {
        var resources = Ns("ns1", QueuesGroup(QueueItem("q1", dlq: 0), QueueItem("q2", dlq: 5)));
        var result = ResourceTreeFilter.Filter(resources, SearchQueryParser.Parse("has:dlq"));
        var queues = result[0].Children!.OfType<ResourceTreeItemData>().First().Children!
            .OfType<ResourceTreeItemData>().ToList();
        queues.Count.ShouldBe(1);
        queues[0].Text.ShouldBe("q2");
    }

    [Fact]
    public void HasDlq_HidesNamespaceWhenNoQueuesHaveDlq()
    {
        var resources = Ns("ns1", QueuesGroup(QueueItem("q1", dlq: 0)));
        ResourceTreeFilter.Filter(resources, SearchQueryParser.Parse("has:dlq")).ShouldBeEmpty();
    }

    [Fact]
    public void HasDlq_ShowsOnlySubscriptionsWithDlqMessages()
    {
        var resources = Ns("ns1", TopicsGroup(
            TopicWithSubs("topic1",
                SubscriptionItem("sub1", dlq: 0),
                SubscriptionItem("sub2", dlq: 3))));
        var result = ResourceTreeFilter.Filter(resources, SearchQueryParser.Parse("has:dlq"));
        var topic = result[0].Children!.OfType<ResourceTreeItemData>().First().Children!
            .OfType<ResourceTreeItemData>().Single();
        topic.Text.ShouldBe("topic1");
        var subs = topic.Children!.OfType<ResourceTreeItemData>().ToList();
        subs.Count.ShouldBe(1);
        subs[0].Text.ShouldBe("sub2");
    }

    [Fact]
    public void HasDlq_HidesTopicWhenNoSubscriptionsHaveDlq()
    {
        var resources = Ns("ns1", TopicsGroup(
            TopicWithSubs("topic1", SubscriptionItem("sub1", dlq: 0))));
        ResourceTreeFilter.Filter(resources, SearchQueryParser.Parse("has:dlq")).ShouldBeEmpty();
    }

    [Fact]
    public void HasDlq_WhenTopicNameMatchesShowsOnlyDlqSubscriptions()
    {
        var resources = Ns("ns1", TopicsGroup(
            TopicWithSubs("topic1",
                SubscriptionItem("sub1", dlq: 2),
                SubscriptionItem("sub2", dlq: 0))));
        var result = ResourceTreeFilter.Filter(resources, SearchQueryParser.Parse("topic1 has:dlq"));
        var subs = result[0].Children!.OfType<ResourceTreeItemData>().First().Children!
            .OfType<ResourceTreeItemData>().Single().Children!
            .OfType<ResourceTreeItemData>().ToList();
        subs.Count.ShouldBe(1);
        subs[0].Text.ShouldBe("sub1");
    }

    // --- combined ---

    [Fact]
    public void NamePhraseAndHasDlq_ShowsOnlyMatchingQueueWithDlq()
    {
        var resources = Ns("ns1", QueuesGroup(
            QueueItem("alpha", dlq: 5),
            QueueItem("alpha-clean", dlq: 0),
            QueueItem("beta", dlq: 5)));
        var result = ResourceTreeFilter.Filter(resources, SearchQueryParser.Parse("alpha has:dlq"));
        var queues = result[0].Children!.OfType<ResourceTreeItemData>().First().Children!
            .OfType<ResourceTreeItemData>().ToList();
        queues.Count.ShouldBe(1);
        queues[0].Text.ShouldBe("alpha");
    }

    // --- folder nodes ---

    [Fact]
    public void KeepFolderGroupWhenResourceNameMatchesPhrase()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("orders-processing");
        var queueNode = new QueueTreeNode("test", queue, config);
        var queueItem = new ResourceTreeItemData { Text = "orders-processing", Value = queueNode };

        var folderNode = new FolderTreeNode(1L, 1L, "My Team", "test", config);
        var folderItem = new ResourceTreeItemData
        {
            Text = "My Team",
            Value = folderNode,
            Children = [queueItem]
        };

        var foldersGroupItem = new ResourceTreeItemData
        {
            Text = "Folders",
            Value = new FoldersTreeNode(1L, "test", config),
            Children = [folderItem]
        };

        var nsItem = new ResourceTreeItemData
        {
            Text = "test-ns",
            Value = new NamespaceTreeNode("test", config),
            Children = [foldersGroupItem]
        };

        // Act
        var result = ResourceTreeFilter.Filter([nsItem], SearchQueryParser.Parse("orders"));

        // Assert
        result.ShouldHaveSingleItem();
        var foldersGroup = result[0].Children!.OfType<ResourceTreeItemData>().First();
        foldersGroup.Value.ShouldBeOfType<FoldersTreeNode>();
        foldersGroup.Children.ShouldNotBeNull();
    }

    [Fact]
    public void HideFolderGroupWhenNoResourcesMatch()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var queue = ResourceTestData.CreateQueue("invoices");
        var queueNode = new QueueTreeNode("test", queue, config);
        var queueItem = new ResourceTreeItemData { Text = "invoices", Value = queueNode };

        var folderNode = new FolderTreeNode(1L, 1L, "My Team", "test", config);
        var folderItem = new ResourceTreeItemData
        {
            Text = "My Team",
            Value = folderNode,
            Children = [queueItem]
        };

        var foldersGroupItem = new ResourceTreeItemData
        {
            Text = "Folders",
            Value = new FoldersTreeNode(1L, "test", config),
            Children = [folderItem]
        };

        var nsItem = new ResourceTreeItemData
        {
            Text = "test-ns",
            Value = new NamespaceTreeNode("test", config),
            Children = [foldersGroupItem]
        };

        // Act
        var result = ResourceTreeFilter.Filter([nsItem], SearchQueryParser.Parse("orders"));

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void KeepFolderGroupWhenSubscriptionNameMatchesPhraseUnderDerivedTopicHeader()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var sub = ResourceTestData.CreateSubscription("orders-sub", "orders-topic");
        var subItem = new ResourceTreeItemData { Text = "orders-sub", Value = new SubscriptionTreeNode("test", sub, config) };
        var topic = ResourceTestData.CreateTopic("orders-topic");
        var derivedTopicHeader = new ResourceTreeItemData
        {
            Text = "orders-topic",
            Value = new TopicTreeNode("test", topic, config),
            Children = [subItem]
        };
        var folderItem = new ResourceTreeItemData
        {
            Text = "My Team",
            Value = new FolderTreeNode(1L, 1L, "My Team", "test", config),
            Children = [derivedTopicHeader]
        };
        var foldersGroupItem = new ResourceTreeItemData
        {
            Text = "Folders",
            Value = new FoldersTreeNode(1L, "test", config),
            Children = [folderItem]
        };
        var nsItem = new ResourceTreeItemData
        {
            Text = "test-ns",
            Value = new NamespaceTreeNode("test", config),
            Children = [foldersGroupItem]
        };

        // Act
        var result = ResourceTreeFilter.Filter([nsItem], SearchQueryParser.Parse("orders-sub"));

        // Assert
        result.ShouldHaveSingleItem();
        var folder = result[0].Children!.OfType<ResourceTreeItemData>().Single()
            .Children!.OfType<ResourceTreeItemData>().Single();
        folder.Value.ShouldBeOfType<FolderTreeNode>();
        var topicHeader = folder.Children!.OfType<ResourceTreeItemData>().Single();
        topicHeader.Value.ShouldBeOfType<TopicTreeNode>();
        topicHeader.Children!.OfType<ResourceTreeItemData>().Single().Text.ShouldBe("orders-sub");
    }

    [Fact]
    public void HideFolderGroupWhenSubscriptionNameDoesNotMatchPhrase()
    {
        // Arrange
        var config = ResourceTestData.CreateConnectionConfig();
        var sub = ResourceTestData.CreateSubscription("orders-sub", "orders-topic");
        var subItem = new ResourceTreeItemData { Text = "orders-sub", Value = new SubscriptionTreeNode("test", sub, config) };
        var topic = ResourceTestData.CreateTopic("orders-topic");
        var derivedTopicHeader = new ResourceTreeItemData
        {
            Text = "orders-topic",
            Value = new TopicTreeNode("test", topic, config),
            Children = [subItem]
        };
        var folderItem = new ResourceTreeItemData
        {
            Text = "My Team",
            Value = new FolderTreeNode(1L, 1L, "My Team", "test", config),
            Children = [derivedTopicHeader]
        };
        var foldersGroupItem = new ResourceTreeItemData
        {
            Text = "Folders",
            Value = new FoldersTreeNode(1L, "test", config),
            Children = [folderItem]
        };
        var nsItem = new ResourceTreeItemData
        {
            Text = "test-ns",
            Value = new NamespaceTreeNode("test", config),
            Children = [foldersGroupItem]
        };

        // Act
        var result = ResourceTreeFilter.Filter([nsItem], SearchQueryParser.Parse("invoices"));

        // Assert
        result.ShouldBeEmpty();
    }

    // --- HasDlqMessages helper ---

    [Fact]
    public void HasDlqMessages_ReturnsTrueForQueueWithDlq()
    {
        ResourceTreeFilter.HasDlqMessages(QueueItem("q", dlq: 1).Value).ShouldBeTrue();
    }

    [Fact]
    public void HasDlqMessages_ReturnsFalseForQueueWithoutDlq()
    {
        ResourceTreeFilter.HasDlqMessages(QueueItem("q", dlq: 0).Value).ShouldBeFalse();
    }

    [Fact]
    public void HasDlqMessages_ReturnsFalseForNull()
    {
        ResourceTreeFilter.HasDlqMessages(null).ShouldBeFalse();
    }
}

