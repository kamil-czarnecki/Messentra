using Bunit;
using Messentra.Domain;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components;
using Messentra.Features.Settings.Connections.GetConnections;
using Messentra.Infrastructure.AzureServiceBus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components;

public sealed class NamespaceTreeShould : ComponentTestBase
{
    private static ConnectionDto BuildEntraIdConnection(string name = "Test Namespace") =>
        new(
            Id: 1,
            Name: name,
            ConnectionConfig: new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                "test.servicebus.windows.net",
                "tenant-id",
                "client-id"));

    [Fact]
    public void ShowEmptyStateWhenNoResourcesConnected()
    {
        // Arrange & Act
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, [])
            .Add(x => x.Connections, []));

        // Assert
        cut.Markup.ShouldContain("No connection selected");
    }

    [Fact]
    public void ShowSavedConnectionsInMenu()
    {
        // Arrange
        var connection = BuildEntraIdConnection();
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, [])
            .Add(x => x.Connections, [connection]));

        // Act - open menu to render popover content
        cut.Find(".mud-menu button").Click();

        // Assert
        MudPopover.Markup.ShouldContain(connection.Name);
    }

    [Fact]
    public void DispatchFetchResourcesActionWhenConnectionSelected()
    {
        // Arrange
        var connection = BuildEntraIdConnection();
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, [])
            .Add(x => x.Connections, [connection]));

        // Act - open menu then click the connection item
        cut.Find(".mud-menu button").Click();
        MudPopover.Find(".mud-menu-item:last-child").Click();

        // Assert
        MockDispatcher.Verify(x => x.Dispatch(It.IsAny<FetchResourcesAction>()), Times.Once);
    }

    [Fact]
    public void NavigateToOptionsWhenAddConnectionClicked()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, [])
            .Add(x => x.Connections, []));

        // Act - open menu then click "Add connection"
        cut.Find(".mud-menu button").Click();
        MudPopover.Find(".mud-menu-item").Click();

        // Assert
        navigationManager.Uri.ShouldContain("/options");
    }

    // --- Filtering ---

    private static ResourceTreeItemData QueueItem(string name) => new() { Text = name };

    private static ResourceTreeItemData QueueItemWithDlq(string name, long dlq)
    {
        var config = ConnectionConfig.CreateConnectionString(
            "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=key=");
        var queue = new Resource.Queue(name, $"https://test/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, dlq, 0, 0, 0, dlq), new SizeInfo(0, 1024)),
            new QueueProperties(TimeSpan.FromDays(14), TimeSpan.FromSeconds(60), TimeSpan.MaxValue,
                10, false, null, null, false, false, TimeSpan.FromMinutes(1), false, 256, string.Empty));
        return new ResourceTreeItemData { Text = name, Value = new QueueTreeNode("TestNS", queue, config) };
    }

    private static ResourceTreeItemData TopicItem(string name, params string[] subscriptionNames)
    {
        var subs = subscriptionNames
            .Select(s => new ResourceTreeItemData { Text = s })
            .ToList<TreeItemData<ResourceTreeNode>>();

        return new ResourceTreeItemData
        {
            Text = name,
            Expandable = subs.Count > 0,
            Expanded = true,
            Children = subs.Count > 0 ? subs : null
        };
    }

    private static List<ResourceTreeItemData> BuildNamespaceTree(string[] queueNames, params ResourceTreeItemData[] topicItems)
    {
        var queuesGroup = new ResourceTreeItemData
        {
            Text = "Queues",
            IsReadonly = true,
            Expandable = true,
            Expanded = true,
            Children = queueNames.Select(QueueItem).ToList<TreeItemData<ResourceTreeNode>>()
        };

        var topicsGroup = new ResourceTreeItemData
        {
            Text = "Topics",
            IsReadonly = true,
            Expandable = true,
            Expanded = true,
            Children = topicItems.ToList<TreeItemData<ResourceTreeNode>>()
        };

        return
        [
            new ResourceTreeItemData
            {
                Text = "TestNamespace",
                IsReadonly = true,
                Expandable = true,
                Expanded = true,
                Children = new[] { queuesGroup, topicsGroup }.ToList<TreeItemData<ResourceTreeNode>>()
            }
        ];
    }

    [Fact]
    public void ShowsAllItemsWhenNoSearchPhraseEntered()
    {
        // Arrange & Act
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1", "queue2"]))
            .Add(x => x.Connections, []));

        // Assert
        cut.Markup.ShouldContain("queue1");
        cut.Markup.ShouldContain("queue2");
    }

    [Fact]
    public void ShowsOnlyMatchingQueueWhenFiltered()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1", "queue2"]))
            .Add(x => x.Connections, []));

        // Act
        cut.Find("input").Input("queue2");

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("queue2");
            cut.Markup.ShouldNotContain("queue1");
        });
    }

    [Fact]
    public void HidesAllItemsWhenSearchMatchesNothing()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1", "queue2"]))
            .Add(x => x.Connections, []));

        // Act
        cut.Find("input").Input("xyznomatch");

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldNotContain("queue1");
            cut.Markup.ShouldNotContain("queue2");
        });
    }

    [Fact]
    public void FilteringIsCaseInsensitive()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1", "queue2"]))
            .Add(x => x.Connections, []));

        // Act
        cut.Find("input").Input("QUEUE2");

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("queue2");
            cut.Markup.ShouldNotContain("queue1");
        });
    }

    [Fact]
    public void ShowsAllItemsAfterSearchIsCleared()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1", "queue2"]))
            .Add(x => x.Connections, []));

        // Act - filter then clear
        cut.Find("input").Input("queue2");
        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("queue1"));

        cut.Find("input").Input("");

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("queue1");
            cut.Markup.ShouldContain("queue2");
        });
    }

    [Fact]
    public void ShowsAllSubscriptionsWhenTopicNameMatchesSearch()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree([], TopicItem("topic1", "sub1", "sub2")))
            .Add(x => x.Connections, []));

        // Act
        cut.Find("input").Input("topic1");

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("topic1");
            cut.Markup.ShouldContain("sub1");
            cut.Markup.ShouldContain("sub2");
        });
    }

    [Fact]
    public void ShowsOnlyMatchingSubscriptionWhenFiltered()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree([], TopicItem("topic1", "sub1", "sub2")))
            .Add(x => x.Connections, []));

        // Act
        cut.Find("input").Input("sub1");

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("topic1");
            cut.Markup.ShouldContain("sub1");
            cut.Markup.ShouldNotContain("sub2");
        });
    }

    [Fact]
    public void NamespaceFilter_ShowsQueuesBelongingToMatchingNamespace()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1", "queue2"]))
            .Add(x => x.Connections, []));

        // Act
        cut.Find("input").Input("namespace:TestNamespace");

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("queue1");
            cut.Markup.ShouldContain("queue2");
        });
    }

    [Fact]
    public void NamespaceFilter_HidesQueuesBelongingToNonMatchingNamespace()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1", "queue2"]))
            .Add(x => x.Connections, []));

        // Act
        cut.Find("input").Input("namespace:production");

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldNotContain("queue1");
            cut.Markup.ShouldNotContain("queue2");
        });
    }

    [Fact]
    public void HasDlqFilter_ShowsOnlyQueuesWithDlqMessages()
    {
        // Arrange
        var queuesGroup = new ResourceTreeItemData
        {
            Text = "Queues", IsReadonly = true, Expandable = true, Expanded = true,
            Children = new List<TreeItemData<ResourceTreeNode>>
            {
                QueueItemWithDlq("dlq-queue", dlq: 5),
                QueueItemWithDlq("clean-queue", dlq: 0)
            }
        };
        var resources = new List<ResourceTreeItemData>
        {
            new() { Text = "TestNamespace", IsReadonly = true, Expandable = true, Expanded = true,
                Children = new List<TreeItemData<ResourceTreeNode>> { queuesGroup } }
        };
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, resources)
            .Add(x => x.Connections, []));

        // Act
        cut.Find("input").Input("has:dlq");

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("dlq-queue");
            cut.Markup.ShouldNotContain("clean-queue");
        });
    }
}
