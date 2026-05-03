using Azure.Messaging.ServiceBus;
using Messentra.Domain;
using Messentra.Features.Mcp;
using Messentra.Features.Mcp.GetResource;
using Messentra.Features.Mcp.ListResources;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Mcp.GetResource;

public sealed class GetResourceQueryHandlerShould
{
    private readonly Mock<IAzureServiceBusQueueProvider> _queueProvider = new();
    private readonly Mock<IAzureServiceBusSubscriptionProvider> _subscriptionProvider = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly GetResourceQueryHandler _sut;

    public GetResourceQueryHandlerShould()
    {
        _sut = new GetResourceQueryHandler(_queueProvider.Object, _subscriptionProvider.Object, _cache);
    }

    [Fact]
    public async Task FetchQueue_WhenTopicNameIsNull()
    {
        var queue = MakeQueue("orders");
        _queueProvider.Setup(p => p.GetByName(It.IsAny<ConnectionInfo>(), "orders", It.IsAny<CancellationToken>()))
            .ReturnsAsync(queue);

        var result = await _sut.Handle(MakeQuery("orders"), CancellationToken.None);

        var summary = result.AsT0;
        summary.Name.ShouldBe("orders");
        summary.Type.ShouldBe("queue");
    }

    [Fact]
    public async Task FetchSubscription_WhenTopicNameIsProvided()
    {
        var sub = MakeSubscription("events", "sub1");
        _subscriptionProvider.Setup(p => p.GetByName(It.IsAny<ConnectionInfo>(), "events", "sub1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);

        var result = await _sut.Handle(MakeQuery("sub1", topicName: "events"), CancellationToken.None);

        var summary = result.AsT0;
        summary.Name.ShouldBe("sub1");
        summary.Type.ShouldBe("subscription");
        summary.TopicName.ShouldBe("events");
    }

    [Fact]
    public async Task UpdateCacheEntry_WhenCacheExists()
    {
        var oldQueue = MakeQueue("orders", dlq: 0);
        _cache.Set(ListResourcesQueryHandler.CacheKey(1L), (IReadOnlyList<Resource>)[oldQueue], TimeSpan.FromMinutes(5));

        var newQueue = MakeQueue("orders", dlq: 3);
        _queueProvider.Setup(p => p.GetByName(It.IsAny<ConnectionInfo>(), "orders", It.IsAny<CancellationToken>()))
            .ReturnsAsync(newQueue);

        await _sut.Handle(MakeQuery("orders"), CancellationToken.None);

        var cached = _cache.Get<IReadOnlyList<Resource>>(ListResourcesQueryHandler.CacheKey(1L))!;
        var cachedQueue = cached.Single().ShouldBeOfType<Resource.Queue>();
        cachedQueue.Overview.MessageInfo.DeadLetter.ShouldBe(3L);
    }

    [Fact]
    public async Task ReturnMcpError_WhenResourceNotFound()
    {
        _queueProvider.Setup(p => p.GetByName(It.IsAny<ConnectionInfo>(), "missing", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("Entity not found", ServiceBusFailureReason.MessagingEntityNotFound));

        var result = await _sut.Handle(MakeQuery("missing"), CancellationToken.None);

        result.AsT1.Message.ShouldContain("missing");
    }

    private static GetResourceQuery MakeQuery(string resourceName, string? topicName = null)
    {
        var config = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test");
        return new GetResourceQuery(1L, config, resourceName, topicName);
    }

    private static Resource.Queue MakeQueue(string name, long active = 0, long dlq = 0)
        => new(name, $"queue:{name}", MakeOverview(active, dlq), MakeQueueProperties());

    private static Resource.Subscription MakeSubscription(string topicName, string name, long active = 0, long dlq = 0)
        => new(name, topicName, $"topic:{topicName}/subscription:{name}", MakeOverview(active, dlq), MakeSubscriptionProperties());

    private static ResourceOverview MakeOverview(long active, long dlq)
        => new("Active", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch,
            new MessageInfo(active, dlq, 0, 0, 0, active + dlq),
            new SizeInfo(0, 1024));

    private static QueueProperties MakeQueueProperties()
        => new(TimeSpan.FromDays(1), TimeSpan.FromSeconds(30), TimeSpan.MaxValue,
            10, false, null, null, false, false, TimeSpan.Zero, false, null, string.Empty);

    private static SubscriptionProperties MakeSubscriptionProperties()
        => new(TimeSpan.FromDays(1), TimeSpan.FromSeconds(30), TimeSpan.MaxValue,
            10, false, null, null, false, string.Empty);
}
