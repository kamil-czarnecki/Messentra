using Azure.Messaging.ServiceBus;
using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus.Topics;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.Features.Explorer.Resources.Topics.GetTopicResource;

public sealed class GetTopicResourceQueryHandler : IQueryHandler<GetTopicResourceQuery, GetTopicResult>
{
    private readonly IAzureServiceBusTopicProvider _provider;

    public GetTopicResourceQueryHandler(IAzureServiceBusTopicProvider provider)
    {
        _provider = provider;
    }

    public async ValueTask<GetTopicResult> Handle(GetTopicResourceQuery query, CancellationToken cancellationToken)
    {
        var connectionInfo = ToConnectionInfo(query.ConnectionConfig);
        try
        {
            var topic = await _provider.GetByName(connectionInfo, query.TopicName, cancellationToken);
            return topic;
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            return new TopicNotFound();
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

