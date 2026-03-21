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

    [Fact]
    public void RefreshQueuesWhenSearchIsEmpty_DispatchesRefreshQueuesAction()
    {
        // Arrange
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildRefreshableNamespaceTree(["queue1", "queue2"]))
            .Add(x => x.Connections, []));

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
            .Add(x => x.Connections, []));

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
            .Add(x => x.Connections, []));

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
            .Add(x => x.Connections, []));

        // Act
        cut.Find(".namespace-loading-cancel").Click();

        // Assert
        MockDispatcher.Verify(
            d => d.Dispatch(It.Is<CancelFetchResourcesAction>(a => a.ConnectionName == "TestNamespace")),
            Times.Once);
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

    private static ConnectionConfig BuildConnectionConfig() =>
        ConnectionConfig.CreateConnectionString(
            "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=key=");

    private static Resource.Queue BuildQueueResource(string name)
    {
        return new Resource.Queue(name, $"https://test/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0), new SizeInfo(0, 1024)),
            new QueueProperties(TimeSpan.FromDays(14), TimeSpan.FromSeconds(60), TimeSpan.MaxValue,
                10, false, null, null, false, false, TimeSpan.FromMinutes(1), false, 256, string.Empty));
    }

    private static Resource.Topic BuildTopicResource(string name)
    {
        return new Resource.Topic(name, $"https://test/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0), new SizeInfo(0, 1024)),
            new TopicProperties(TimeSpan.FromDays(14), TimeSpan.MaxValue, false, false, TimeSpan.Zero, 256,
                string.Empty), []);
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
            Expanded = true,
            Children = queueItems
        };

        var topicsGroup = new ResourceTreeItemData
        {
            Text = "Topics",
            Value = new TopicsTreeNode(connectionName, config),
            IsReadonly = true,
            Expandable = true,
            Expanded = true,
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
                Expanded = true,
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
                Expanded = true,
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

    // --- Autocomplete suggestions ---

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
            .Add(x => x.Connections, []));

        var suggestions = await GetSuggestions(cut, "");

        suggestions.ShouldContain("namespace:");
        suggestions.ShouldContain("has:dlq");
    }

    [Fact]
    public async Task SuggestionSearch_PartialN_SuggestsNamespaceKeyword()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, []));

        var suggestions = await GetSuggestions(cut, "n");

        suggestions.ShouldContain("namespace:");
        suggestions.ShouldNotContain("has:dlq");
    }

    [Fact]
    public async Task SuggestionSearch_PartialH_SuggestsHasDlqKeyword()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, []));

        var suggestions = await GetSuggestions(cut, "h");

        suggestions.ShouldContain("has:dlq");
        suggestions.ShouldNotContain("namespace:");
    }

    [Fact]
    public async Task SuggestionSearch_NamespaceColon_SuggestsConnectedNamespaceNames()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, []));

        var suggestions = await GetSuggestions(cut, "namespace:");

        suggestions.ShouldContain("namespace:TestNamespace");
    }

    [Fact]
    public async Task SuggestionSearch_NamespaceColonPartial_FiltersNamespacesByPartialName()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, []));

        var suggestions = await GetSuggestions(cut, "namespace:Test");

        suggestions.ShouldContain("namespace:TestNamespace");
    }

    [Fact]
    public async Task SuggestionSearch_NamespaceColonNoMatch_ReturnsEmpty()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, []));

        var suggestions = await GetSuggestions(cut, "namespace:zzznomatch");

        suggestions.ShouldBeEmpty();
    }

    [Fact]
    public async Task SuggestionSearch_CompleteToken_DoesNotSuggestItself()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, []));

        var suggestions = await GetSuggestions(cut, "has:dlq");

        suggestions.ShouldNotContain("has:dlq");
    }

    [Fact]
    public async Task SuggestionSearch_TrailingSpace_SuggestsForNextToken()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, []));

        var suggestions = await GetSuggestions(cut, "queue1 ");

        suggestions.ShouldContain("queue1 namespace:");
        suggestions.ShouldContain("queue1 has:dlq");
    }

    [Fact]
    public async Task SuggestionSearch_PartialSecondToken_PreservesCompletedPrefix()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, []));

        var suggestions = await GetSuggestions(cut, "queue1 h");

        suggestions.ShouldContain("queue1 has:dlq");
        suggestions.ShouldNotContain("queue1 namespace:");
    }

    [Fact]
    public async Task SuggestionSearch_IsCaseInsensitiveForKeywords()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, []));

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
            .Add(x => x.Connections, []));
        var ac = cut.FindComponent<MudAutocomplete<string>>();

        await cut.InvokeAsync(() => ac.Instance.ValueChanged.InvokeAsync("has:dlq"));

        MockDispatcher.Verify(
            d => d.Dispatch(It.Is<SetSearchPhraseAction>(a => a.Phrase == "has:dlq")),
            Times.Once);
    }

    [Fact]
    public async Task SuggestionSearch_CompleteNamespaceToken_DoesNotSuggestItself()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, []));

        var suggestions = await GetSuggestions(cut, "namespace:TestNamespace");

        suggestions.ShouldNotContain("namespace:TestNamespace");
    }

    [Fact]
    public async Task SuggestionSearch_CompleteNamespaceTokenDifferentCase_DoesNotSuggestItself()
    {
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1"]))
            .Add(x => x.Connections, []));

        var suggestions = await GetSuggestions(cut, "NAMESPACE:TESTNAMESPACE");

        suggestions.ShouldNotContain("namespace:TestNamespace");
    }

    [Fact]
    public void OnParametersSet_WhenSearchPhraseChangesExternally_SyncsLocalPhrase()
    {
        // Arrange — render with an initial phrase
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, BuildNamespaceTree(["queue1", "queue2"]))
            .Add(x => x.Connections, [])
            .Add(x => x.SearchPhrase, "queue1"));

        cut.Markup.ShouldNotContain("queue2");

        // Act — simulate an external state reset by re-rendering with a null phrase
        cut.Render(p => p
            .Add(x => x.SearchPhrase, (string?)null));

        // Assert — both queues visible again, confirming _localSearchPhrase was synced
        cut.Markup.ShouldContain("queue1");
        cut.Markup.ShouldContain("queue2");
    }
}
