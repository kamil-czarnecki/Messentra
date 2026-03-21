using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Messentra.Infrastructure.AzureServiceBus.Factories;

namespace Messentra.Infrastructure.AzureServiceBus;

public abstract class AzureServiceBusResourceProviderBase(IAzureServiceBusAdminClientFactory clientFactory)
{
    protected async Task<ServiceBusAdministrationClient> GetClient(ConnectionInfo info) =>
        info switch
        {
            ConnectionInfo.ConnectionString cs => await clientFactory.CreateClient(cs.Value),
            ConnectionInfo.ManagedIdentity mi => await clientFactory.CreateClient(
                mi.FullyQualifiedNamespace,
                mi.TenantId,
                mi.ClientId),
            _ => throw new InvalidOperationException("Invalid connection info type")
        };

    protected static string GetNamespace(ConnectionInfo info) =>
        info switch
        {
            ConnectionInfo.ConnectionString cs => ServiceBusConnectionStringProperties
                .Parse(cs.Value)
                .FullyQualifiedNamespace,
            ConnectionInfo.ManagedIdentity mi => mi.FullyQualifiedNamespace,
            _ => throw new InvalidOperationException("Invalid connection info type")
        };
}

