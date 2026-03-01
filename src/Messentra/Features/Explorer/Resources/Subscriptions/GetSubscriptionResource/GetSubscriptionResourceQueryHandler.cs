using Azure.Messaging.ServiceBus;
using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.Features.Explorer.Resources.Subscriptions.GetSubscriptionResource;

public sealed class GetSubscriptionResourceQueryHandler : IQueryHandler<GetSubscriptionResourceQuery, GetSubscriptionResult>
{
    private readonly IAzureServiceBusSubscriptionProvider _provider;

    public GetSubscriptionResourceQueryHandler(IAzureServiceBusSubscriptionProvider provider)
    {
        _provider = provider;
    }

    public async ValueTask<GetSubscriptionResult> Handle(GetSubscriptionResourceQuery query, CancellationToken cancellationToken)
    {
        var connectionInfo = ToConnectionInfo(query.ConnectionConfig);
        try
        {
            var subscription = await _provider.GetByName(connectionInfo, query.TopicName, query.SubscriptionName, cancellationToken);
            return subscription;
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            return new SubscriptionNotFound();
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

