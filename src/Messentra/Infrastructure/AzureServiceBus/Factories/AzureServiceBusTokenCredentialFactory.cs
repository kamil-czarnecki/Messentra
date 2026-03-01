using System.Collections.Concurrent;
using Azure.Core;
using Azure.Identity;

namespace Messentra.Infrastructure.AzureServiceBus.Factories;

public interface IAzureServiceBusTokenCredentialFactory
{
    TokenCredential Create(string fullyQualifiedNamespace, string tenantId, string clientId);
}

public sealed class AzureServiceBusTokenCredentialFactory : IAzureServiceBusTokenCredentialFactory
{
    private readonly ConcurrentDictionary<CacheKey, TokenCredential> _credentials = new();
    
    public TokenCredential Create(string fullyQualifiedNamespace, string tenantId, string clientId)
    {
        var cacheKey = CacheKey.Create(fullyQualifiedNamespace, tenantId, clientId);
        var tokenCredential = _credentials.GetOrAdd(cacheKey, _ => new InteractiveBrowserCredential(
            new InteractiveBrowserCredentialOptions
            {
                TenantId = tenantId,
                ClientId = clientId,
                RedirectUri = new Uri("http://localhost"),
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = "Messentra"
                }
            }));
        
        return tokenCredential;
    }
    
    private record CacheKey(string Key)
    {
        public static CacheKey Create(string fullyQualifiedNamespace, string tenantId, string clientId) =>
            new($"{fullyQualifiedNamespace}|{tenantId}|{clientId}");
    }
}