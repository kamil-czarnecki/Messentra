using System.Reflection;
using Bunit;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components.Details.Tabs;
using Messentra.Infrastructure.AzureServiceBus;
using Microsoft.AspNetCore.Components;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components.Details.Tabs.Messages;

public sealed class MessageGridShould : ComponentTestBase
{
    private static QueueTreeNode BuildQueueNode(string connectionName = "TestNS", string queueName = "my-queue")
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
        var queue = new Resource.Queue(queueName, $"sb://test/queues/{queueName}", overview, props);
        var connectionConfig = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant", "client");
        return new QueueTreeNode(connectionName, queue, connectionConfig);
    }

    private static ServiceBusMessage BuildServiceBusMessage(string messageId = "msg-1")
    {
        var brokerProperties = new BrokerProperties(
            MessageId: messageId,
            SequenceNumber: 1,
            CorrelationId: null, SessionId: null, ReplyToSessionId: null,
            EnqueuedTimeUtc: DateTime.UtcNow, ScheduledEnqueueTimeUtc: DateTime.UtcNow,
            TimeToLive: TimeSpan.FromDays(1),
            LockedUntilUtc: DateTime.UtcNow.AddMinutes(5),
            ExpiresAtUtc: DateTime.UtcNow.AddDays(1),
            DeliveryCount: 1,
            Label: null, To: null, ReplyTo: null, PartitionKey: null, ContentType: null,
            DeadLetterReason: null, DeadLetterErrorDescription: null);
        var dto = new MessageDto("Hello", brokerProperties, new Dictionary<string, object>());
        return new ServiceBusMessage(dto, new Mock<IServiceBusMessageContext>().Object);
    }

    private static List<ServiceBusMessage> GetMessages(IRenderedComponent<MessageGrid> cut)
    {
        var field = typeof(MessageGrid)
            .GetField("_messages", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (List<ServiceBusMessage>)field.GetValue(cut.Instance)!;
    }

    private static void SetMessages(IRenderedComponent<MessageGrid> cut, List<ServiceBusMessage> messages)
    {
        var field = typeof(MessageGrid)
            .GetField("_messages", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(cut.Instance, messages);
    }

    private static void SetResourceNode(IRenderedComponent<MessageGrid> cut, ResourceTreeNode node)
    {
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(MessageGrid.ResourceTreeNode)] = node,
            [nameof(MessageGrid.SubQueue)] = SubQueue.Active,
            [nameof(MessageGrid.OnRefresh)] = EventCallback.Empty
        });
        cut.InvokeAsync(() => cut.Instance.SetParametersAsync(parameters));
    }

    [Fact]
    public void RetainMessagesWhenSameResourceNodeIsRefreshed()
    {
        // Arrange
        var node = BuildQueueNode();
        var cut = Render<MessageGrid>(p => p
            .Add(x => x.ResourceTreeNode, node)
            .Add(x => x.SubQueue, SubQueue.Active)
            .Add(x => x.OnRefresh, EventCallback.Empty));

        SetMessages(cut, [BuildServiceBusMessage()]);

        // Act — simulate Fluxor creating a new node instance for the same queue (e.g. updated counters)
        var refreshedNode = node with { IsLoading = false };
        SetResourceNode(cut, refreshedNode);

        // Assert — messages must survive the Fluxor-triggered re-render
        GetMessages(cut).Count.ShouldBe(1);
    }

    [Fact]
    public void ClearMessagesWhenNavigatingToDifferentResource()
    {
        // Arrange
        var node = BuildQueueNode(queueName: "queue-a");
        var cut = Render<MessageGrid>(p => p
            .Add(x => x.ResourceTreeNode, node)
            .Add(x => x.SubQueue, SubQueue.Active)
            .Add(x => x.OnRefresh, EventCallback.Empty));

        SetMessages(cut, [BuildServiceBusMessage()]);

        // Act — navigate to a different queue
        var differentNode = BuildQueueNode(queueName: "queue-b");
        SetResourceNode(cut, differentNode);

        // Assert — messages must be cleared for the new resource
        GetMessages(cut).ShouldBeEmpty();
    }

    [Fact]
    public void ClearMessagesWhenNavigatingToDifferentConnection()
    {
        // Arrange
        var node = BuildQueueNode(connectionName: "NS-1");
        var cut = Render<MessageGrid>(p => p
            .Add(x => x.ResourceTreeNode, node)
            .Add(x => x.SubQueue, SubQueue.Active)
            .Add(x => x.OnRefresh, EventCallback.Empty));

        SetMessages(cut, [BuildServiceBusMessage()]);

        // Act — same queue name, different connection
        var differentConnection = BuildQueueNode(connectionName: "NS-2");
        SetResourceNode(cut, differentConnection);

        // Assert
        GetMessages(cut).ShouldBeEmpty();
    }
}
