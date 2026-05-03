using Azure.Messaging.ServiceBus;
using Mediator;
using Messentra.Features.Mcp.ListResources;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using Microsoft.Extensions.Caching.Memory;

namespace Messentra.Features.Mcp.GetResource;

public sealed class GetResourceQueryHandler(
    IAzureServiceBusQueueProvider queueProvider,
    IAzureServiceBusSubscriptionProvider subscriptionProvider,
    IMemoryCache cache)
    : IQueryHandler<GetResourceQuery, GetResourceResult>
{
    public async ValueTask<GetResourceResult> Handle(GetResourceQuery query, CancellationToken ct)
    {
        var connectionInfo = query.Config.ToConnectionInfo();

        try
        {
            Resource resource = query.TopicName is null
                ? await queueProvider.GetByName(connectionInfo, query.ResourceName, ct)
                : await subscriptionProvider.GetByName(connectionInfo, query.TopicName, query.ResourceName, ct);

            UpdateCache(query.ConnectionId, resource);
            
            return ResourceSummary.From(resource);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            return new McpError($"Resource '{query.ResourceName}' not found.");
        }
    }

    private void UpdateCache(long connectionId, Resource updated)
    {
        var key = ListResourcesQueryHandler.CacheKey(connectionId);
        
        if (!cache.TryGetValue(key, out IReadOnlyList<Resource>? existing) || existing is null)
            return;

        var updatedList = existing
            .Select(r => r.Url == updated.Url ? updated : r)
            .ToList();

        cache.Set(key, updatedList, TimeSpan.FromMinutes(ListResourcesQueryHandler.CacheTtlMinutes));
    }
}
