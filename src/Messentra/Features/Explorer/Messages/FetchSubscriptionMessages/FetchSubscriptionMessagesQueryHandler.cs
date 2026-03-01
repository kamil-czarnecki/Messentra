using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.Features.Explorer.Messages.FetchSubscriptionMessages;

public sealed class FetchSubscriptionMessagesQueryHandler
    : IQueryHandler<FetchSubscriptionMessagesQuery, IReadOnlyCollection<ServiceBusMessage>>
{
    private readonly IAzureServiceBusSubscriptionMessagesProvider _provider;

    public FetchSubscriptionMessagesQueryHandler(IAzureServiceBusSubscriptionMessagesProvider provider)
    {
        _provider = provider;
    }

    public async ValueTask<IReadOnlyCollection<ServiceBusMessage>> Handle(
        FetchSubscriptionMessagesQuery query,
        CancellationToken cancellationToken) =>
        await _provider.Get(
            ToConnectionInfo(query.ConnectionConfig),
            query.TopicName,
            query.SubscriptionName,
            query.Options,
            cancellationToken);

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

