using Mediator;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Messentra.Infrastructure.AzureServiceBus.Topics;
using Microsoft.Extensions.Caching.Memory;

namespace Messentra.Features.Mcp.ListResources;

public sealed class ListResourcesQueryHandler(
    IAzureServiceBusQueueProvider queueProvider,
    IAzureServiceBusTopicProvider topicProvider,
    IMemoryCache cache)
    : IQueryHandler<ListResourcesQuery, IEnumerable<ResourceSummary>>
{
    internal const int CacheTtlMinutes = 5;
    internal static string CacheKey(long connectionId) => $"mcp:resources:{connectionId}";

    public async ValueTask<IEnumerable<ResourceSummary>> Handle(ListResourcesQuery query, CancellationToken ct)
    {
        var flat = await GetOrLoadResources(query, ct);

        IEnumerable<Resource> result = flat;

        if (query.ResourceUrlFilter is not null)
            result = result.Where(r => query.ResourceUrlFilter.Contains(r.Url));

        if (query.NameFilter is not null)
            result = result.Where(r => r.Name.Contains(query.NameFilter, StringComparison.OrdinalIgnoreCase));

        if (query.HasDlq)
            result = result.Where(r => r.Overview.MessageInfo.DeadLetter > 0);

        return result.Select(ResourceSummary.From).ToList();
    }

    private async Task<IReadOnlyList<Resource>> GetOrLoadResources(ListResourcesQuery query, CancellationToken ct)
    {
        var key = CacheKey(query.ConnectionId);
        
        if (cache.TryGetValue(key, out IReadOnlyList<Resource>? cached) && cached is not null)
            return cached;

        var connectionInfo = query.Config.ToConnectionInfo();

        var queuesTask = queueProvider.GetAll(connectionInfo, ct);
        var topicsTask = topicProvider.GetAll(connectionInfo, ct);
        var queues = await queuesTask;
        var topics = await topicsTask;

        var flat = queues
            .Cast<Resource>()
            .Concat(topics.SelectMany(t => t.Subscriptions))
            .ToList();

        cache.Set(key, flat, TimeSpan.FromMinutes(CacheTtlMinutes));
        
        return flat;
    }
}
