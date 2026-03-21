using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus.Administration;

namespace Messentra.Infrastructure.AzureServiceBus.Factories;

public interface IAzureServiceBusAdminClientFactory
{
    Task<ServiceBusAdministrationClient> CreateClient(string connectionString, CancellationToken cancellationToken);
    Task<ServiceBusAdministrationClient> CreateClient(string fullyQualifiedNamespace,
        string tenantId,
        string clientId,
        CancellationToken cancellationToken);
}

public sealed class AzureServiceBusAdminClientFactory : IAzureServiceBusAdminClientFactory
{
    private readonly IAzureServiceBusTokenCredentialFactory _tokenCredentialFactory;
    private readonly ConcurrentDictionary<CacheKey, Lazy<Task<ServiceBusAdministrationClient>>> _clients = new();

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
            _ => new Lazy<Task<ServiceBusAdministrationClient>>(() =>
                Task.FromResult(new ServiceBusAdministrationClient(connectionString))));
        
        return GetOrResetOnFailure(cacheKey, client);
    }
    
    public Task<ServiceBusAdministrationClient> CreateClient(
        string fullyQualifiedNamespace,
        string tenantId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey.Create(fullyQualifiedNamespace, tenantId, clientId);
        var client = _clients.GetOrAdd(cacheKey, _ => new Lazy<Task<ServiceBusAdministrationClient>>(async () =>
        {
            var token = await _tokenCredentialFactory.Create(tenantId, clientId, cancellationToken);
            return new ServiceBusAdministrationClient(fullyQualifiedNamespace, token);
        }));
        
        return GetOrResetOnFailure(cacheKey, client);
    }
    
    private async Task<ServiceBusAdministrationClient> GetOrResetOnFailure(
        CacheKey cacheKey,
        Lazy<Task<ServiceBusAdministrationClient>> lazyCredential)
    {
        try
        {
            return await lazyCredential.Value.ConfigureAwait(false);
        }
        catch
        {
            _clients.TryRemove(new KeyValuePair<CacheKey, Lazy<Task<ServiceBusAdministrationClient>>>(cacheKey, lazyCredential));
            
            throw;
        }
    }

    private record CacheKey(string Key)
    {
        public static CacheKey Create(string connectionString) => new(connectionString);

        public static CacheKey Create(string fullyQualifiedNamespace, string tenantId, string clientId) =>
            new($"{fullyQualifiedNamespace}|{tenantId}|{clientId}");
    }
}