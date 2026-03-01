using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.Features.Explorer.Resources.Queues.GetAllQueueResources;

public sealed class GetAllQueueResourcesQueryHandler : IQueryHandler<GetAllQueueResourcesQuery, IReadOnlyCollection<Resource.Queue>>
{
    private readonly IAzureServiceBusQueueProvider _provider;

    public GetAllQueueResourcesQueryHandler(IAzureServiceBusQueueProvider provider)
    {
        _provider = provider;
    }

    public async ValueTask<IReadOnlyCollection<Resource.Queue>> Handle(GetAllQueueResourcesQuery query, CancellationToken cancellationToken)
    {
        var connectionInfo = ToConnectionInfo(query.ConnectionConfig);
        return await _provider.GetAll(connectionInfo, cancellationToken);
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

