using Messentra.Domain;
using Messentra.Features.Explorer.Resources;
using Messentra.Infrastructure.AzureServiceBus;

namespace Messentra.UnitTests.Features.Explorer.Resources;

internal static class ResourceTestData
{
    internal static ConnectionConfig CreateConnectionConfig() =>
        ConnectionConfig.CreateConnectionString(
            "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");

    internal static Resource.Queue CreateQueue(string name) =>
        new(
            name,
            $"https://test.servicebus.windows.net/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0),
                new SizeInfo(0, 1024)),
            new QueueProperties(
                TimeSpan.FromDays(14), TimeSpan.FromSeconds(60), TimeSpan.MaxValue,
                10, false, null, null, false, false, TimeSpan.FromMinutes(1), false, 256, string.Empty));

    internal static Resource.Subscription CreateSubscription(string name, string topicName) =>
        new(
            name,
            topicName,
            $"https://test.servicebus.windows.net/{topicName}/subscriptions/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0),
                new SizeInfo(0, 1024)),
            new SubscriptionProperties(
                TimeSpan.FromDays(14), TimeSpan.FromSeconds(60), TimeSpan.MaxValue,
                10, false, null, null, false, string.Empty));

    internal static Resource.Topic CreateTopic(string name, IReadOnlyCollection<Resource.Subscription>? subscriptions = null) =>
        new(
            name,
            $"https://test.servicebus.windows.net/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0),
                new SizeInfo(0, 1024)),
            new TopicProperties(
                TimeSpan.FromDays(14), TimeSpan.MaxValue, false, false, TimeSpan.FromMinutes(1), 256, string.Empty),
            subscriptions ?? []);

    internal static NamespaceEntry CreateNamespaceEntry(
        string connectionName = "test-connection",
        long connectionId = 1,
        bool isLoading = false,
        Dictionary<string, QueueEntry>? queues = null,
        Dictionary<string, TopicEntry>? topics = null,
        Dictionary<long, FolderEntry>? folders = null)
    {
        var config = CreateConnectionConfig();
        return new NamespaceEntry(
            connectionId, connectionName, config, isLoading,
            queues ?? [],
            topics ?? [],
            folders ?? []);
    }

    internal static FolderEntry CreateFolderEntry(
        long folderId, string name, string connectionName,
        long connectionId = 1L, IEnumerable<string>? resourceUrls = null)
    {
        var config = CreateConnectionConfig();
        var node = new FolderTreeNode(folderId, connectionId, name, connectionName, config);
        return new FolderEntry(node, new HashSet<string>(resourceUrls ?? []));
    }
}

