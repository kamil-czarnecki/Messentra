using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

namespace Messentra.Infrastructure.AzureServiceBus.Factories;

public interface IAzureServiceBusClientFactory
{
    Task<ServiceBusClient> CreateClient(string connectionString);
    Task<ServiceBusClient> CreateClient(string fullyQualifiedNamespace, string tenantId, string clientId);
}

public sealed class AzureServiceBusClientFactory : IAzureServiceBusClientFactory, IAsyncDisposable
{
    private readonly IAzureServiceBusTokenCredentialFactory _credentialFactory;
    private readonly ConcurrentDictionary<CacheKey, Lazy<Task<ServiceBusClient>>> _clients = new();

    public AzureServiceBusClientFactory(IAzureServiceBusTokenCredentialFactory credentialFactory)
    {
        _credentialFactory = credentialFactory;
    }

    public Task<ServiceBusClient> CreateClient(string connectionString)
    {
        var key = CacheKey.Create(connectionString);
        var client = _clients.GetOrAdd(key,
            _ => new Lazy<Task<ServiceBusClient>>(() => Task.FromResult(new ServiceBusClient(connectionString))));
        
        return GetOrResetOnFailure(key, client);
    }
    
    public Task<ServiceBusClient> CreateClient(
        string fullyQualifiedNamespace,
        string tenantId,
        string clientId)
    {
        var key = CacheKey.Create(fullyQualifiedNamespace, tenantId, clientId);
        var client = _clients.GetOrAdd(key, _ => new Lazy<Task<ServiceBusClient>>(async () =>
        {
            var tokenCredential = await _credentialFactory.Create(tenantId, clientId);

            return new ServiceBusClient(fullyQualifiedNamespace, tokenCredential);
        }));
        
        return GetOrResetOnFailure(key, client);
    }

    private async Task<ServiceBusClient> GetOrResetOnFailure(
        CacheKey cacheKey,
        Lazy<Task<ServiceBusClient>> lazyCredential)
    {
        try
        {
            return await lazyCredential.Value.ConfigureAwait(false);
        }
        catch
        {
            _clients.TryRemove(new KeyValuePair<CacheKey, Lazy<Task<ServiceBusClient>>>(cacheKey, lazyCredential));

            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values)
        {
            if (!client.IsValueCreated)
                continue;
            
            var serviceBusClient = await client.Value.ConfigureAwait(false);
            
            await serviceBusClient.DisposeAsync().ConfigureAwait(false);
        }
    }
    
    private record CacheKey(string Key)
    {
        public static CacheKey Create(string connectionString) => new(connectionString);

        public static CacheKey Create(string fullyQualifiedNamespace, string tenantId, string clientId) =>
            new($"{fullyQualifiedNamespace}|{tenantId}|{clientId}");
    }
}