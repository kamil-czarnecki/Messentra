using System.Collections.Concurrent;
using Azure.Core;
using Azure.Identity;

namespace Messentra.Infrastructure.AzureServiceBus.Factories;

public interface IAzureServiceBusTokenCredentialFactory
{
    Task<TokenCredential> Create(string tenantId, string clientId, CancellationToken cancellationToken);
}

public sealed class AzureServiceBusTokenCredentialFactory : IAzureServiceBusTokenCredentialFactory
{
    private static readonly string[] ServiceBusScopes = ["https://servicebus.azure.net/.default"];
    private readonly ConcurrentDictionary<CacheKey, Lazy<Task<TokenCredential>>> _credentials = new();
    private readonly IAuthenticationRecordStore _authenticationRecordStore;
    private readonly IInteractiveAuthBootstrapper _bootstrapper;

    public AzureServiceBusTokenCredentialFactory(
        IAuthenticationRecordStore authenticationRecordStore,
        IInteractiveAuthBootstrapper bootstrapper)
    {
        _authenticationRecordStore = authenticationRecordStore;
        _bootstrapper = bootstrapper;
    }

    public Task<TokenCredential> Create(string tenantId, string clientId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey.Create(tenantId, clientId);
        var lazyCredential = _credentials.GetOrAdd(
            cacheKey,
            _ => new Lazy<Task<TokenCredential>>(
                () => CreateCredential(cacheKey, tenantId, clientId, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return GetOrResetOnFailure(cacheKey, lazyCredential);
    }

    private async Task<TokenCredential> GetOrResetOnFailure(CacheKey cacheKey, Lazy<Task<TokenCredential>> lazyCredential)
    {
        try
        {
            return await lazyCredential.Value.ConfigureAwait(false);
        }
        catch
        {
            _credentials.TryRemove(new KeyValuePair<CacheKey, Lazy<Task<TokenCredential>>>(cacheKey, lazyCredential));
            
            throw;
        }
    }

    private async Task<TokenCredential> CreateCredential(
        CacheKey cacheKey,
        string tenantId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var options = new InteractiveBrowserCredentialOptions
        {
            TenantId = tenantId,
            ClientId = clientId,
            RedirectUri = new Uri("http://localhost"),
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions
            {
                Name = "Messentra"
            }
        };
        var tokenCredential = new InteractiveBrowserCredential(options);
        var existingRecord = _authenticationRecordStore.Get(cacheKey.Key);

        if (existingRecord is null)
        {
            existingRecord = await _bootstrapper
                .AuthenticateAsync(
                    tokenCredential,
                    new TokenRequestContext(ServiceBusScopes),
                    cancellationToken)
                .ConfigureAwait(false);
            
            _authenticationRecordStore.Save(cacheKey.Key, existingRecord);
        }

        options.AuthenticationRecord = existingRecord;

        return new InteractiveBrowserCredential(options);
    }

    private record CacheKey(string Key)
    {
        public static CacheKey Create(string tenantId, string clientId) =>
            new($"{Normalize(tenantId)}|{Normalize(clientId)}");

        private static string Normalize(string value) => value.Trim().ToLowerInvariant();
    }
}