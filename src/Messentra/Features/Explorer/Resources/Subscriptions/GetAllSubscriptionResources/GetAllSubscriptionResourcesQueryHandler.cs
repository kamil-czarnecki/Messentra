using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.Features.Explorer.Resources.Subscriptions.GetAllSubscriptionResources;

public sealed class GetAllSubscriptionResourcesQueryHandler : IQueryHandler<GetAllSubscriptionResourcesQuery, IReadOnlyCollection<Resource.Subscription>>
{
    private readonly IAzureServiceBusSubscriptionProvider _provider;

    public GetAllSubscriptionResourcesQueryHandler(IAzureServiceBusSubscriptionProvider provider)
    {
        _provider = provider;
    }

    public async ValueTask<IReadOnlyCollection<Resource.Subscription>> Handle(GetAllSubscriptionResourcesQuery query, CancellationToken cancellationToken)
    {
        var connectionInfo = ToConnectionInfo(query.ConnectionConfig);
        return await _provider.GetAll(connectionInfo, query.TopicName, cancellationToken);
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

