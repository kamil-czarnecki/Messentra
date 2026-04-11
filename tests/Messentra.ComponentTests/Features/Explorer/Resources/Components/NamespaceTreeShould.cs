using Bunit;
using Messentra.Domain;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components;
using Messentra.Features.Settings.Connections.GetConnections;
using Messentra.Infrastructure.AzureServiceBus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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

        // Act
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

        // Act
        cut.Find(".mud-menu button").Click();
        MudPopover.Find(".mud-menu-item").Click();

        // Assert
        navigationManager.Uri.ShouldContain("/options");
    }

    [Fact]
    public void RefreshQueuesWhenSearchIsEmpty_DispatchesRefreshQueuesAction()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildRefreshableNamespaceTree(["queue1", "queue2"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        // Act
        cut.FindAll(".tree-item-body .mud-icon-button")[0].Click();

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshQueuesAction>()), Times.Once);
        MockDispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshQueueAction>()), Times.Never);
    }

    [Fact]
    public void RefreshQueuesWhenSearchIsActive_DispatchesOnlyFilteredQueueRefreshActions()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildRefreshableNamespaceTree(["queue1", "queue2"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        // Act
        cut.Find("input").Input("queue2");
        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("queue1"));
        cut.FindAll(".tree-item-body .mud-icon-button")[0].Click();

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshQueuesAction>()), Times.Never);
        MockDispatcher.Verify(
            d => d.Dispatch(It.Is<RefreshQueueAction>(a => a.Node.Resource.Name == "queue2")),
            Times.Once);
        MockDispatcher.Verify(
            d => d.Dispatch(It.Is<RefreshQueueAction>(a => a.Node.Resource.Name == "queue1")),
            Times.Never);
    }

    [Fact]
    public void RefreshQueuesWhenNamespaceFilterDoesNotNarrow_DispatchesRefreshQueuesAction()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildRefreshableNamespaceTree(["queue1", "queue2"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        // Act
        cut.Find("input").Input("namespace:TestNamespace");
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("queue1");
            cut.Markup.ShouldContain("queue2");
        });
        cut.FindAll(".tree-item-body .mud-icon-button")[0].Click();

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshQueuesAction>()), Times.Once);
        MockDispatcher.Verify(d => d.Dispatch(It.IsAny<RefreshQueueAction>()), Times.Never);
    }

    [Fact]
    public void ClickCancelLoadingOnNamespace_DispatchesCancelFetchResourcesAction()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildLoadingNamespaceTree())
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, ["ns:TestNamespace"]));

        // Act
        cut.Find(".namespace-loading-cancel").Click();

        // Assert
        MockDispatcher.Verify(
            d => d.Dispatch(It.Is<CancelFetchResourcesAction>(a => a.ConnectionName == "TestNamespace")),
            Times.Once);
    }

    [Fact]
    public void RightClickOnResourceRow_DispatchesSelectResourceAction()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildRefreshableNamespaceTree(["queue1"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        // Act
        var queueRow = cut.FindAll(".tree-item-body").First(x => x.TextContent.Contains("queue1"));
        queueRow.TriggerEvent("oncontextmenu", new MouseEventArgs { Button = 2 });

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(It.Is<SelectResourceAction>(a =>
            a.Node is QueueTreeNode &&
            ((QueueTreeNode)a.Node).Resource.Name == "queue1")), Times.Once);
    }

    [Fact]
    public async Task ExpandingSameTopicInRootAndFolder_DispatchesDifferentExpandedKeys()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTreeWithDuplicateTopicInRootAndFolder())
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, [
                "ns:TestNamespace",
                "topics:TestNamespace",
                "folders:TestNamespace",
                "folder:TestNamespace:10"
            ]));

        var topicWrappers = cut.FindComponents<ResourceTreeItemWrapper>()
            .Where(x => x.Instance.Presenter.Value is TopicTreeNode { Resource.Name: "worker-topic" })
            .ToList();

        topicWrappers.Count.ShouldBe(2);

        var rootTopic = topicWrappers.Single(x => x.Instance.Presenter.ParentFolderNode is null);
        var folderTopic = topicWrappers.Single(x => x.Instance.Presenter.ParentFolderNode is not null);

        // Act
        await cut.InvokeAsync(async () =>
        {
            await rootTopic.Instance.ExpandedChanged.InvokeAsync(true);
            await folderTopic.Instance.ExpandedChanged.InvokeAsync(true);
        });

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(It.IsAny<ToggleExpandedAction>()), Times.Exactly(2));
        MockDispatcher.Verify(d => d.Dispatch(It.Is<ToggleExpandedAction>(a =>
            a.NodeKey == "topic:TestNamespace:https://test/shared-topic" && a.Expanded)), Times.Once);
        MockDispatcher.Verify(d => d.Dispatch(It.Is<ToggleExpandedAction>(a =>
            a.NodeKey == "topic:TestNamespace:https://test/shared-topic|folder:TestNamespace:10" && a.Expanded)), Times.Once);
    }

    [Fact]
    public async Task SelectingKeywordSuggestion_ReopensAutocompleteDropdown()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildRefreshableNamespaceTree(["queue1"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        var autocomplete = cut.FindComponent<MudAutocomplete<string>>();

        // Act – simulate selecting a keyword suggestion (ValueChanged fires with "folders:")
        await cut.InvokeAsync(() => autocomplete.Instance.ValueChanged.InvokeAsync("folders:"));

        // Assert – autocomplete menu is open after the next render cycle
        cut.WaitForAssertion(() => autocomplete.Instance.Open.ShouldBeTrue());
    }

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

    private static ConnectionConfig BuildConnectionConfig() =>
        ConnectionConfig.CreateConnectionString(
            "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=key=");

    private static HashSet<string> DefaultExpandedKeys(string connectionName = "TestNamespace") =>
        [$"ns:{connectionName}", $"queues:{connectionName}", $"topics:{connectionName}"];

    private static Resource.Queue BuildQueueResource(string name)
    {
        return new Resource.Queue(name, $"https://test/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0), new SizeInfo(0, 1024)),
            new QueueProperties(TimeSpan.FromDays(14), TimeSpan.FromSeconds(60), TimeSpan.MaxValue,
                10, false, null, null, false, false, TimeSpan.FromMinutes(1), false, 256, string.Empty));
    }

    private static Resource.Topic BuildTopicResource(string name, string? url = null)
    {
        return new Resource.Topic(name, url ?? $"https://test/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0), new SizeInfo(0, 1024)),
            new TopicProperties(TimeSpan.FromDays(14), TimeSpan.MaxValue, false, false, TimeSpan.Zero, 256,
                string.Empty), []);
    }

    private static Resource.Subscription BuildSubscriptionResource(string name, string topicName, string? url = null)
    {
        return new Resource.Subscription(name, topicName, url ?? $"https://test/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0), new SizeInfo(0, 1024)),
            new SubscriptionProperties(TimeSpan.FromDays(14), TimeSpan.FromSeconds(60), TimeSpan.MaxValue,
                10, false, null, null, false, string.Empty));
    }

    private static List<ResourceTreeItemData> BuildNamespaceTreeWithDuplicateTopicInRootAndFolder()
    {
        var config = BuildConnectionConfig();
        const string connectionName = "TestNamespace";

        var rootTopicNode = new TopicTreeNode(
            connectionName,
            BuildTopicResource("worker-topic", "https://test/shared-topic"),
            config);
        var rootSubNode = new SubscriptionTreeNode(
            connectionName,
            BuildSubscriptionResource("worker-sub", "worker-topic", "https://test/shared-sub"),
            config);

        var folderNode = new FolderTreeNode(10L, 1L, "Team Folder", connectionName, config);
        var folderTopicNode = new TopicTreeNode(
            connectionName,
            BuildTopicResource("worker-topic", "https://test/shared-topic"),
            config);
        var folderSubNode = new SubscriptionTreeNode(
            connectionName,
            BuildSubscriptionResource("worker-sub", "worker-topic", "https://test/shared-sub"),
            config);

        var rootTopicItem = new ResourceTreeItemData
        {
            Text = "worker-topic",
            Value = rootTopicNode,
            Expandable = true,
            Children =
            [
                new ResourceTreeItemData
                {
                    Text = "worker-sub",
                    Value = rootSubNode
                }
            ]
        };

        var folderTopicItem = new ResourceTreeItemData
        {
            Text = "worker-topic",
            Value = folderTopicNode,
            ParentFolderNode = folderNode,
            Expandable = true,
            Children =
            [
                new ResourceTreeItemData
                {
                    Text = "worker-sub",
                    Value = folderSubNode,
                    ParentFolderNode = folderNode
                }
            ]
        };

        var foldersGroup = new ResourceTreeItemData
        {
            Text = "Folders",
            Value = new FoldersTreeNode(1L, connectionName, config),
            IsReadonly = true,
            Expandable = true,
            Children =
            [
                new ResourceTreeItemData
                {
                    Text = "Team Folder",
                    Value = folderNode,
                    IsReadonly = true,
                    Expandable = true,
                    Children = [folderTopicItem]
                }
            ]
        };

        var topicsGroup = new ResourceTreeItemData
        {
            Text = "Topics",
            Value = new TopicsTreeNode(connectionName, config),
            IsReadonly = true,
            Expandable = true,
            Children = [rootTopicItem]
        };

        return
        [
            new ResourceTreeItemData
            {
                Text = connectionName,
                Value = new NamespaceTreeNode(connectionName, config),
                IsReadonly = true,
                Expandable = true,
                Children = [foldersGroup, topicsGroup]
            }
        ];
    }

    private static List<ResourceTreeItemData> BuildRefreshableNamespaceTree(string[] queueNames)
    {
        var config = BuildConnectionConfig();
        const string connectionName = "TestNamespace";

        var queueItems = queueNames
            .Select(name =>
            {
                var queueNode = new QueueTreeNode(connectionName, BuildQueueResource(name), config);
                return new ResourceTreeItemData { Text = name, Value = queueNode };
            })
            .ToList<TreeItemData<ResourceTreeNode>>();

        var topicNode = new TopicTreeNode(connectionName, BuildTopicResource("topic1"), config);

        var queuesGroup = new ResourceTreeItemData
        {
            Text = "Queues",
            Value = new QueuesTreeNode(connectionName, config),
            IsReadonly = true,
            Expandable = true,
            Children = queueItems
        };

        var topicsGroup = new ResourceTreeItemData
        {
            Text = "Topics",
            Value = new TopicsTreeNode(connectionName, config),
            IsReadonly = true,
            Expandable = true,
            Children = new List<TreeItemData<ResourceTreeNode>>
            {
                new ResourceTreeItemData { Text = "topic1", Value = topicNode }
            }
        };

        return
        [
            new ResourceTreeItemData
            {
                Text = connectionName,
                Value = new NamespaceTreeNode(connectionName, config),
                IsReadonly = true,
                Expandable = true,
                Children = [queuesGroup, topicsGroup]
            }
        ];
    }

    private static List<ResourceTreeItemData> BuildLoadingNamespaceTree()
    {
        var config = BuildConnectionConfig();
        const string connectionName = "TestNamespace";

        return
        [
            new ResourceTreeItemData
            {
                Text = connectionName,
                Value = new NamespaceTreeNode(connectionName, config, IsLoading: true),
                IsReadonly = true,
                Expandable = true,
                Children = []
            }
        ];
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
            Children = subs.Count > 0 ? subs : null
        };
    }

    private static List<ResourceTreeItemData> BuildNamespaceTree(string[] queueNames, params ResourceTreeItemData[] topicItems)
    {
        var config = BuildConnectionConfig();
        const string connectionName = "TestNamespace";

        var queuesGroup = new ResourceTreeItemData
        {
            Text = "Queues",
            Value = new QueuesTreeNode(connectionName, config),
            IsReadonly = true,
            Expandable = true,
            Children = queueNames.Select(QueueItem).ToList<TreeItemData<ResourceTreeNode>>()
        };

        var topicsGroup = new ResourceTreeItemData
        {
            Text = "Topics",
            Value = new TopicsTreeNode(connectionName, config),
            IsReadonly = true,
            Expandable = true,
            Children = topicItems.ToList<TreeItemData<ResourceTreeNode>>()
        };

        return
        [
            new ResourceTreeItemData
            {
                Text = connectionName,
                Value = new NamespaceTreeNode(connectionName, config),
                IsReadonly = true,
                Expandable = true,
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
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

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
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

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
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

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
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

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
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

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
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

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
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

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
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

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
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

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
        var config = BuildConnectionConfig();
        var queuesGroup = new ResourceTreeItemData
        {
            Text = "Queues",
            Value = new QueuesTreeNode("TestNamespace", config),
            IsReadonly = true, Expandable = true,
            Children = new List<TreeItemData<ResourceTreeNode>>
            {
                QueueItemWithDlq("dlq-queue", dlq: 5),
                QueueItemWithDlq("clean-queue", dlq: 0)
            }
        };
        var resources = new List<ResourceTreeItemData>
        {
            new() {
                Text = "TestNamespace",
                Value = new NamespaceTreeNode("TestNamespace", config),
                IsReadonly = true, Expandable = true,
                Children = new List<TreeItemData<ResourceTreeNode>> { queuesGroup }
            }
        };
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, resources)
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        // Act
        cut.Find("input").Input("has:dlq");

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("dlq-queue");
            cut.Markup.ShouldNotContain("clean-queue");
        });
    }

    private static async Task<List<string>> GetSuggestions(IRenderedComponent<NamespaceTree> cut, string input)
    {
        var ac = cut.FindComponent<MudAutocomplete<string>>();
        ac.Instance.SearchFunc.ShouldNotBeNull();
        var results = await ac.Instance.SearchFunc(input, CancellationToken.None)!;
        return results.ToList();
    }

    [Fact]
    public async Task SuggestionSearch_EmptyInput_SuggestsBothKeywords()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        var suggestions = await GetSuggestions(cut, "");

        suggestions.ShouldContain("namespace:");
        suggestions.ShouldContain("has:dlq");
    }

    [Fact]
    public async Task SuggestionSearch_PartialN_SuggestsNamespaceKeyword()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        var suggestions = await GetSuggestions(cut, "n");

        suggestions.ShouldContain("namespace:");
        suggestions.ShouldNotContain("has:dlq");
    }

    [Fact]
    public async Task SuggestionSearch_PartialH_SuggestsHasDlqKeyword()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        var suggestions = await GetSuggestions(cut, "h");

        suggestions.ShouldContain("has:dlq");
        suggestions.ShouldNotContain("namespace:");
    }

    [Fact]
    public async Task SuggestionSearch_NamespaceColon_SuggestsConnectedNamespaceNames()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        var suggestions = await GetSuggestions(cut, "namespace:");

        suggestions.ShouldContain("namespace:TestNamespace");
    }

    [Fact]
    public async Task SuggestionSearch_NamespaceColonPartial_FiltersNamespacesByPartialName()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        var suggestions = await GetSuggestions(cut, "namespace:Test");

        suggestions.ShouldContain("namespace:TestNamespace");
    }

    [Fact]
    public async Task SuggestionSearch_NamespaceColonNoMatch_ReturnsEmpty()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        var suggestions = await GetSuggestions(cut, "namespace:zzznomatch");

        suggestions.ShouldBeEmpty();
    }

    [Fact]
    public async Task SuggestionSearch_CompleteToken_DoesNotSuggestItself()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        var suggestions = await GetSuggestions(cut, "has:dlq");

        suggestions.ShouldNotContain("has:dlq");
    }

    [Fact]
    public async Task SuggestionSearch_TrailingSpace_SuggestsForNextToken()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        var suggestions = await GetSuggestions(cut, "queue1 ");

        suggestions.ShouldContain("queue1 namespace:");
        suggestions.ShouldContain("queue1 has:dlq");
    }

    [Fact]
    public async Task SuggestionSearch_PartialSecondToken_PreservesCompletedPrefix()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        var suggestions = await GetSuggestions(cut, "queue1 h");

        suggestions.ShouldContain("queue1 has:dlq");
        suggestions.ShouldNotContain("queue1 namespace:");
    }

    [Fact]
    public async Task SuggestionSearch_IsCaseInsensitiveForKeywords()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));

        var suggestionsUpper = await GetSuggestions(cut, "HAS");
        var suggestionsNamespace = await GetSuggestions(cut, "NAMESPACE:");

        suggestionsUpper.ShouldContain("has:dlq");
        suggestionsNamespace.ShouldContain("namespace:TestNamespace");
    }

    [Fact]
    public async Task OnSuggestionSelected_DispatchesSetSearchPhraseAction()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys()));
        var ac = cut.FindComponent<MudAutocomplete<string>>();

        await cut.InvokeAsync(() => ac.Instance.ValueChanged.InvokeAsync("has:dlq"));

        MockDispatcher.Verify(
            d => d.Dispatch(It.Is<SetSearchPhraseAction>(a => a.Phrase == "has:dlq")),
            Times.Once);
    }


    private static ResourceTreeItemData BuildFoldersGroup(
        string connectionName = "Test Namespace",
        ConnectionConfig? config = null,
        List<ResourceTreeItemData>? folders = null)
    {
        config ??= ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "t", "c");
        return new ResourceTreeItemData
        {
            Text = "Folders",
            Value = new FoldersTreeNode(1L, connectionName, config),
            Children = folders?.Cast<TreeItemData<ResourceTreeNode>>().ToList()
        };
    }

    [Fact]
    public void ShowFoldersSectionWhenResourcesConnected()
    {
        // Arrange
        var foldersGroup = BuildFoldersGroup();
        var nsItem = new ResourceTreeItemData
        {
            Text = "Test Namespace",
            Value = new NamespaceTreeNode("Test Namespace",
                ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "t", "c")),
            Children = [foldersGroup]
        };

        // Act
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, [nsItem])
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, ["ns:Test Namespace", "folders:Test Namespace"]));

        // Assert
        cut.Markup.ShouldContain("Folders");
    }

    [Fact]
    public async Task DispatchCreateFolderActionWhenPlusButtonClickedAndDialogConfirmed()
    {
        // Arrange
        var config = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "t", "c");
        var foldersGroup = BuildFoldersGroup(config: config);
        var nsItem = new ResourceTreeItemData
        {
            Text = "Test Namespace",
            Value = new NamespaceTreeNode("Test Namespace", config),
            IsReadonly = true,
            Expandable = true,
            Children = new List<TreeItemData<ResourceTreeNode>> { foldersGroup }
        };

        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, [nsItem])
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, ["ns:Test Namespace", "folders:Test Namespace"]));

        // Act
        await cut.InvokeAsync(() => cut.Find(".folders-add-btn").Click());

        await MudDialog.WaitForAssertionAsync(() => MudDialog.Find(".folder-name-input input").ShouldNotBeNull());
        await MudDialog.Find(".folder-name-input input").InputAsync("My Team");
        await MudDialog.Find(".folder-create-confirm").ClickAsync();

        // Assert
        MockDispatcher.Verify(x => x.Dispatch(It.Is<CreateFolderAction>(a =>
            a.ConnectionId == 1L &&
            a.ConnectionName == "Test Namespace" &&
            a.ConnectionConfig == config &&
            a.Name == "My Team")), Times.Once);
    }

    [Fact]
    public async Task DoNotDispatchCreateFolderActionWhenDuplicateFolderNameEntered()
    {
        // Arrange
        var config = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "t", "c");
        var existingFolder = new ResourceTreeItemData
        {
            Text = "My Team",
            Value = new FolderTreeNode(10L, 1L, "My Team", "Test Namespace", config),
            IsReadonly = true,
            Expandable = false
        };
        var foldersGroup = BuildFoldersGroup(config: config, folders: [existingFolder]);
        var nsItem = new ResourceTreeItemData
        {
            Text = "Test Namespace",
            Value = new NamespaceTreeNode("Test Namespace", config),
            IsReadonly = true,
            Expandable = true,
            Children = new List<TreeItemData<ResourceTreeNode>> { foldersGroup }
        };

        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, [nsItem])
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, ["ns:Test Namespace", "folders:Test Namespace"]));

        // Act
        await cut.InvokeAsync(() => cut.Find(".folders-add-btn").Click());
        await MudDialog.WaitForAssertionAsync(() => MudDialog.Find(".folder-name-input input").ShouldNotBeNull());
        await MudDialog.Find(".folder-name-input input").InputAsync(" my team ");

        // Assert
        await MudDialog.WaitForAssertionAsync(() =>
            MudDialog.Find(".folder-create-confirm").HasAttribute("disabled").ShouldBeTrue());
        MockDispatcher.Verify(x => x.Dispatch(It.IsAny<CreateFolderAction>()), Times.Never);
    }

    [Fact]
    public void OnParametersSet_WhenSearchPhraseChangesExternally_SyncsLocalPhrase()
    {
        // Arrange — render with an initial phrase
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1", "queue2"]))
            .Add(x => x.Connections, [])
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys())
            .Add(x => x.SearchPhrase, "queue1"));

        cut.Markup.ShouldNotContain("queue2");

        // Act
        cut.Render(p => p
            .Add(x => x.ExpandedKeys, DefaultExpandedKeys())
            .Add(x => x.SearchPhrase, null));

        // Assert
        cut.Markup.ShouldContain("queue1");
        cut.Markup.ShouldContain("queue2");
    }
}
