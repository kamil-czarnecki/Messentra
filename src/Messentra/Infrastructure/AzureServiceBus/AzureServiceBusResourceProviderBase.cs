using System.Net.Sockets;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Messentra.Infrastructure.AzureServiceBus.Factories;

namespace Messentra.Infrastructure.AzureServiceBus;

public abstract class AzureServiceBusResourceProviderBase(IAzureServiceBusAdminClientFactory clientFactory)
{
    protected async Task<TResult> ExecuteWithClientRecovery<TResult>(
        ConnectionInfo info,
        Func<ServiceBusAdministrationClient, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var client = await GetClient(info, cancellationToken);

        try
        {
            return await operation(client);
        }
        catch (Exception ex) when (ShouldInvalidateClient(ex))
        {
            InvalidateClient(info);
            throw;
        }
    }

    protected async Task<ServiceBusAdministrationClient> GetClient(
        ConnectionInfo info,
        CancellationToken cancellationToken) =>
        info switch
        {
            ConnectionInfo.ConnectionString cs => await clientFactory.CreateClient(cs.Value, cancellationToken),
            ConnectionInfo.ManagedIdentity mi => await clientFactory.CreateClient(
                mi.FullyQualifiedNamespace,
                mi.TenantId,
                mi.ClientId,
                cancellationToken),
            _ => throw new InvalidOperationException("Invalid connection info type")
        };

    private void InvalidateClient(ConnectionInfo info)
    {
        switch (info)
        {
            case ConnectionInfo.ConnectionString cs:
                clientFactory.InvalidateClient(cs.Value);
                return;
            case ConnectionInfo.ManagedIdentity mi:
                clientFactory.InvalidateClient(mi.FullyQualifiedNamespace, mi.TenantId, mi.ClientId);
                return;
            default:
                throw new InvalidOperationException("Invalid connection info type");
        }
    }

    private static bool ShouldInvalidateClient(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => true,
            ServiceBusException serviceBusException => !serviceBusException.IsTransient,
            RequestFailedException requestFailedException => requestFailedException.Status is 401 or 403,
            SocketException or IOException or HttpRequestException => true,
            TimeoutException => false,
            _ => ex.InnerException is not null && ShouldInvalidateClient(ex.InnerException)
        };
    }

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

