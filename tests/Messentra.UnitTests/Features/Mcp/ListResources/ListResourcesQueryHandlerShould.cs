using Messentra.Domain;
using Messentra.Features.Mcp.ListResources;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Messentra.Infrastructure.AzureServiceBus.Topics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Mcp.ListResources;

public sealed class ListResourcesQueryHandlerShould
{
    private readonly Mock<IAzureServiceBusQueueProvider> _queueProvider = new();
    private readonly Mock<IAzureServiceBusTopicProvider> _topicProvider = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ListResourcesQueryHandler _sut;

    public ListResourcesQueryHandlerShould()
    {
        _sut = new ListResourcesQueryHandler(_queueProvider.Object, _topicProvider.Object, _cache);
    }

    private static Resource.Queue MakeQueue(string name, long active = 0, long dlq = 0)
        => new(name, $"queue:{name}", MakeOverview(active, dlq), MakeQueueProperties());

    private static Resource.Subscription MakeSubscription(string topicName, string name, long active = 0, long dlq = 0)
        => new(name, topicName, $"topic:{topicName}/subscription:{name}", MakeOverview(active, dlq), MakeSubscriptionProperties());

    private static ResourceOverview MakeOverview(long active, long dlq)
        => new("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            new MessageInfo(active, dlq, 0, 0, 0, active + dlq),
            new SizeInfo(0, 1024));

    private static QueueProperties MakeQueueProperties()
        => new(TimeSpan.FromDays(1), TimeSpan.FromSeconds(30), TimeSpan.MaxValue,
            10, false, null, null, false, false, TimeSpan.Zero, false, null, string.Empty);

    private static SubscriptionProperties MakeSubscriptionProperties()
        => new(TimeSpan.FromDays(1), TimeSpan.FromSeconds(30), TimeSpan.MaxValue,
            10, false, null, null, false, string.Empty);

    private static ListResourcesQuery MakeQuery(
        IReadOnlySet<string>? urlFilter = null,
        string? nameFilter = null,
        bool hasDlq = false)
    {
        var config = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test");
        return new ListResourcesQuery(1L, config, urlFilter, nameFilter, hasDlq);
    }

    [Fact]
    public async Task ReturnAllResources_WhenNoFilters()
    {
        _queueProvider.Setup(p => p.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { MakeQueue("orders") }.AsReadOnly());
        _topicProvider.Setup(p => p.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Resource.Topic>().AsReadOnly());

        var result = await _sut.Handle(MakeQuery(), CancellationToken.None);

        result.ShouldHaveSingleItem().Name.ShouldBe("orders");
    }

    [Fact]
    public async Task FilterByResourceUrlSet_WhenProvided()
    {
        _queueProvider.Setup(p => p.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { MakeQueue("orders"), MakeQueue("invoices") }.AsReadOnly());
        _topicProvider.Setup(p => p.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Resource.Topic>().AsReadOnly());

        var result = await _sut.Handle(MakeQuery(urlFilter: new HashSet<string> { "queue:orders" }), CancellationToken.None);

        result.ShouldHaveSingleItem().Name.ShouldBe("orders");
    }

    [Fact]
    public async Task FilterByNameSubstring_WhenProvided()
    {
        _queueProvider.Setup(p => p.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { MakeQueue("orders-prod"), MakeQueue("invoices") }.AsReadOnly());
        _topicProvider.Setup(p => p.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Resource.Topic>().AsReadOnly());

        var result = await _sut.Handle(MakeQuery(nameFilter: "ORDER"), CancellationToken.None);

        result.ShouldHaveSingleItem().Name.ShouldBe("orders-prod");
    }

    [Fact]
    public async Task FilterByHasDlq_WhenTrue()
    {
        _queueProvider.Setup(p => p.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { MakeQueue("orders", dlq: 5), MakeQueue("invoices", dlq: 0) }.AsReadOnly());
        _topicProvider.Setup(p => p.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Resource.Topic>().AsReadOnly());

        var result = await _sut.Handle(MakeQuery(hasDlq: true), CancellationToken.None);

        result.ShouldHaveSingleItem().Name.ShouldBe("orders");
    }

    [Fact]
    public async Task FlattenSubscriptions_FromTopics()
    {
        _queueProvider.Setup(p => p.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Resource.Queue>().AsReadOnly());
        var topic = new Resource.Topic(
            "events",
            "topic:events",
            MakeOverview(0, 0),
            new TopicProperties(TimeSpan.FromDays(1), TimeSpan.MaxValue, false, false, TimeSpan.Zero, null, string.Empty),
            [MakeSubscription("events", "sub1"), MakeSubscription("events", "sub2")]);
        _topicProvider.Setup(p => p.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { topic }.AsReadOnly());

        var result = await _sut.Handle(MakeQuery(), CancellationToken.None);

        var list = result.ToList();
        list.Count.ShouldBe(2);
        list.ShouldAllBe(r => r.Type == "subscription");
        list.ShouldAllBe(r => r.TopicName == "events");
    }

    [Fact]
    public async Task ReturnFromCache_WhenCacheHit()
    {
        _queueProvider.Setup(p => p.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { MakeQueue("cached-queue") }.AsReadOnly());
        _topicProvider.Setup(p => p.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Resource.Topic>().AsReadOnly());

        await _sut.Handle(MakeQuery(), CancellationToken.None);
        _queueProvider.Reset();
        _topicProvider.Reset();

        var result = await _sut.Handle(MakeQuery(), CancellationToken.None);

        result.ShouldHaveSingleItem().Name.ShouldBe("cached-queue");
        _queueProvider.Verify(p => p.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
