using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus.Administration;

namespace Messentra.Infrastructure.AzureServiceBus.Factories;

public interface IAzureServiceBusAdminClientFactory
{
    ServiceBusAdministrationClient CreateClient(string connectionString);
    ServiceBusAdministrationClient CreateClient(
        string fullyQualifiedNamespace,
        string tenantId,
        string clientId);
}

public sealed class AzureServiceBusAdminClientFactory : IAzureServiceBusAdminClientFactory
{
    private readonly IAzureServiceBusTokenCredentialFactory _tokenCredentialFactory;
    private readonly ConcurrentDictionary<CacheKey, ServiceBusAdministrationClient> _clients = new();

    public AzureServiceBusAdminClientFactory(IAzureServiceBusTokenCredentialFactory tokenCredentialFactory)
    {
        _tokenCredentialFactory = tokenCredentialFactory;
    }

    public ServiceBusAdministrationClient CreateClient(string connectionString)
    {
        var cacheKey = CacheKey.Create(connectionString);
        var client = _clients.GetOrAdd(cacheKey, _ => new ServiceBusAdministrationClient(connectionString));
        
        return client;
    }
    
    public ServiceBusAdministrationClient CreateClient(
        string fullyQualifiedNamespace,
        string tenantId,
        string clientId)
    {
        var cacheKey = CacheKey.Create(fullyQualifiedNamespace, tenantId, clientId);
        var tokenCredential = _tokenCredentialFactory.Create(fullyQualifiedNamespace, tenantId, clientId);
        var client = _clients
            .GetOrAdd(cacheKey, _ => new ServiceBusAdministrationClient(fullyQualifiedNamespace, tokenCredential));
        
        return client;
    }

    private record CacheKey(string Key)
    {
        public static CacheKey Create(string connectionString) => new(connectionString);

        public static CacheKey Create(string fullyQualifiedNamespace, string tenantId, string clientId) =>
            new($"{fullyQualifiedNamespace}|{tenantId}|{clientId}");
    }
}