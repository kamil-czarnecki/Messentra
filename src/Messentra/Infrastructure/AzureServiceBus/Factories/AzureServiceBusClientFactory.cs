using System.Collections.Concurrent;
using System.Data.Common;
using Azure.Messaging.ServiceBus;

namespace Messentra.Infrastructure.AzureServiceBus.Factories;

public interface IAzureServiceBusClientFactory
{
    Task<ServiceBusClient> CreateClient(string connectionString, CancellationToken cancellationToken);
    Task<ServiceBusClient> CreateClient(
        string fullyQualifiedNamespace,
        string tenantId,
        string clientId,
        CancellationToken cancellationToken);
    Task InvalidateClient(string connectionString);
    Task InvalidateClient(string fullyQualifiedNamespace, string tenantId, string clientId);
}

public sealed class AzureServiceBusClientFactory : IAzureServiceBusClientFactory, IAsyncDisposable
{
    private readonly IAzureServiceBusTokenCredentialFactory _credentialFactory;
    private readonly ILogger<AzureServiceBusClientFactory> _logger;
    private readonly ConcurrentDictionary<CacheKey, Lazy<Task<ServiceBusClient>>> _clients = new();

    public AzureServiceBusClientFactory(
        IAzureServiceBusTokenCredentialFactory credentialFactory,
        ILogger<AzureServiceBusClientFactory> logger)
    {
        _credentialFactory = credentialFactory;
        _logger = logger;
    }

    public Task<ServiceBusClient> CreateClient(string connectionString, CancellationToken cancellationToken)
    {
        var normalizedConnectionString = RemoveEndpointPortWhenEmulator(connectionString);
        var key = CacheKey.Create(normalizedConnectionString);
        var client = _clients.GetOrAdd(key,
            _ => new Lazy<Task<ServiceBusClient>>(() =>
                Task.FromResult(new ServiceBusClient(normalizedConnectionString))));
        
        return GetOrResetOnFailure(key, client);
    }
    
    public Task<ServiceBusClient> CreateClient(
        string fullyQualifiedNamespace,
        string tenantId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var key = CacheKey.Create(fullyQualifiedNamespace, tenantId, clientId);
        var client = _clients.GetOrAdd(key, _ => new Lazy<Task<ServiceBusClient>>(async () =>
        {
            var tokenCredential = await _credentialFactory.Create(tenantId, clientId, cancellationToken);

            return new ServiceBusClient(fullyQualifiedNamespace, tokenCredential);
        }));
        
        return GetOrResetOnFailure(key, client);
    }

    public Task InvalidateClient(string connectionString)
    {
        var normalizedConnectionString = RemoveEndpointPortWhenEmulator(connectionString);
        var key = CacheKey.Create(normalizedConnectionString);

        return RemoveAndDispose(key);
    }

    public async Task InvalidateClient(string fullyQualifiedNamespace, string tenantId, string clientId)
    {
        var key = CacheKey.Create(fullyQualifiedNamespace, tenantId, clientId);
        await RemoveAndDispose(key).ConfigureAwait(false);
        _credentialFactory.Invalidate(tenantId, clientId);
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

    private async Task RemoveAndDispose(CacheKey cacheKey)
    {
        if (!_clients.TryRemove(cacheKey, out var existing))
            return;

        if (!existing.IsValueCreated)
            return;

        try
        {
            var client = await existing.Value.ConfigureAwait(false);
            await client.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
            // Ignore dispose/creation errors while forcing cache invalidation.
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
    
    private string RemoveEndpointPortWhenEmulator(string connectionString)
    {
        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

            if (!builder.TryGetValue("UseDevelopmentEmulator", out var emulatorValue) ||
                !bool.TryParse(emulatorValue.ToString(), out var isEmulator) ||
                !isEmulator ||
                !builder.TryGetValue("Endpoint", out var endpointValue) ||
                endpointValue is not string endpoint ||
                string.IsNullOrWhiteSpace(endpoint))
            {
                return connectionString;
            }

            var endpointToParse = endpoint.EndsWith('/') ? endpoint : endpoint + "/";

            if (!Uri.TryCreate(endpointToParse, UriKind.Absolute, out var endpointUri))
            {
                return connectionString;
            }

            var uriBuilder = new UriBuilder(endpointUri)
            {
                Port = -1
            };

            builder["Endpoint"] = uriBuilder.Uri.GetLeftPart(UriPartial.Authority);

            return builder.ConnectionString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to normalize connection string for emulator. Returning original connection string.");
            
            return connectionString;
        }
    }
    
    private record CacheKey(string Key)
    {
        public static CacheKey Create(string connectionString) => new(connectionString);

        public static CacheKey Create(string fullyQualifiedNamespace, string tenantId, string clientId) =>
            new($"{fullyQualifiedNamespace}|{tenantId}|{clientId}");
    }
}