using System.Collections.Concurrent;
using Azure.Core.Pipeline;
using Azure.Messaging.ServiceBus.Administration;

namespace Messentra.Infrastructure.AzureServiceBus.Factories;

public interface IAzureServiceBusAdminClientFactory
{
    Task<ServiceBusAdministrationClient> CreateClient(string connectionString, CancellationToken cancellationToken);
    Task<ServiceBusAdministrationClient> CreateClient(string fullyQualifiedNamespace,
        string tenantId,
        string clientId,
        CancellationToken cancellationToken);
    void InvalidateClient(string connectionString);
    void InvalidateClient(string fullyQualifiedNamespace, string tenantId, string clientId);
}

public sealed class AzureServiceBusAdminClientFactory : IAzureServiceBusAdminClientFactory
{
    private readonly IAzureServiceBusTokenCredentialFactory _tokenCredentialFactory;
    private readonly ConcurrentDictionary<CacheKey, Lazy<Task<CachedClient>>> _clients = new();

    public AzureServiceBusAdminClientFactory(IAzureServiceBusTokenCredentialFactory tokenCredentialFactory)
    {
        _tokenCredentialFactory = tokenCredentialFactory;
    }

    public Task<ServiceBusAdministrationClient> CreateClient(
        string connectionString,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey.Create(connectionString);
        var client = _clients.GetOrAdd(cacheKey,
            _ => new Lazy<Task<CachedClient>>(() =>
                Task.FromResult(CreateWithConnectionString(connectionString))));
        
        return GetOrResetOnFailure(cacheKey, client);
    }
    
    public Task<ServiceBusAdministrationClient> CreateClient(
        string fullyQualifiedNamespace,
        string tenantId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey.Create(fullyQualifiedNamespace, tenantId, clientId);
        var client = _clients.GetOrAdd(cacheKey, _ => new Lazy<Task<CachedClient>>(async () =>
        {
            var token = await _tokenCredentialFactory.Create(tenantId, clientId, cancellationToken);
            return CreateWithManagedIdentity(fullyQualifiedNamespace, token);
        }));
        
        return GetOrResetOnFailure(cacheKey, client);
    }

    public void InvalidateClient(string connectionString)
    {
        var cacheKey = CacheKey.Create(connectionString);
        TryRemoveAndDispose(cacheKey);
    }

    public void InvalidateClient(string fullyQualifiedNamespace, string tenantId, string clientId)
    {
        var cacheKey = CacheKey.Create(fullyQualifiedNamespace, tenantId, clientId);
        TryRemoveAndDispose(cacheKey);
        _tokenCredentialFactory.Invalidate(tenantId, clientId);
    }
    
    private async Task<ServiceBusAdministrationClient> GetOrResetOnFailure(
        CacheKey cacheKey,
        Lazy<Task<CachedClient>> lazyCredential)
    {
        try
        {
            return (await lazyCredential.Value.ConfigureAwait(false)).Client;
        }
        catch
        {
            _clients.TryRemove(new KeyValuePair<CacheKey, Lazy<Task<CachedClient>>>(cacheKey, lazyCredential));
            
            throw;
        }
    }

    private static CachedClient CreateWithConnectionString(string connectionString)
    {
        var transportClient = CreateTransportHttpClient();
        var options = new ServiceBusAdministrationClientOptions
        {
            Transport = new HttpClientTransport(transportClient)
        };

        return new CachedClient(new ServiceBusAdministrationClient(connectionString, options), transportClient);
    }

    private static CachedClient CreateWithManagedIdentity(string fullyQualifiedNamespace, Azure.Core.TokenCredential token)
    {
        var transportClient = CreateTransportHttpClient();
        var options = new ServiceBusAdministrationClientOptions
        {
            Transport = new HttpClientTransport(transportClient)
        };

        return new CachedClient(new ServiceBusAdministrationClient(fullyQualifiedNamespace, token, options), transportClient);
    }

    private static HttpClient CreateTransportHttpClient() => new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
    });

    private void TryRemoveAndDispose(CacheKey cacheKey)
    {
        if (!_clients.TryRemove(cacheKey, out var cached))
            return;

        if (!cached.IsValueCreated || !cached.Value.IsCompletedSuccessfully)
            return;

        cached.Value.Result.TransportHttpClient.Dispose();
    }

    private record CacheKey(string Key)
    {
        public static CacheKey Create(string connectionString) => new(connectionString);

        public static CacheKey Create(string fullyQualifiedNamespace, string tenantId, string clientId) =>
            new($"{fullyQualifiedNamespace}|{tenantId}|{clientId}");
    }

    private sealed record CachedClient(ServiceBusAdministrationClient Client, HttpClient TransportHttpClient);
}