using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

namespace Messentra.Infrastructure.AzureServiceBus.Factories;

public interface IAzureServiceBusClientFactory
{
    ServiceBusClient CreateClient(string connectionString);
    ServiceBusClient CreateClient(string fullyQualifiedNamespace, string tenantId, string clientId);
}

public sealed class AzureServiceBusClientFactory : IAzureServiceBusClientFactory, IAsyncDisposable
{
    private readonly IAzureServiceBusTokenCredentialFactory _credentialFactory;
    private readonly ConcurrentDictionary<CacheKey, ServiceBusClient> _clients = new();

    public AzureServiceBusClientFactory(IAzureServiceBusTokenCredentialFactory credentialFactory)
    {
        _credentialFactory = credentialFactory;
    }

    public ServiceBusClient CreateClient(string connectionString)
    {
        var key = CacheKey.Create(connectionString);
        var client = _clients.GetOrAdd(key, _ => new ServiceBusClient(connectionString));
        
        return client;
    }
    
    public ServiceBusClient CreateClient(
        string fullyQualifiedNamespace,
        string tenantId,
        string clientId)
    {
        var key = CacheKey.Create(fullyQualifiedNamespace, tenantId, clientId);
        var tokenCredential = _credentialFactory.Create(fullyQualifiedNamespace, tenantId, clientId);
        var client = _clients.GetOrAdd(key, _ => new ServiceBusClient(fullyQualifiedNamespace, tokenCredential));
        
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values)
        {
            await client.DisposeAsync();
        }
    }
    
    private record CacheKey(string Key)
    {
        public static CacheKey Create(string connectionString) => new(connectionString);

        public static CacheKey Create(string fullyQualifiedNamespace, string tenantId, string clientId) =>
            new($"{fullyQualifiedNamespace}|{tenantId}|{clientId}");
    }
}