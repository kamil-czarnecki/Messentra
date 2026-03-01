using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus.Queues;

namespace Messentra.Features.Explorer.Messages.FetchQueueMessages;

public sealed class
    FetchQueueMessagesQueryHandler : IQueryHandler<FetchQueueMessagesQuery, IReadOnlyCollection<ServiceBusMessage>>
{
   private readonly IAzureServiceBusQueueMessagesProvider _provider;

   public FetchQueueMessagesQueryHandler(IAzureServiceBusQueueMessagesProvider provider)
   {
       _provider = provider;
   }

   public async ValueTask<IReadOnlyCollection<ServiceBusMessage>> Handle(
       FetchQueueMessagesQuery query,
       CancellationToken cancellationToken) =>
       await _provider.Get(ToConnectionInfo(query.ConnectionConfig), query.QueueName, query.Options, cancellationToken);
   
    private static Infrastructure.AzureServiceBus.ConnectionInfo ToConnectionInfo(ConnectionConfig config) =>
        config.ConnectionType switch
        {
            ConnectionType.ConnectionString => new Infrastructure.AzureServiceBus.ConnectionInfo.ConnectionString(
                config.ConnectionStringConfig!.ConnectionString),
            ConnectionType.EntraId => new Infrastructure.AzureServiceBus.ConnectionInfo.ManagedIdentity(
                FullyQualifiedNamespace: config.EntraIdConfig!.Namespace,
                TenantId: config.EntraIdConfig.TenantId,
                ClientId: config.EntraIdConfig.ClientId),
            _ => throw new InvalidOperationException($"Unsupported connection type: {config.ConnectionType}")
        };
}