using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Topics;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.Features.Explorer.Resources.Topics.GetAllTopicResources;

public sealed class GetAllTopicResourcesQueryHandler : IQueryHandler<GetAllTopicResourcesQuery, IReadOnlyCollection<Resource.Topic>>
{
    private readonly IAzureServiceBusTopicProvider _provider;

    public GetAllTopicResourcesQueryHandler(IAzureServiceBusTopicProvider provider)
    {
        _provider = provider;
    }

    public async ValueTask<IReadOnlyCollection<Resource.Topic>> Handle(GetAllTopicResourcesQuery query, CancellationToken cancellationToken)
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

