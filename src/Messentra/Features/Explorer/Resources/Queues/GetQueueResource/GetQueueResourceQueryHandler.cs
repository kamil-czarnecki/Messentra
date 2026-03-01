using Azure.Messaging.ServiceBus;
using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.Features.Explorer.Resources.Queues.GetQueueResource;

public sealed class GetQueueResourceQueryHandler : IQueryHandler<GetQueueResourceQuery, GetQueueResult>
{
    private readonly IAzureServiceBusQueueProvider _provider;

    public GetQueueResourceQueryHandler(IAzureServiceBusQueueProvider provider)
    {
        _provider = provider;
    }

    public async ValueTask<GetQueueResult> Handle(GetQueueResourceQuery query, CancellationToken cancellationToken)
    {
        var connectionInfo = ToConnectionInfo(query.ConnectionConfig);
        try
        {
            var queue = await _provider.GetByName(connectionInfo, query.QueueName, cancellationToken);
            
            return queue;
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            return new QueueNotFound();
        }
    }

    private static ConnectionInfo ToConnectionInfo(ConnectionConfig config) =>
        config.ConnectionType switch
        {
            ConnectionType.ConnectionString => new ConnectionInfo.ConnectionString(
                config.ConnectionStringConfig!.ConnectionString),
            ConnectionType.EntraId => new ConnectionInfo.ManagedIdentity(
                FullyQualifiedNamespace: config.EntraIdConfig!.Namespace,
                TenantId: config.EntraIdConfig.TenantId,
                ClientId: config.EntraIdConfig.ClientId),
            _ => throw new InvalidOperationException($"Unsupported connection type: {config.ConnectionType}")
        };
}